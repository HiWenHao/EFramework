/*
 * ================================================
 * Describe:        向 Luban 表定义工作簿（__tables__.xlsx）追加数据的极简工具：指定列追加单值 / 末行追加整行，自动定位路径、检测数据、内部报错
 * Author:          Alvin8412
 * CreationTime:    2026-10-21 11:25:00
 * ModifyAuthor:    Alvin8412
 * ModifyTime:      2026-10-21 16:40:00
 * ScriptVersion:   0.6
 * ================================================
 */

using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using EasyFramework.Edit.Windows.Dataable;
using UnityEngine;
using CompressionLevel = System.IO.Compression.CompressionLevel;

namespace EasyFramework.Edit
{
    /// <summary>
    /// 向 Luban 表定义工作簿（__tables__.xlsx）追加数据的极简工具
    /// <para>Minimal tool for appending data to the Luban table-definition workbook (__tables__.xlsx).</para>
    /// </summary>
    public static class XlsxRowAppender
    {
        // Luban 表定义工作簿文件名
        private const string TablesFileName = "__tables__.xlsx";

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
        /// <returns>true = 追加成功；false = 数据不正确或写入失败<para>true on success; false when the data is invalid or the write failed.</para></returns>
        public static bool AppendLastRow(IReadOnlyDictionary<int, object> cells, int uniqueColumn)
        {
            // 数据检测：字典与列号
            if (cells == null || cells.Count == 0)
            {
                D.Error("[ XlsxRowAppender ] 追加失败：没有可写入的数据");
                return false;
            }

            if (cells.Keys.Any(c => c < 1))
            {
                D.Error("[ XlsxRowAppender ] 追加失败：列号必须从 1 开始（A 列 = 1）");
                return false;
            }

            if (uniqueColumn < 1 || !cells.ContainsKey(uniqueColumn))
            {
                D.Error($"[ XlsxRowAppender ] 追加失败：唯一性判定列 {uniqueColumn} 不在待写入数据中");
                return false;
            }

            // 数据检测：值非空且类型可写
            foreach (var pair in cells)
            {
                if (pair.Value == null || pair.Value.ToString().Length == 0)
                {
                    D.Error($"[ XlsxRowAppender ] 追加失败：第 {pair.Key} 列的值为空");
                    return false;
                }

                if (pair.Value is int or string) continue;
                D.Error($"[ XlsxRowAppender ] 追加失败：第 {pair.Key} 列的值类型 {pair.Value.GetType().Name} 不支持（仅 int / string）");
                return false;
            }

            if (!LoadWorkbook(out var context)) return false;

            // 唯一性检测：判定列的值已存在 = 数据重复
            var keyText = cells[uniqueColumn].ToString();
            if (ColumnContains(context, uniqueColumn, keyText))
            {
                D.Error($"[ XlsxRowAppender ] 追加失败：第 {uniqueColumn} 列已存在相同数据「{keyText}」（重复追加）");
                return false;
            }

            return WriteAppend(context, cells);
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
                D.Error("[ XlsxRowAppender ] 检测失败：列号必须从 1 开始（A 列 = 1）");
                return false;
            }

            if (value == null || value.ToString().Length == 0)
            {
                D.Error("[ XlsxRowAppender ] 检测失败：检测内容为空");
                return false;
            }

            if (!LoadWorkbook(out var context)) return false;

            return ColumnContains(context, columnIndex, value.ToString());
        }

        /// <summary>
        /// 检测 Luban 表定义工作簿（__tables__.xlsx）是否存在
        /// <para>Checks whether the Luban table-definition workbook (__tables__.xlsx) exists.</para>
        /// </summary>
        /// <returns>true = 文件存在；false = 不存在（未配置数据表的工程属正常状态，不报错）<para>true when the file exists; false otherwise (normal for projects without tables, no error logged).</para></returns>
        public static bool Exists()
            => File.Exists(ResolveTablesPath());

        // 解析 Luban 表定义工作簿绝对路径：DatableConfig.LubanSourcePath 优先（相对值按工程根归一化、目录必须存在），否则回落默认数据源目录
        private static string ResolveTablesPath()
        {
            if (EditorUtils.CheckAssets<DatableConfig>(out var configPath))
            {
                var config = EditorUtils.LoadSettingAtPath<DatableConfig>(configPath);
                var source = config != null ? config.LubanSourcePath : null;
                if (!string.IsNullOrWhiteSpace(source))
                {
                    var directory = Path.IsPathRooted(source)
                        ? source
                        : Path.GetFullPath(Path.Combine(Application.dataPath, "..", source));
                    if (Directory.Exists(directory))
                        return Path.Combine(directory, TablesFileName);
                }
            }

            return Path.Combine(DatableConfigPanel.ExcelFolderPath, "Datas", TablesFileName);
        }

        // 定位并解析工作簿（路径定位 + zip 读取 + 工作表与共享字符串解析），任何一步失败 LogError 并返回 false
        private static bool LoadWorkbook(out WorkbookContext context)
        {
            context = null;

            var path = ResolveTablesPath();
            if (!File.Exists(path))
            {
                D.Error($"[ XlsxRowAppender ] 找不到表定义文件：{path}");
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
                    D.Error("[ XlsxRowAppender ] 缺少 xl/workbook.xml，不是有效的 xlsx 文件");
                    return false;
                }

                var officeRns = XNamespace.Get("http://schemas.openxmlformats.org/officeDocument/2006/relationships");
                var workbook = XDocument.Parse(Encoding.UTF8.GetString(workbookBytes));
                var mainNs = workbook.Root.Name.Namespace;
                var sheet = workbook.Root.Element(mainNs + "sheets")?.Elements(mainNs + "sheet").FirstOrDefault();
                if (sheet == null)
                {
                    D.Error("[ XlsxRowAppender ] workbook.xml 中没有工作表");
                    return false;
                }

                var rid = (string)sheet.Attribute(officeRns + "id");
                if (!entries.TryGetValue("xl/_rels/workbook.xml.rels", out var relsBytes))
                {
                    D.Error("[ XlsxRowAppender ] 缺少 xl/_rels/workbook.xml.rels");
                    return false;
                }

                var packageRns = XNamespace.Get("http://schemas.openxmlformats.org/package/2006/relationships");
                var xElement = XDocument.Parse(Encoding.UTF8.GetString(relsBytes)).Root;
                if (xElement == null) return true;

                var relationship = xElement.Elements(packageRns + "Relationship")
                    .FirstOrDefault(r => (string)r.Attribute("Id") == rid);
                if (relationship == null)
                {
                    D.Error($"[ XlsxRowAppender ] workbook.xml.rels 中找不到 rId：{rid}");
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
                D.Error($"[ XlsxRowAppender ] 解析工作簿失败：{e.Message}");
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

        // 组装新行、更新共享字符串计数、写回工作簿（临时文件 → 原子替换，不做备份）
        private static bool WriteAppend(WorkbookContext context, IReadOnlyDictionary<int, object> cells)
        {
            var tmpPath = context.Path + ".tmp";
            try
            {
                var sheetData = context?.Sheet?.Root?.Element(context.SheetNs + "sheetData");
                if (sheetData == null)
                {
                    sheetData = new XElement(context.SheetNs + "sheetData");
                    context?.Sheet?.Root?.Add(sheetData);
                }

                var newRow = sheetData.Elements(context.SheetNs + "row")
                    .Select(r => (int?)r.Attribute("r"))
                    .Max() ?? 0;
                newRow++;

                // 组装行元素：int 写数字单元格，string 写共享字符串单元格（查重复用索引）；空值跳过（稀疏行）
                var rowElement = new XElement(context.SheetNs + "row", new XAttribute("r", newRow));
                var sharedRefCount = 0;
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
                    sharedRefCount++;
                }

                sheetData.Add(rowElement);

                // 共享字符串计数：uniqueCount = si 总数（必须准确），count = 全簿引用总数（增量）
                context.SharedStrings?.Root?.SetAttributeValue("uniqueCount", context.SharedTexts.Count);
                if (context.SharedStrings != null)
                {
                    var countAttr = context.SharedStrings?.Root?.Attribute("count");
                    var newCount = sharedRefCount
                                   + (countAttr != null && int.TryParse(countAttr.Value, out var parsed) ? parsed : 0);
                    context.SharedStrings?.Root.SetAttributeValue("count", newCount);
                }

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

                File.Replace(tmpPath, context.Path, null);

                if (File.Exists(tmpPath)) File.Delete(tmpPath);
                return true;
            }
            catch (Exception e)
            {
                D.Error($"[ XlsxRowAppender ] 写入工作簿失败：{e.Message}");
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