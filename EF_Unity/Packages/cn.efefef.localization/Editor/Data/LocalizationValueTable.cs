/*
 * ================================================
 * Describe:        多语言 value 表：按语言拆分的 CSV，供翻译填写、再由 Luban 导表进包
 * Author:          Alvin5100
 * CreationTime:    2026-10-14 15:12:00
 * ModifyAuthor:    Alvin5100
 * ModifyTime:      2026-10-14 15:12:00
 * ScriptVersion:   0.1
 * ================================================
 */

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using EasyFramework.Edit;

namespace EasyFramework.Localization.Edit
{
    /// <summary>
    /// value 表 CSV 的单行
    /// <para>key/des/group/kind/note 五列由清单同步而来，只有 value 供人手填。</para>
    /// <para>Only the value column is meant to be filled by humans.</para>
    /// </summary>
    internal sealed class LocalizationValueRow
    {
        /// <summary>
        /// Key 名（与清单一致，导出后不可改）
        /// <para>与 Key 清单同名；改名会让运行时引用失效。</para>
        /// </summary>
        internal string key = string.Empty;

        /// <summary>
        /// 用途说明（给人看，方便翻译判断语境）
        /// </summary>
        internal string des = string.Empty;

        /// <summary>
        /// 分组（便于翻译按模块批量处理）
        /// </summary>
        internal string group = string.Empty;

        /// <summary>
        /// Key 类型：Static / Dynamic
        /// <para>Dynamic 表示需复数规则，翻译必须确认。</para>
        /// </summary>
        internal string kind = string.Empty;

        /// <summary>
        /// 备注（语气要求、术语约定等）
        /// </summary>
        internal string note = string.Empty;

        /// <summary>
        /// 译文（唯一需要人工填写的列）
        /// </summary>
        internal string value = string.Empty;

        /// <summary>
        /// 是否已翻译
        /// <para>空串视为未翻译，导出报告据此统计进度。</para>
        /// </summary>
        internal bool IsTranslated => !string.IsNullOrEmpty(value);
    }

    /// <summary>
    /// 单语言的 value 表（对应一个 L10n_{lang}.csv）
    /// <para>由 Key 清单单向导出：清单是唯一真相，value 表只是它的翻译投影。
    /// 重复导出不会丢译文——已存在的 value 会被原样带回。</para>
    /// <para>A per-language projection of the key manifest; re-export preserves existing values.</para>
    /// </summary>
    internal sealed class LocalizationValueTable
    {
        #region CSV 约定

        /// <summary> key 列名 </summary>
        internal const string ColKey = "key";

        /// <summary> 说明列名 </summary>
        internal const string ColDes = "des";

        /// <summary> 分组列名 </summary>
        internal const string ColGroup = "group";

        /// <summary> 种类列名 </summary>
        internal const string ColKind = "kind";

        /// <summary> 备注列名 </summary>
        internal const string ColNote = "note";

        /// <summary> 译文列名（唯一人工填写列） </summary>
        internal const string ColValue = "value";

        /// <summary> 表头列顺序。改这里等于改表结构，需同步告知翻译与 Luban 侧 </summary>
        internal static readonly string[] Header =
        {
            ColKey, ColDes, ColGroup, ColKind, ColNote, ColValue,
        };

        #region Luban 标记行与占位列

        /// <summary>
        /// Luban 单元薄标记：CSV 首行 A 列必须是它，否则报「不包含有效的单元薄」
        /// </summary>
        internal const string SheetMarker = "##";

        /// <summary>
        /// Luban 列类型标记行首格
        /// </summary>
        internal const string TypeMarker = "##type";

        /// <summary>
        /// 数据行首列的占位值
        /// <para>必须非空。Luban 把首列当标记列忽略，若数据行从 A 列直接写起，
        /// 整行会静默左移一列（key 会拿到des 的内容）且不报任何错——实测确认。
        /// 用固定 X 而非行号，是为了避免翻译在 Excel 里增删行后编号对不上造成困惑。</para>
        /// <para>Placeholder for the leading marker column; must be non-empty.</para>
        internal const string DataRowPlaceholder = "";

        /// <summary> 所有列的 Luban 类型，当前均为 string </summary>
        internal static readonly string[] ColumnTypes =
        {
            "string", "string", "string", "string", "string", "string",
        };

        /// <summary>
        /// 完整的表头行：<c>##,key,des,group,kind,note,value</c>
        /// </summary>
        internal static string MarkerHeaderLine
            => SheetMarker + "," + string.Join(",", Header);

        /// <summary>
        /// 完整的类型行：<c>##type,string,string,...</c>
        /// </summary>
        internal static string TypeLine
            => TypeMarker + "," + string.Join(",", ColumnTypes);

/// <summary>
        /// 判断一行是否为 Luban 单元薄标记行（首格为 ##）
        /// <para>该行同时兼作表头行：<c>##,key,des,...</c>，其第 0 格是标记，第 1 格起才是列名。</para>
        /// </summary>
        internal static bool IsMarkerRow(List<string> cells)
        {
       if (cells == null || cells.Count == 0) return false;
 var first = cells[0]?.Trim();
            return !string.IsNullOrEmpty(first)
     && first.StartsWith(SheetMarker, StringComparison.Ordinal);
   }

        /// <summary>
   /// 判断一行是否为 Luban 列类型标记行（首格为 ##type）
      /// </summary>
      internal static bool IsTypeMarkerRow(List<string> cells)
        {
            if (cells == null || cells.Count == 0) return false;
 return string.Equals(cells[0]?.Trim(), TypeMarker, StringComparison.OrdinalIgnoreCase);
        }

        #endregion

        /// <summary>
        /// 是否写 UTF-8 BOM
        /// <para>true：Excel / WPS 双击打开不乱码（Windows 版对本包是主要使用场景）。
        /// 已实测 Luban 内置csv 加载器能正确识别 BOM，故保持开启。</para>
        /// <para>Write UTF-8 BOM so Excel opens Chinese text correctly; Luban handles it.</para>
        /// </summary>
        internal const bool UseUtf8Bom = true;

        /// <summary> 行尾统一 CRLF，兼容 Excel 另存为与 Luban 读取 </summary>
        private const string NEWLINE = "\r\n";

        #endregion

        /// <summary>
        /// 语言代码（如 zh-CN）
        /// </summary>
        internal string language = string.Empty;

        /// <summary>
        /// 表内所有行（顺序与清单一致）
        /// </summary>
        internal List<LocalizationValueRow> rows = new List<LocalizationValueRow>();

        /// <summary>
        /// 本表绝对路径
        /// </summary>
        internal string path = string.Empty;

        /// <summary>
        /// 已翻译行数
        /// </summary>
        internal int TranslatedCount
        {
            get
            {
                int n = 0;
                foreach (var row in rows)
                {
                    if (row.IsTranslated) n++;
                }

                return n;
            }
        }

        /// <summary>
        /// 从磁盘读取 value 表
        /// <para>文件不存在时返回空表（不是 null），便于直接进入导出流程。</para>
        /// <para>Load from disk; returns an empty table when the file does not exist.</para>
        /// </summary>
        /// <param name="language">语言代码</param>
        /// <returns>value 表实例；解析失败返回空表（错误已打日志）</returns>
        internal static LocalizationValueTable Load(string language)
        {
            var table = new LocalizationValueTable
            {
                language = language,
                path = LocalizationData.GetValuesPath(language),
            };

            if (!File.Exists(table.path)) return table;

try
{
            // 显式按 UTF-8 读：文件可能带 BOM，StreamReader 默认已识别，此处仅保证异常可控
       var rows = ParseCsv(File.ReadAllText(table.path, Encoding.UTF8));

     // 本表格式固定为 Luban 兼容格式：
    // 第 1 行 = `##,key,des,...`（标记行本身兼作表头），第 2 行 = `##type,string,...`，第 3 行起是数据。
// 少任何一行都说明文件被手工改坏或不是本工具产物。
      if (rows.Count < 2
            || !IsMarkerRow(rows[0])
   || !IsTypeMarkerRow(rows[1]))
       {
        D.Error($"[Localization] value 表格式不正确（缺少 Luban 标记行），已按空表处理：" +
      $"{table.path}\n请用面板重新导出，不要手工增删标记行。");
    return table;
            }

    // 表头行与数据行都在第 0 格带了标记/占位（## 与 X），下标天然对齐，无需额外偏移
            var header = rows[0];

            int iKey = IndexOf(header, ColKey);
            int iValue = IndexOf(header, ColValue);

    if (iKey < 0 || iValue < 0)
       {
       D.Error($"[Localization] value 表缺少 {ColKey} 或 {ColValue} 列，已按空表处理：" +
           $"{table.path}");
         return table;
            }

            int iDes = IndexOf(header, ColDes);
   int iGroup = IndexOf(header, ColGroup);
      int iKind = IndexOf(header, ColKind);
            int iNote = IndexOf(header, ColNote);

   for (int r = 2; r < rows.Count; r++)
{
     var cells = rows[r];
                var key = Cell(cells, iKey);

      // 整行为空视为空行（Excel 末尾常见），跳过而不是产出一条空 key
if (string.IsNullOrEmpty(key)) continue;

       table.rows.Add(new LocalizationValueRow
         {
      key = key,
    des = Cell(cells, iDes),
              group = Cell(cells, iGroup),
      kind = Cell(cells, iKind),
          note = Cell(cells, iNote),
            value = Cell(cells, iValue),
     });
         }
         }
            catch (Exception e)
            {
                D.Error($"[Localization] 读取 value 表失败：{table.path}\n{e.Message}");
            }

            return table;
        }

        /// <summary>
        /// 覆写磁盘 CSV（自动创建目录，UTF-8 + 可选 BOM）
        /// <para>直接产出 Luban 可吃的格式：标记行 + 类型行 + 数据行首列占位。
        /// 使用者填完 value 后可直接交给面板导表，不需要任何手工预处理。</para>
        /// <returns>写入成功返回 true；失败（已输出错误日志）返回 false</returns>
        /// </summary>
        internal bool Save()
        {
            if (string.IsNullOrEmpty(path)) return false;

            try
            {
                var dir = Path.GetDirectoryName(path);
                if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                    Directory.CreateDirectory(dir);

                var sb = new StringBuilder();

                // 第1 行：## + 表头     第 2 行：##type + 列类型
                sb.Append(MarkerHeaderLine).Append(NEWLINE);
                sb.Append(TypeLine).Append(NEWLINE);

                // 数据行：首列写占位值，其余按表头顺序
                foreach (var row in rows)
                {
                    sb
                        .Append(DataRowPlaceholder).Append(',')
                        .Append(Escape(row.key)).Append(',')
                        .Append(Escape(row.des)).Append(',')
                        .Append(Escape(row.group)).Append(',')
                        .Append(Escape(row.kind)).Append(',')
                        .Append(Escape(row.note)).Append(',')
                        .Append(Escape(row.value))
                        .Append(NEWLINE);
                }

                File.WriteAllText(path, sb.ToString(), new UTF8Encoding(UseUtf8Bom));
                return true;
            }
            catch (Exception e)
            {
                D.Error($"[Localization] 写入 value 表失败：{path}\n{e.Message}");
                return false;
            }
        }

        /// <summary>
        /// 建立 key → value 索引，用于导出时把旧译文带回新表
        /// <para>重复 key 取首次出现（Luban 遇重复主键会报错，故导出时会去重）。</para>
        /// </summary>
        internal Dictionary<string, string> BuildValueMap()
        {
            var map = new Dictionary<string, string>(StringComparer.Ordinal);
            foreach (var row in rows)
            {
                if (string.IsNullOrEmpty(row.key)) continue;
                if (map.ContainsKey(row.key)) continue;
                map[row.key] = row.value ?? string.Empty;
            }

            return map;
        }

        #region CSV 解析与转义

        /// <summary>
        /// 解析 CSV 文本为行 → 单元格
        /// <para>按 RFC 4180 处理：双引号包裹、引号内逗号与换行、连续两个引号表示一个引号。
        /// 必须逐字符扫描而非按行拆分——des/note 里出现换行是常态（Excel 粘贴多行单元格）。</para>
        /// <para>Full RFC 4180 parser; embedded newlines inside quotes are supported.</para>
        /// </summary>
        /// <param name="text">CSV 全文</param>
        /// <returns>解析出的行集合；空文本返回空集合</returns>
        private static List<List<string>> ParseCsv(string text)
        {
            var result = new List<List<string>>();
            if (string.IsNullOrEmpty(text)) return result;

            var cells = new List<string>();
            var sb = new StringBuilder();
            bool inQuote = false;

            for (int i = 0; i < text.Length; i++)
            {
                char c = text[i];

                if (inQuote)
                {
                    if (c != '"')
                    {
                        sb.Append(c);
                    }
                    else if (i + 1 < text.Length && text[i + 1] == '"')
                    {
                        // 转义的双引号
                        sb.Append('"');
                        i++;
                    }
                    else
                    {
                        inQuote = false;
                    }

                    continue;
                }

                switch (c)
                {
                    case '"':
                        inQuote = true;
                        break;

                    case ',':
                        cells.Add(sb.ToString());
                        sb.Length = 0;
                        break;

                    case '\r':
                        // 交给 \n 统一处理，避免 CRLF 被拆成两行
                        break;

                    case '\n':
                        cells.Add(sb.ToString());
                        sb.Length = 0;
                        PushRow(result, cells);
                        break;

                    default:
                        sb.Append(c);
                        break;
                }
            }

            // 收尾：文件末尾无换行时补出最后一行
            if (sb.Length > 0 || cells.Count > 0)
            {
                cells.Add(sb.ToString());
                PushRow(result, cells);
            }

            return result;
        }

        /// <summary>
        /// 收下一行；整行为空则丢弃
        /// <para>Excel 保存后常留尾部空行，若不丢弃会产出一条空 key 污染表。</para>
        /// <para>必须先拷贝再清空入参：直接把 cells 塞进 result 会让所有行引用同一个
        /// List，后续行继续往里追加，导致读回来每行都是全表内容。</para>
        /// </summary>
        private static void PushRow(List<List<string>> result, List<string> cells)
        {
            foreach (var cell in cells)
            {
                if (string.IsNullOrEmpty(cell))
                    continue;

                // 拷贝一份，避免与复用的 cells 产生别名
                result.Add(new List<string>(cells));
                cells.Clear();
                return;
            }

            cells.Clear();
        }

/// <summary>
    /// 取指定下标的单元格，越界返回空串
    /// </summary>
    private static string Cell(List<string> cells, int index)
      => index >= 0 && index < cells.Count ? cells[index] ?? string.Empty : string.Empty;

        /// <summary>
        /// 在表头中查找列名
        /// </summary>
        private static int IndexOf(List<string> header, string name)
        {
            for (int i = 0; i < header.Count; i++)
            {
                if (string.Equals(header[i]?.Trim(), name, StringComparison.OrdinalIgnoreCase))
                    return i;
            }

            return -1;
        }

        /// <summary>
        /// 单个字段转义
        /// <para>含逗号 / 引号 / 换行时用双引号包裹，内部引号翻倍。
        /// 注意不能用 \r 之外的判断遗漏——本方法覆盖全部三类触发字符。</para>
        /// </summary>
        /// <param name="field">原始字段</param>
        /// <returns>可直接写入 CSV 的字段文本</returns>
        private static string Escape(string field)
        {
            if (string.IsNullOrEmpty(field)) return string.Empty;

            bool needQuote = field.IndexOf(',') >= 0
                             || field.IndexOf('"') >= 0
                             || field.IndexOf('\n') >= 0
                             || field.IndexOf('\r') >= 0;

            if (!needQuote) return field;

            return "\"" + field.Replace("\"", "\"\"") + "\"";
        }

        #endregion
    }

    /// <summary>
    /// 一次导出的结果报告
    /// <para>给面板与 CI 用：新增 / 保留 / 剔除各多少条，以及被剔除的孤儿 key。</para>
    /// <para>Result of one export run, used by the panel and by CI.</para>
    /// </summary>
    internal sealed class LocalizationExportReport
    {
        /// <summary> 语言代码 </summary>
        internal string language = string.Empty;

        /// <summary> 目标文件绝对路径 </summary>
        internal string path = string.Empty;

        /// <summary> 写入是否成功 </summary>
        internal bool success;

        /// <summary> 清单新增到表里的行数（此前没有译文） </summary>
        internal int addedCount;

        /// <summary> 沿用已有译文的行数 </summary>
        internal int keptCount;

        /// <summary> 因清单中已删除而被剔除的孤儿 key </summary>
        internal List<string> removedKeys = new List<string>();

        /// <summary> 清单内名字为空或重名而跳过的 key（附原因） </summary>
        internal List<string> skippedKeys = new List<string>();

        /// <summary> 本表总行数 </summary>
        internal int totalCount;

        /// <summary> 本表已翻译行数 </summary>
        internal int translatedCount;

        /// <summary>
        /// 一行摘要，供弹窗与 Console 输出
        /// </summary>
        internal string Summary =>
            $"[{language}] {(success ? "OK" : "FAIL")}  " +
            $"新增 {addedCount} / 保留 {keptCount} / 剔除 {removedKeys.Count} / 跳过 {skippedKeys.Count}" +
            $"  共 {totalCount} 条，已翻译 {translatedCount} 条" +
            (translatedCount < totalCount ? $"，未翻译 {totalCount - translatedCount} 条" : string.Empty);
    }

    /// <summary>
    /// value 表导出器：以 Key 清单为唯一真相，单向投影出各语言 value 表
    /// <para>三条硬规则：
    /// ① 已填译文绝不覆盖——重复导出只是补新 key、改 meta 列；
    /// ② 清单里没有的 key 从表中剔除（清单是唯一真相），但会在报告里点名，不静默吞掉；
    /// ③ 清单内重名 / 空名 key 直接跳过并报告，避免把脏数据写进 Luban 表。</para>
    /// <para>Exports per-language value tables from the key manifest without losing existing values.</para>
    /// </summary>
    internal static class LocalizationValueExporter
    {
        /// <summary>
        /// 导出一个语言的 value 表
        /// </summary>
        /// <param name="keys">Key 清单（唯一真相）</param>
        /// <param name="language">语言代码，如 zh-CN</param>
        /// <returns>导出报告；清单为空时返回 success=false 的报告</returns>
        internal static LocalizationExportReport Export(LocalizationKeyFile keys, string language)
        {
            var report = new LocalizationExportReport
            {
                language = language,
                path = LocalizationData.GetValuesPath(language),
            };

            if (keys?.keys == null)
            {
                D.Error("[Localization] Key 清单为空，无法导出 value 表。");
                return report;
            }

            // 先读旧表拿到已有译文，再按清单顺序重建
            var old = LocalizationValueTable.Load(language);
            var oldValues = old.BuildValueMap();

            var fresh = new List<LocalizationValueRow>();
            var written = new HashSet<string>(StringComparer.Ordinal);

            foreach (var entry in keys.keys)
            {
                var name = entry.name ?? string.Empty;

                if (string.IsNullOrEmpty(name))
                {
                    report.skippedKeys.Add($"(空名) {entry.des}");
                    continue;
                }

                if (!entry.IsValidName())
                {
                    report.skippedKeys.Add($"{name} → {LC.Combine(Lc.Name, Lc.Is, Lc.Illegal, Lc.Character)}");
                    continue;
                }

                // 重名会让 Luban 主键冲突，保留首条并报告
                if (!written.Add(name))
                {
                    report.skippedKeys.Add($"{name} → {LC.Combine(Lc.Row, Lc.Repetition)}");
                    continue;
                }

                bool existed = oldValues.TryGetValue(name, out var value);
                if (!existed)
                {
                    value = string.Empty;
                    report.addedCount++;
                }
                else
                {
                    report.keptCount++;
                }

                fresh.Add(new LocalizationValueRow
                {
                    key = name,
                    des = entry.des ?? string.Empty,
                    group = entry.group ?? string.Empty,
                    kind = entry.kind.ToString(),
                    note = entry.note ?? string.Empty,
                    value = value ?? string.Empty,
                });
            }

            // 清单已删除的 key：点名剔除
            foreach (var key in oldValues.Keys)
            {
                if (!written.Contains(key)) report.removedKeys.Add(key);
            }

            fresh.Sort((a, b) => string.Compare(a.key, b.key, StringComparison.Ordinal));

            var output = new LocalizationValueTable
            {
                language = language,
                path = report.path,
                rows = fresh,
            };

            report.totalCount = fresh.Count;
            report.translatedCount = output.TranslatedCount;
            report.success = output.Save();

            if (report.removedKeys.Count > 0)
                D.Warning($"[Localization] value 表 [{language}] 剔除了 {report.removedKeys.Count} 个" +
                          $"清单中已不存在的 key：{string.Join(", ", report.removedKeys)}");

            foreach (var skip in report.skippedKeys)
                D.Warning($"[Localization] value 表 [{language}] 跳过 {skip}");

            return report;
        }

        /// <summary>
        /// 批量导出所有语言
        /// </summary>
        /// <param name="keys">Key 清单</param>
        /// <param name="languages">语言代码集合</param>
        /// <returns>逐语言的导出报告，顺序与入参一致</returns>
        internal static List<LocalizationExportReport> ExportAll(LocalizationKeyFile keys,
            List<string> languages)
        {
            var reports = new List<LocalizationExportReport>();
            if (languages == null || languages.Count == 0) return reports;

            foreach (var language in languages)
            {
                if (string.IsNullOrEmpty(language)) continue;
                reports.Add(Export(keys, language));
            }

            return reports;
        }
    }
}
