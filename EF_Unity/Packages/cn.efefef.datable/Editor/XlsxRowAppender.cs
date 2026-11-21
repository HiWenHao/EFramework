/*
 * ================================================
 * Describe:        向 Luban 表定义工作簿追加数据的极简工具：指定列追加单值 / 末行追加整行，自动定位路径、检测数据、内部报错
 * Author:          Alvin8412
 * CreationTime:    2026-10-21 11:25:00
 * ModifyAuthor:    Alvin8412
 * ModifyTime:      2026-10-22 16:40:00
 * ScriptVersion:   0.8
 * ================================================
 */

using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using EasyFramework.Dataable;
using CompressionLevel = System.IO.Compression.CompressionLevel;

namespace EasyFramework.Edit
{
    /// <summary>
    /// 向 Luban 表定义工作簿追加数据的极简工具
    /// <para>Minimal tool for appending data to the Luban table-definition workbook.</para>
    /// </summary>
    public static class XlsxRowAppender
    {
        // Luban 表定义工作簿文件名
        private const string TablesFileName = "__tables__.xlsx";

        // Luban bean 定义工作簿文件名（与表定义同目录、同生命周期）
        private const string BeansFileName = "__beans__.xlsx";

        // Luban 枚举定义工作簿文件名
        private const string EnumsFileName = "__enums__.xlsx";

        /// <summary>
        /// Luban 三配置文件标识：表定义 / bean 定义 / 枚举定义
        /// <para>Luban config workbooks: table / bean / enum definitions.</para>
        /// </summary>
        public enum LubanConfigFile
        {
            /// <summary>表定义 __tables__.xlsx</summary>
            Tables,
            /// <summary>bean 定义 __beans__.xlsx</summary>
            Beans,
            /// <summary>枚举定义 __enums__.xlsx</summary>
            Enums
        }

        // 配置文件标识 → 文件名
        private static string ResolveFileName(LubanConfigFile file) => file switch
        {
            LubanConfigFile.Beans => BeansFileName,
            LubanConfigFile.Enums => EnumsFileName,
            _ => TablesFileName
        };

        // __beans__.xlsx 字段区列位（字段区从 J 列开始，与 B 列的 bean 全名并存于同一工作簿）
        private const int ColBeanFieldName = 10;  // J：字段名
        private const int ColBeanFieldType = 12;  // L：字段类型

        // 表头说明行行数（1-based）：第 1 行列名、第 2 行填写说明、第 3 行示例，数据从第 4 行起
        // 表头行不参与增删查，避免误伤列名与说明文字
        private const int HeaderRowCount = 3;

        // __tables__.xlsx 表定义行列位（1-based；A 列为 ## 标记列，数据行留空不写）。
        // 列位是 Luban 的隐性契约，以既有 L10n.TbText_en/zh 行实测为准，勿凭猜测调整

        /// <summary>
        /// B 列：表全名（包含模块与名字），如 L10n.TbText_zh
        /// <para>Column B: the full table name including module, e.g. L10n.TbText_zh.</para>
        /// </summary>
        public const int ColFullName = 2;

        /// <summary>
        /// C 列：记录类（bean）名，L10n 全部表共用 L10n.Text
        /// <para>Column C: the record (bean) type name; all L10n tables share L10n.Text.</para>
        /// </summary>
        public const int ColBeanType = 3;

        /// <summary>
        /// D 列：是否从 Excel 读取定义，0 = 取已有 bean 定义
        /// <para>Column D: whether to read the schema from Excel; 0 means using the existing bean definition.</para>
        /// </summary>
        public const int ColReadSchema = 4;

        /// <summary>
        /// E 列：数据源文件名，如 L10n_zh.csv
        /// <para>Column E: the data source file name, e.g. L10n_zh.csv.</para>
        /// </summary>
        public const int ColInput = 5;

        /// <summary>
        /// F 列：主键字段名，L10n 表固定为 key
        /// <para>Column F: the index (key) field name; fixed to key for L10n tables.</para>
        /// </summary>
        public const int ColIndex = 6;

        /// <summary>
        /// G 列：表模式，L10n 表固定为 map
        /// <para>Column G: the table mode; fixed to map for L10n tables.</para>
        /// </summary>
        public const int ColMode = 7;

        /// <summary>
        /// H 列：分组，c = client
        /// <para>Column H: the group; c stands for client.</para>
        /// </summary>
        public const int ColGroup = 8;

        /// <summary>
        /// I 列：注释（给人看，不参与导表逻辑）
        /// <para>Column I: the comment (for humans only, not used by the pipeline).</para>
        /// </summary>
        public const int ColComment = 9;
        
        /// <summary>
        /// 把一个数据增加到指定列的尾部
        /// <para>Append a data item to the end of a specified column.</para>
        /// </summary>
        /// <param name="columnIndex">列号（1-based，A 列 = 1）<para>Column index (1-based, A = 1).</para></param>
        /// <param name="value">单元格值：int 写为数字，其余写为文本<para>Cell value: int as a number, anything else as text.</para></param>
        /// <returns>true = 追加成功；false = 数据不正确或写入失败<para>true on success; false when the data is invalid or the write failed.</para></returns>
        public static bool AppendToColumn(int columnIndex, object value)
            => AppendLastRow(new Dictionary<int, object> { [columnIndex] = value }, columnIndex);

        /// <summary>
        /// 增加一行数据至尾部
        /// <para>Add a row of data to the end.</para>
        /// </summary>
        /// <param name="cells">列号（1-based）→ 单元格值：int 写为数字，其余写为文本<para>Column index (1-based) → cell value: int as a number, anything else as text.</para></param>
        /// <param name="uniqueColumn">唯一性判定列号，必须包含在 cells 中<para>Uniqueness column index, must be one of the cells.</para></param>
        /// <param name="rowNumber">目标行号：0 = 自动紧贴最后一个有内容的行（空行骨架不参与定位），&gt;0 = 指定行号（已有内容时失败）<para>Target row: 0 = auto after the last content row, &gt;0 = explicit row (fails if it already has content).</para></param>
        /// <returns>true = 追加成功；false = 数据不正确或写入失败<para>true on success; false when the data is invalid or the write failed.</para></returns>
        public static bool AppendLastRow(IReadOnlyDictionary<int, object> cells, int uniqueColumn, int rowNumber = 0)
        {
            // 数据检测：字典与列号
            if (cells == null || cells.Count == 0)
            {
                D.Error(LC.Tr("[ XlsxRowAppender ] 追加失败：没有可写入的数据",
                    "[ XlsxRowAppender ] Append failed: no data to write"));
                return false;
            }

            if (cells.Keys.Any(c => c < 1))
            {
                D.Error(LC.Tr("[ XlsxRowAppender ] 追加失败：列号必须从 1 开始（A 列 = 1）",
                    "[ XlsxRowAppender ] Append failed: column index must start at 1 (A = 1)"));
                return false;
            }

            if (uniqueColumn < 1 || !cells.ContainsKey(uniqueColumn))
            {
                D.Error(LC.Tr("[ XlsxRowAppender ] 追加失败：唯一性判定列 {0} 不在待写入数据中",
                    "[ XlsxRowAppender ] Append failed: uniqueness column {0} is not among the cells to write",
                    uniqueColumn));
                return false;
            }

            // 数据检测：值非空且类型可写
            foreach (var pair in cells)
            {
                if (pair.Value == null || pair.Value.ToString().Length == 0)
                {
                    D.Error(LC.Tr("[ XlsxRowAppender ] 追加失败：第 {0} 列的值为空",
                    "[ XlsxRowAppender ] Append failed: value of column {0} is empty",
                    pair.Key));
                    return false;
                }

                if (pair.Value is int or string) continue;
                D.Error(LC.Tr("[ XlsxRowAppender ] 追加失败：第 {0} 列的值类型 {1} 不支持（仅 int / string）",
                    "[ XlsxRowAppender ] Append failed: value type {1} of column {0} is not supported (int / string only)",
                    pair.Key, pair.Value.GetType().Name));
                return false;
            }

            if (!LoadWorkbook(TablesFileName, out var context)) return false;

            // 唯一性检测：判定列的值已存在 = 数据重复
            var keyText = cells[uniqueColumn].ToString();
            if (ColumnContains(context, uniqueColumn, keyText))
            {
                D.Error(LC.Tr("[ XlsxRowAppender ] 追加失败：第 {0} 列已存在相同数据「{1}」（重复追加）",
                    "[ XlsxRowAppender ] Append failed: column {0} already contains the same value '{1}' (duplicate append)",
                    uniqueColumn, keyText));
                return false;
            }

            return WriteAppend(context, cells, rowNumber);
        }

        /// <summary>
        /// 检测指定列中是否已存在某个数据
        /// <para>Check whether a value already exists in the specified column.</para>
        /// </summary>
        /// <param name="columnIndex">列号（1-based，A 列 = 1）<para>Column index (1-based, A = 1).</para></param>
        /// <param name="value">要检测的数据：int 按数字比对，其余按文本比对<para>Value to check: int compares numerically, others as text.</para></param>
        /// <returns>true = 已存在；false = 不存在或检测失败（原因见 Console）<para>true when present; false when absent or the check failed (see Console).</para></returns>
        public static bool ContainsColumn(int columnIndex, object value)
        {
            if (columnIndex < 1)
            {
                D.Error(LC.Tr("[ XlsxRowAppender ] 检测失败：列号必须从 1 开始（A 列 = 1）",
                    "[ XlsxRowAppender ] Check failed: column index must start at 1 (A = 1)"));
                return false;
            }

            if (value == null || value.ToString().Length == 0)
            {
                D.Error(LC.Tr("[ XlsxRowAppender ] 检测失败：检测内容为空",
                    "[ XlsxRowAppender ] Check failed: value to check is empty"));
                return false;
            }

            if (!LoadWorkbook(TablesFileName, out var context)) return false;

            return ColumnContains(context, columnIndex, value.ToString());
        }

        /// <summary>
        /// 检测 Luban 表定义工作簿（__tables__.xlsx）是否存在
        /// <para>Checks whether the Luban table-definition workbook (__tables__.xlsx) exists.</para>
        /// </summary>
        /// <returns>true = 文件存在；false = 不存在（未配置数据表的工程属正常状态，不报错）<para>true when the file exists; false otherwise (normal for projects without tables, no error logged).</para></returns>
        public static bool Exists()
            => File.Exists(ResolveTablesPath());

        /// <summary>
        /// 删除工作簿中「指定列的值等于指定文本」的那一行（整行删除）
        /// <para>Deletes the row whose value in the given column equals the given text (the whole row is removed).</para>
        /// </summary>
        /// <remarks>
        /// 用于「配置项被删除后同步清理」的场景：如多语言面板里某语言被移除后，
        /// 同步时把 __tables__.xlsx 中对应的表定义行一并删掉，避免留下指向已不存在 CSV 的僵尸表。
        /// <para>Used to clean up table definitions whose source data was removed
        /// (e.g. a language deleted in the localization panel), so no zombie table remains
        /// pointing at a missing CSV.</para>
        /// <para>只删数据行：表头说明行（行号 &lt;= 3，含列名与填写说明）永不参与匹配。</para>
        /// <para>Only data rows are matched: header rows (row number &lt;= 3) are never candidates.</para>
        /// </remarks>
        /// <param name="columnIndex">列号（1-based，A 列 = 1）<para>Column index (1-based, A = 1).</para></param>
        /// <param name="value">要匹配的值（按文本比对，与 <see cref="ContainsColumn"/> 一致）<para>Value to match (compared as text, same as ContainsColumn).</para></param>
        /// <returns>实际删除的行数（0 = 无匹配行，属正常情况）<para>Number of rows actually removed (0 when nothing matched, which is normal).</para></returns>
        public static int RemoveRowsByColumnValue(int columnIndex, string value)
        {
            if (columnIndex < 1)
            {
                D.Error(LC.Tr("[ XlsxRowAppender ] 删除失败：列号必须从 1 开始（A 列 = 1）",
                    "[ XlsxRowAppender ] Remove failed: column index must start at 1 (A = 1)"));
                return 0;
            }

            if (string.IsNullOrEmpty(value))
            {
                D.Error(LC.Tr("[ XlsxRowAppender ] 删除失败：匹配内容为空",
                    "[ XlsxRowAppender ] Remove failed: value to match is empty"));
                return 0;
            }

            if (!LoadWorkbook(TablesFileName, out var context)) return 0;

            try
            {
                int removed = RemoveRowsFromContext(context, columnIndex, value);
                if (removed == 0) return 0;

                return WriteWorkbook(context) ? removed : 0;
            }
            catch (Exception e)
            {
                D.Error(LC.Tr("[ XlsxRowAppender ] 删除失败：{0}",
                    "[ XlsxRowAppender ] Remove failed: {0}",
                    e.Message));
                var tmpPath = context.Path + ".tmp";
                if (File.Exists(tmpPath)) File.Delete(tmpPath);
                return 0;
            }
        }

        /// <summary>
        /// 读取指定列中所有非空值（去重，保持出现顺序；跳过表头说明行）
        /// <para>Reads all non-empty values of a column (deduplicated, in order of appearance; header rows skipped).</para>
        /// </summary>
        /// <param name="columnIndex">列号（1-based，A 列 = 1）<para>Column index (1-based, A = 1).</para></param>
        /// <returns>该列的所有值；失败返回空列表（原因见 Console）<para>All values in the column; empty on failure (reason logged).</para></returns>
        public static List<string> GetColumnValues(int columnIndex)
        {
            var result = new List<string>();

            if (columnIndex < 1)
            {

                D.Error(LC.Tr("[ XlsxRowAppender ] 读取失败：列号必须从 1 开始（A 列 = 1）",
                    "[XlsxRowAppender] Read failed: Column numbers must start from 1 (column A = 1)"));
                return result;
            }

            if (!LoadWorkbook(TablesFileName, out var context)) return result;

            var sheetData = context?.Sheet?.Root?.Element(context.SheetNs + "sheetData");
            if (sheetData == null) return result;

            var target = ToColumnName(columnIndex);
            var seen = new HashSet<string>(StringComparer.Ordinal);

            foreach (var row in sheetData.Elements(context.SheetNs + "row"))
            {
                // 跳过表头说明行（列名行 + 填写说明行）
                if (GetRowNumber(row) <= HeaderRowCount) continue;

                foreach (var cell in row.Elements(context.SheetNs + "c"))
                {
                    if (CellColumn((string)cell.Attribute("r")) != target) continue;

                    var text = CellText(cell, context.SharedTexts);
                    if (string.IsNullOrEmpty(text)) continue;
                    if (!seen.Add(text)) continue;

                    result.Add(text);
                }
            }

            return result;
        }

        /// <summary>
        /// 按数据源文件查找对应的表全名（读 __tables__.xlsx 的 E列 input 与 B列 full_name 配对）。
        /// <para>Find table full names by data source file (pairs column E input with column B full_name).</para>
        /// </summary>
        /// <param name="inputFile">数据源：相对路径或纯文件名均可<para>Data source: relative path or bare file name.</para></param>
        /// <returns>匹配的表全名列表（一个文件可对应多表）<para>Matched table full names (one file may map to several tables).</para></returns>
        public static List<string> GetTableNamesByInput(string inputFile)
        {
            var result = new List<string>();
            if (string.IsNullOrWhiteSpace(inputFile)) return result;
            if (!LoadWorkbook(TablesFileName, out var context)) return result;

            var sheetData = context?.Sheet?.Root?.Element(context.SheetNs + "sheetData");
            if (sheetData == null) return result;

            var fullCol = ToColumnName(ColFullName);
            var inputCol = ToColumnName(ColInput);
            var normalizedFile = inputFile.Replace('\\', '/');
            var fileName = Path.GetFileName(normalizedFile);

            foreach (var row in sheetData.Elements(context.SheetNs + "row"))
            {
                if (GetRowNumber(row) <= HeaderRowCount) continue;

                string fullName = null;
                string input = null;
                foreach (var cell in row.Elements(context.SheetNs + "c"))
                {
                    var col = CellColumn((string)cell.Attribute("r"));
                    if (col != fullCol && col != inputCol) continue;
                    var text = CellText(cell, context.SharedTexts);
                    if (string.IsNullOrEmpty(text)) continue;
                    if (col == fullCol) fullName = text;
                    else input = text;
                }

                if (string.IsNullOrEmpty(fullName) || string.IsNullOrEmpty(input)) continue;

                // E 列允许多个文件以逗号分隔，逐项比对完整路径或文件名
                var sources = input.Split(',');
                bool matched = false;
                foreach (var source in sources)
                {
                    var trimmed = source.Trim().Replace('\\', '/');
                    if (string.Equals(trimmed, normalizedFile, StringComparison.OrdinalIgnoreCase)
                        || string.Equals(Path.GetFileName(trimmed), fileName, StringComparison.OrdinalIgnoreCase))
                    {
                        matched = true;
                        break;
                    }
                }

                if (matched && !result.Contains(fullName))
                    result.Add(fullName);
            }

            return result;
        }

        /// <summary>
        /// 生成只保留指定表定义的 __tables__.xlsx 副本（原文件不动）。
        /// 删除所有表全名不在 keepFullNames 中的数据行，表头行保留；用于单表生成时绕过其它坏表定义。
        /// <para>Create a filtered copy of __tables__.xlsx keeping only the given tables (original untouched).</para>
        /// </summary>
        /// <param name="keepFullNames">要保留的表全名列表<para>Table full names to keep</para></param>
        /// <param name="targetPath">副本输出路径<para>Output path for the filtered copy</para></param>
        /// <returns>true = 生成成功<para>true on success</para></returns>
        public static bool CreateFilteredTablesWorkbook(IReadOnlyList<string> keepFullNames, string targetPath)
        {
            if (keepFullNames == null || keepFullNames.Count == 0)
            {
                D.Error(LC.Tr("[ XlsxRowAppender ] 精简失败：没有指定要保留的表",
                    "[ XlsxRowAppender ] Filter failed: no tables specified to keep"));
                return false;
            }

            if (!LoadWorkbook(TablesFileName, out var context)) return false;

            try
            {
                var sheetData = context.Sheet.Root.Element(context.SheetNs + "sheetData");
                if (sheetData == null) return false;

                var fullCol = ToColumnName(ColFullName);
                var rows = sheetData.Elements(context.SheetNs + "row").ToList();
                int removed = 0;
                foreach (var row in rows)
                {
                    if (GetRowNumber(row) <= HeaderRowCount) continue;

                    string fullName = null;
                    foreach (var cell in row.Elements(context.SheetNs + "c"))
                    {
                        if (CellColumn((string)cell.Attribute("r")) != fullCol) continue;
                        fullName = CellText(cell, context.SharedTexts);
                        break;
                    }

                    // 无表全名的行（空壳/注释）保留，避免误删结构行；有名字但不在保留集的删除
                    if (string.IsNullOrEmpty(fullName)) continue;
                    if (keepFullNames.Contains(fullName)) continue;

                    row.Remove();
                    removed++;
                }

                //D.Log($"[ XlsxRowAppender ] 精简表定义：移除 {removed} 行，保留 {keepFullNames.Count} 张表 → {targetPath}");
                return WriteWorkbook(context, targetPath);
            }
            catch (Exception e)
            {
                D.Error(LC.Tr("[ XlsxRowAppender ] 精简表定义失败：{0}",
                    "[ XlsxRowAppender ] Failed to filter table definitions: {0}", e.Message));
                return false;
            }
        }

        /// <summary>
        /// 登记 Luban bean 定义（写入 __beans__.xlsx）
        /// <para>Registers a Luban bean definition into __beans__.xlsx.</para>
        /// <para>幂等（Ensure 语义）：bean 全名已存在时跳过并返回 true。字段按 Luban 约定分行：
        /// 首字段与 bean 名同行，后续字段行 full_name 留空。</para>
        /// </summary>
        /// <param name="beanFullName">bean 全名（包含模块），如 L10n.Text<para>Full bean name including module, e.g. L10n.Text.</para></param>
        /// <param name="rowNumber">目标起始行号：0 = 自动紧贴最后一个有内容的行（空行骨架不参与定位），&gt;0 = 指定起始行（区间已有内容时失败）<para>Start row: 0 = auto after the last content row, &gt;0 = explicit start row (fails if the range has content).</para></param>
        /// <param name="fields">字段列表（字段名, 类型），按声明顺序写入<para>Fields as (name, type) pairs, written in declaration order.</para></param>
        /// <returns>true = 已存在或登记成功；false = 数据不正确或写入失败（原因已 LogError）<para>true when present or registered; false when the data is invalid or the write failed (reason logged).</para></returns>
        public static bool AppendBean(string beanFullName, int rowNumber, params (string name, string type)[] fields)
        {
            // 数据检测：bean 名与字段
            if (string.IsNullOrWhiteSpace(beanFullName))
            {
                D.Error(LC.Tr("[ XlsxRowAppender ] bean 登记失败：bean 全名为空",
                    "[ XlsxRowAppender ] Bean registration failed: bean full name is empty"));
                return false;
            }

            if (fields == null || fields.Length == 0)
            {
                D.Error(LC.Tr("[ XlsxRowAppender ] bean 登记失败：{0} 没有字段",
                    "[ XlsxRowAppender ] Bean registration failed: {0} has no fields",
                    beanFullName));
                return false;
            }

            foreach (var field in fields)
            {
                if (string.IsNullOrWhiteSpace(field.name) || string.IsNullOrWhiteSpace(field.type))
                {
                    D.Error(LC.Tr("[ XlsxRowAppender ] bean 登记失败：{0} 存在空字段名或空类型",
                    "[ XlsxRowAppender ] Bean registration failed: {0} has an empty field name or type",
                    beanFullName));
                    return false;
                }
            }

            if (!LoadWorkbook(BeansFileName, out var context)) return false;

            // 幂等：bean 已存在直接跳过（Ensure 语义，不视为错误）
            if (ColumnContains(context, ColFullName, beanFullName)) return true;

            try
            {
                // Luban 约定：首字段与 bean 名同行，后续字段行 full_name 留空（多字段分行）
                var first = fields[0];
                AppendRowToContext(context, new Dictionary<int, object>
                {
                    [ColFullName] = beanFullName,
                    [ColBeanFieldName] = first.name,
                    [ColBeanFieldType] = first.type,
                }, rowNumber);

                for (var i = 1; i < fields.Length; i++)
                {
                    AppendRowToContext(context, new Dictionary<int, object>
                    {
                        [ColBeanFieldName] = fields[i].name,
                        [ColBeanFieldType] = fields[i].type,
                    }, rowNumber > 0 ? rowNumber + i : 0);
                }

                return WriteWorkbook(context);
            }
            catch (Exception e)
            {
                D.Error(LC.Tr("[ XlsxRowAppender ] bean 登记失败：{0}",
                    "[ XlsxRowAppender ] Bean registration failed: {0}",
                    e.Message));
                var tmpPath = context.Path + ".tmp";
                if (File.Exists(tmpPath)) File.Delete(tmpPath);
                return false;
            }
        }

        #region 三文件通用 CRUD（Tables / Beans / Enums）

        /// <summary>
        ///  Luban 配置文件是否存在
        /// <para>Whether the given Luban config workbook exists.</para>
        /// </summary>
        /// <param name="file">配置文件类型<para>Config file kind</para></param>
        public static bool Exists(LubanConfigFile file)
            => File.Exists(Path.Combine(Path.GetDirectoryName(ResolveTablesPath()) ?? ".", ResolveFileName(file)));

        /// <summary>
        /// 在指定配置文件中查找某列是否包含某值（表头行不参与）
        /// <para>Check whether a column in the given workbook contains a value (header rows excluded).</para>
        /// </summary>
        /// <param name="file">配置文件类型<para>Config file kind</para></param>
        /// <param name="columnIndex">列号（1-based）<para>Column index (1-based)</para></param>
        /// <param name="value">匹配值<para>Value to match</para></param>
        public static bool ContainsValue(LubanConfigFile file, int columnIndex, string value)
        {
            if (columnIndex < 1 || string.IsNullOrEmpty(value)) return false;
            if (!LoadWorkbook(ResolveFileName(file), out var context)) return false;
            return ColumnContains(context, columnIndex, value);
        }

        /// <summary>
        /// 读取指定配置文件某列的所有非空值（去重、保序、跳过表头）
        /// <para>Read all non-empty values of a column (deduplicated, ordered, header skipped).</para>
        /// </summary>
        /// <param name="file">配置文件类型<para>Config file kind</para></param>
        /// <param name="columnIndex">列号（1-based）<para>Column index (1-based)</para></param>
        public static List<string> ReadColumn(LubanConfigFile file, int columnIndex)
        {
            var result = new List<string>();
            if (columnIndex < 1 || !LoadWorkbook(ResolveFileName(file), out var context)) return result;

            var sheetData = context?.Sheet?.Root?.Element(context.SheetNs + "sheetData");
            if (sheetData == null) return result;

            var target = ToColumnName(columnIndex);
            var seen = new HashSet<string>(StringComparer.Ordinal);
            foreach (var row in sheetData.Elements(context.SheetNs + "row"))
            {
                if (GetRowNumber(row) <= HeaderRowCount) continue;
                foreach (var cell in row.Elements(context.SheetNs + "c"))
                {
                    if (CellColumn((string)cell.Attribute("r")) != target) continue;
                    var text = CellText(cell, context.SharedTexts);
                    if (string.IsNullOrEmpty(text) || !seen.Add(text)) continue;
                    result.Add(text);
                }
            }
            return result;
        }

        /// <summary>
        /// 在指定配置文件追加一行（列号→值；uniqueColumn 用于查重，rowNumber=0 自动定位末行）
        /// <para>Append a row to the given workbook.</para>
        /// </summary>
        /// <param name="file">配置文件类型<para>Config file kind</para></param>
        /// <param name="cells">列号→值<para>Column index to value</para></param>
        /// <param name="uniqueColumn">查重列<para>Uniqueness column</para></param>
        /// <param name="rowNumber">目标行号，0=自动<para>Target row, 0 = auto</para></param>
        public static bool AppendRow(LubanConfigFile file, IReadOnlyDictionary<int, object> cells,
            int uniqueColumn, int rowNumber = 0)
        {
            if (cells == null || cells.Count == 0) return false;
            if (uniqueColumn < 1 || !cells.ContainsKey(uniqueColumn)) return false;
            foreach (var pair in cells)
                if (pair.Value == null || !(pair.Value is int or string)) return false;

            if (!LoadWorkbook(ResolveFileName(file), out var context)) return false;
            if (ColumnContains(context, uniqueColumn, cells[uniqueColumn].ToString()))
            {
                D.Warning(LC.Tr("[ XlsxRowAppender ] 追加失败：第 {0} 列已存在「{1}」",
                    "[ XlsxRowAppender ] Append failed: column {0} already contains '{1}'",
                    uniqueColumn, cells[uniqueColumn]));
                return false;
            }
            return WriteAppend(context, cells, rowNumber);
        }

        /// <summary>
        /// 删除指定配置文件中「某列值等于给定值」的所有数据行（表头不参与）
        /// <para>Delete rows whose column value matches, in the given workbook (header excluded).</para>
        /// </summary>
        /// <param name="file">配置文件类型<para>Config file kind</para></param>
        /// <param name="columnIndex">列号（1-based）<para>Column index (1-based)</para></param>
        /// <param name="value">匹配值<para>Value to match</para></param>
        /// <returns>删除行数<para>Number of rows removed</para></returns>
        public static int RemoveRows(LubanConfigFile file, int columnIndex, string value)
        {
            if (columnIndex < 1 || string.IsNullOrEmpty(value)) return 0;
            if (!LoadWorkbook(ResolveFileName(file), out var context)) return 0;
            try
            {
                int removed = RemoveRowsFromContext(context, columnIndex, value);
                return removed > 0 && WriteWorkbook(context) ? removed : 0;
            }
            catch (Exception e)
            {
                D.Error(LC.Tr("[ XlsxRowAppender ] 删除失败：{0}", "[ XlsxRowAppender ] Remove failed: {0}", e.Message));
                var tmp = context.Path + ".tmp";
                if (File.Exists(tmp)) File.Delete(tmp);
                return 0;
            }
        }

        /// <summary>
        /// 统计指定配置文件的数据行数（跳过表头）
        /// <para>Count data rows in the given workbook (header excluded).</para>
        /// </summary>
        /// <param name="file">配置文件类型<para>Config file kind</para></param>
        public static int CountDataRows(LubanConfigFile file)
        {
            if (!LoadWorkbook(ResolveFileName(file), out var context)) return 0;
            var sheetData = context?.Sheet?.Root?.Element(context.SheetNs + "sheetData");
            if (sheetData == null) return 0;
            int count = 0;
            foreach (var row in sheetData.Elements(context.SheetNs + "row"))
                if (GetRowNumber(row) > HeaderRowCount && RowHasContent(row, context.SheetNs)) count++;
            return count;
        }

        #endregion

        // 解析 Luban 表定义工作簿绝对路径：统一走 DatableConfig.ResolveSourcePath()（相对值按工程根归一化、目录必须存在），否则回落默认数据源目录
        private static string ResolveTablesPath()
        {
            if (EditorUtils.CheckAssets<DatableConfig>(out var configPath))
            {
                var config = EditorUtils.LoadSettingAtPath<DatableConfig>(configPath);
                if (config != null)
                {
                    var directory = config.ResolveSourcePath();
                    if (Directory.Exists(directory))
                        return Path.Combine(directory, TablesFileName);
                }
            }

            return Path.Combine(DatableConfigPanel.ExcelFolderPath, "Datas", TablesFileName);
        }

        // 定位并解析工作簿（路径定位 + zip 读取 + 工作表与共享字符串解析），任何一步失败 LogError 并返回 false
        private static bool LoadWorkbook(string fileName, out WorkbookContext context)
        {
            context = null;

            var path = Path.Combine(Path.GetDirectoryName(ResolveTablesPath()) ?? ".", fileName);
            if (!File.Exists(path))
            {
                D.Error(LC.Tr("[ XlsxRowAppender ] 找不到表定义文件：{0}",
                    "[ XlsxRowAppender ] Table definition file not found: {0}",
                    path));
                return false;
            }

            try
            {
                var entries = new Dictionary<string, byte[]>(StringComparer.OrdinalIgnoreCase);
                using (var archive = ZipFile.OpenRead(path))
                {
                    foreach (var entry in archive.Entries)
                    {
                        using var stream = entry.Open();
                        using var buffer = new MemoryStream();
                        stream.CopyTo(buffer);
                        entries[entry.FullName] = buffer.ToArray();
                    }
                }

                // 经 workbook.xml → r:id → rels 定位第一个工作表条目
                if (!entries.TryGetValue("xl/workbook.xml", out var workbookBytes))
                {
                    D.Error(LC.Tr("[ XlsxRowAppender ] 缺少 xl/workbook.xml，不是有效的 xlsx 文件",
                    "[ XlsxRowAppender ] Missing xl/workbook.xml, not a valid xlsx file"));
                    return false;
                }

                var officeRns = XNamespace.Get("http://schemas.openxmlformats.org/officeDocument/2006/relationships");
                var workbook = XDocument.Parse(Encoding.UTF8.GetString(workbookBytes));
                var mainNs = workbook.Root.Name.Namespace;
                var sheet = workbook.Root.Element(mainNs + "sheets")?.Elements(mainNs + "sheet").FirstOrDefault();
                if (sheet == null)
                {
                    D.Error(LC.Tr("[ XlsxRowAppender ] workbook.xml 中没有工作表",
                    "[ XlsxRowAppender ] No worksheet found in workbook.xml"));
                    return false;
                }

                var rid = (string)sheet.Attribute(officeRns + "id");
                if (!entries.TryGetValue("xl/_rels/workbook.xml.rels", out var relsBytes))
                {
                    D.Error(LC.Tr("[ XlsxRowAppender ] 缺少 xl/_rels/workbook.xml.rels",
                    "[ XlsxRowAppender ] Missing xl/_rels/workbook.xml.rels"));
                    return false;
                }

                var packageRns = XNamespace.Get("http://schemas.openxmlformats.org/package/2006/relationships");
                var xElement = XDocument.Parse(Encoding.UTF8.GetString(relsBytes)).Root;
                if (xElement == null)
                {
                    D.Error(LC.Tr("[ XlsxRowAppender ] workbook.xml.rels 根节点为空，无法解析",
                        "[ XlsxRowAppender ] workbook.xml.rels root is null, parse failed"));
                    return false;
                }

                var relationship = xElement.Elements(packageRns + "Relationship")
                    .FirstOrDefault(r => (string)r.Attribute("Id") == rid);
                if (relationship == null)
                {
                    D.Error(LC.Tr("[ XlsxRowAppender ] workbook.xml.rels 中找不到 rId：{0}",
                    "[ XlsxRowAppender ] rId not found in workbook.xml.rels: {0}",
                    rid));
                    return false;
                }

                var target = ((string)relationship.Attribute("Target") ?? string.Empty).Replace('\\', '/')
                    .TrimStart('/');
                var sheetEntryName = target.StartsWith("xl/", StringComparison.OrdinalIgnoreCase)
                    ? target
                    : "xl/" + target;

                var sharedStrings = entries.TryGetValue("xl/sharedStrings.xml", out var sharedBytes)
                    ? XDocument.Parse(Encoding.UTF8.GetString(sharedBytes))
                    : null;
                var sharedNs = sharedStrings?.Root.Name.Namespace;
                var sharedTexts = sharedStrings == null
                    ? new List<string>()
                    : sharedStrings.Root.Elements(sharedNs + "si")
                        .Select(si => string.Concat(si.Descendants(sharedNs + "t").Select(t => t.Value)))
                        .ToList();

                context = new WorkbookContext
                {
                    Path = path,
                    Entries = entries,
                    SheetEntryName = sheetEntryName,
                    Sheet = XDocument.Parse(Encoding.UTF8.GetString(entries[sheetEntryName])),
                    SharedStrings = sharedStrings,
                    SharedTexts = sharedTexts,
                };
                context.SheetNs = context.Sheet.Root.Name.Namespace;
                context.SharedNs = sharedNs;

                return true;
            }
            catch (Exception e)
            {
                D.Error(LC.Tr("[ XlsxRowAppender ] 解析工作簿失败：{0}",
                    "[ XlsxRowAppender ] Failed to parse the workbook: {0}",
                    e.Message));
                return false;
            }
        }

        // 判定指定列（1-based）是否已存在某文本（共享字符串/内联/数值单元格统一转文本比对）
        private static bool ColumnContains(WorkbookContext context, int columnIndex, string text)
        {
            var target = ToColumnName(columnIndex);
            var sheetData = context?.Sheet?.Root?.Element(context.SheetNs + "sheetData");
            if (sheetData == null) return false;

            return sheetData.Elements(context.SheetNs + "row")
                .SelectMany(row => row.Elements(context.SheetNs + "c"))
                .Where(cell => CellColumn((string)cell.Attribute("r")) == target)
                .Select(cell => CellText(cell, context.SharedTexts))
                .Any(existing => existing == text);
        }

        // 追加一行并写回（AppendLastRow 的执行段）
        private static bool WriteAppend(WorkbookContext context, IReadOnlyDictionary<int, object> cells, int rowNumber)
        {
            try
            {
                AppendRowToContext(context, cells, rowNumber);
                return WriteWorkbook(context);
            }
            catch (Exception e)
            {
                D.Error(LC.Tr("[ XlsxRowAppender ] 写入工作簿失败：{0}",
                    "[ XlsxRowAppender ] Failed to write the workbook: {0}",
                    e.Message));
                var tmpPath = context.Path + ".tmp";
                if (File.Exists(tmpPath)) File.Delete(tmpPath);
                return false;
            }
        }

        // 删除匹配指定列值的那一行（整行移除，含该行所有单元格；共享字符串保留不清理，
        // 避免重排索引导致其它行的引用错位——Excel 允许共享字符串表有未引用项）
        // 表头说明行不参与匹配（防止误删列名行）
        private static int RemoveRowsFromContext(WorkbookContext context, int columnIndex, string value)
        {
            var sheetData = context?.Sheet?.Root?.Element(context.SheetNs + "sheetData");
            if (sheetData == null) return 0;

            var target = ToColumnName(columnIndex);
            int removed = 0;

            // 倒序遍历：删除会改变集合，直接遍历会漏行
            var rows = sheetData.Elements(context.SheetNs + "row").ToList();
            for (int i = rows.Count - 1; i >= 0; i--)
            {
                var row = rows[i];
                if (GetRowNumber(row) <= HeaderRowCount) continue;

                bool matched = row.Elements(context.SheetNs + "c").Any(cell =>
                {
                    if (CellColumn((string)cell.Attribute("r")) != target) return false;
                    return CellText(cell, context.SharedTexts) == value;
                });

                if (!matched) continue;

                row.Remove();
                removed++;
            }

            return removed;
        }

        // 取行元素的行号（r 属性缺失时按 0 处理）
        private static int GetRowNumber(XElement row)
            => (int?)row.Attribute("r") ?? 0;

        // 组装一行并写入 sheetData（含共享字符串登记）；写回由 WriteWorkbook 统一执行。
        // rowNumber <= 0：自动定位到最后一个有内容的行之后（空行骨架不参与定位，新行紧贴真实数据）；
        // rowNumber > 0：写入指定行号（该行已有内容时抛异常，由调用方处理）。
        // 插入位置按行号升序，避免乱序 row 导致 Excel / Luban 读取异常。
        private static void AppendRowToContext(WorkbookContext context, IReadOnlyDictionary<int, object> cells, int rowNumber = 0)
        {
            var sheetData = context?.Sheet?.Root?.Element(context.SheetNs + "sheetData");
            if (sheetData == null)
            {
                sheetData = new XElement(context.SheetNs + "sheetData");
                context?.Sheet?.Root?.Add(sheetData);
            }

            var rows = sheetData.Elements(context.SheetNs + "row").ToList();

            int newRow;
            XElement insertBefore;
            if (rowNumber > 0)
            {
                newRow = rowNumber;

                // 指定行号：该行已有内容 = 覆盖风险，直接失败
                var existing = rows.FirstOrDefault(r => (int?)r.Attribute("r") == rowNumber);
                if (existing != null && RowHasContent(existing, context.SheetNs))
                    throw new InvalidOperationException(LC.Tr($"指定行 {rowNumber} 已有内容，拒绝覆盖",
                        $"The specified row {rowNumber} already contains content and cannot be overwritten."));

                insertBefore = rows.FirstOrDefault(r => ((int?)r.Attribute("r") ?? int.MaxValue) > rowNumber);
                existing?.Remove();
            }
            else
            {
                // 自动定位：最后一个有内容的行之后（空行骨架不参与定位）
                newRow = rows.Where(r => RowHasContent(r, context.SheetNs))
                    .Select(r => (int?)r.Attribute("r"))
                    .Max() ?? 0;
                newRow++;

                // 目标行号若存在空骨架（同号空 row），移除避免产生重复行号的 row 元素
                rows.FirstOrDefault(r => (int?)r.Attribute("r") == newRow && !RowHasContent(r, context.SheetNs))
                    ?.Remove();

                insertBefore = rows.FirstOrDefault(r => ((int?)r.Attribute("r") ?? int.MaxValue) > newRow);
            }

            // 组装行元素：int 写数字单元格，string 写共享字符串单元格（查重复用索引）；空值跳过（稀疏行）
            var rowElement = new XElement(context.SheetNs + "row", new XAttribute("r", newRow));
            foreach (var column in cells.Keys.OrderBy(c => c))
            {
                var cellRef = ToColumnName(column) + newRow;
                if (cells[column] is int number)
                {
                    rowElement.Add(new XElement(context.SheetNs + "c",
                        new XAttribute("r", cellRef),
                        new XElement(context.SheetNs + "v", number)));
                    continue;
                }

                var text = cells[column].ToString();
                var index = context.SharedTexts.IndexOf(text);
                if (index < 0)
                {
                    index = context.SharedTexts.Count;
                    context.SharedTexts.Add(text);
                    context.SharedStrings?.Root?.Add(
                        new XElement(context.SharedNs + "si", new XElement(context.SharedNs + "t", text)));
                }

                rowElement.Add(new XElement(context.SheetNs + "c",
                    new XAttribute("r", cellRef),
                    new XAttribute("t", "s"),
                    new XElement(context.SheetNs + "v", index)));
            }

            // 按行号升序插入，保持 row 元素有序
            if (insertBefore != null) insertBefore.AddBeforeSelf(rowElement);
            else sheetData.Add(rowElement);
        }

        // 行是否含有内容的单元格（v 非空或内联文本；纯样式空壳行不算）
        private static bool RowHasContent(XElement row, XNamespace ns)
            => row.Elements(ns + "c").Any(c =>
                   (c.Element(ns + "v") != null && !string.IsNullOrEmpty(c.Element(ns + "v").Value))
                   || c.Element(ns + "is") != null);

        // 更新共享字符串计数并写回工作簿（临时文件 → 原子替换，不做备份）
        // targetPath 为空时写回 context.Path；否则写到指定路径（用于生成精简副本，不动原文件）
        private static bool WriteWorkbook(WorkbookContext context, string targetPath = null)
        {
            string dest = string.IsNullOrEmpty(targetPath) ? context.Path : targetPath;
            var tmpPath = dest + ".tmp";
            try
            {
                // 共享字符串计数：uniqueCount = si 总数（必须准确），count = 全簿 t="s" 引用总数（重算，多行追加天然正确）
                var totalSharedRefs = context.Sheet.Root.Descendants(context.SheetNs + "c")
                    .Count(c => (string)c.Attribute("t") == "s");
                context.SharedStrings?.Root?.SetAttributeValue("uniqueCount", context.SharedTexts.Count);
                context.SharedStrings?.Root?.SetAttributeValue("count", totalSharedRefs);

                context.Entries[context.SheetEntryName] = ToXmlBytes(context.Sheet);
                if (context.SharedStrings != null)
                    context.Entries["xl/sharedStrings.xml"] = ToXmlBytes(context.SharedStrings);

                using (var stream = new FileStream(tmpPath, FileMode.Create))
                using (var archive = new ZipArchive(stream, ZipArchiveMode.Create))
                {
                    foreach (var pair in context.Entries)
                    {
                        var entry = archive.CreateEntry(pair.Key, CompressionLevel.Optimal);
                        using var entryStream = entry.Open();
                        entryStream.Write(pair.Value, 0, pair.Value.Length);
                    }
                }

                // 目标文件不存在时（生成精简副本）不能用 File.Replace，改用直接移动
                if (File.Exists(dest))
                    File.Replace(tmpPath, dest, null);
                else
                    File.Move(tmpPath, dest);

                if (File.Exists(tmpPath)) File.Delete(tmpPath);
                return true;
            }
            catch (Exception e)
            {
                D.Error(LC.Tr("[ XlsxRowAppender ] 写入工作簿失败：{0}",
                    "[ XlsxRowAppender ] Failed to write the workbook: {0}",
                    e.Message));
                if (File.Exists(tmpPath)) File.Delete(tmpPath);
                return false;
            }
        }

        // 工作簿解析产物（一次解析，查重与追加共用）
        private sealed class WorkbookContext
        {
            internal string Path;
            internal Dictionary<string, byte[]> Entries;
            internal string SheetEntryName;
            internal XDocument Sheet;
            internal XNamespace SheetNs;
            internal XDocument SharedStrings;
            internal XNamespace SharedNs;
            internal List<string> SharedTexts;
        }

        // 单元格文本：共享字符串 / 内联字符串 / 数值与公式结果，统一转字符串
        private static string CellText(XElement cell, List<string> shared)
        {
            var type = (string)cell.Attribute("t");
            var ns = cell.Name.Namespace;

            switch (type)
            {
                case "s":
                    var index = (int?)cell.Element(ns + "v");
                    return index is >= 0 && index.Value < shared.Count ? shared[index.Value] : string.Empty;
                case "inlineStr":
                    return string.Concat(cell.Element(ns + "is")?.Descendants(ns + "t").Select(t => t.Value) ??
                                         Enumerable.Empty<string>());
                default:
                    return cell.Element(ns + "v")?.Value ?? string.Empty;
            }
        }

        // 单元格引用（如 B7）中的列字母部分
        private static string CellColumn(string cellRef)
        {
            if (string.IsNullOrEmpty(cellRef)) return string.Empty;
            var letters = new StringBuilder();
            foreach (var c in cellRef)
            {
                if (char.IsLetter(c)) letters.Append(c);
                else break;
            }

            return letters.ToString();
        }

        // 列号（1-based）转列字母：1→A，26→Z，27→AA
        private static string ToColumnName(int column)
        {
            var letters = new StringBuilder();
            while (column > 0)
            {
                column--;
                letters.Insert(0, (char)('A' + column % 26));
                column /= 26;
            }

            return letters.ToString();
        }

        // XDocument 序列化为 UTF-8 字节（声明与 Excel 原生格式保持一致）
        private static byte[] ToXmlBytes(XDocument document)
        {
            var text = "<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?>\r\n" + document.Root;
            return new UTF8Encoding(false).GetBytes(text);
        }
    }
}