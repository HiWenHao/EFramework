/*
 * ================================================
 * Describe:        语言表统一解析层：按内容分发到注册的格式解析器（json / Luban bytes / 自定义格式）
 * Author:          Alvin5100
 * CreationTime:    2026-10-30 14:30:00
 * ModifyAuthor:    Alvin5100
 * ModifyTime:      2026-10-30 14:30:00
 * ScriptVersion:   0.1
 * ================================================
 */

using System;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json;
using UnityEngine;

namespace EasyFramework.Localization
{
    /// <summary>
    /// 语言表解析器接口：一种实现对应一种导表数据格式
    /// <para>Contract for one table-data format (json / Luban bytes / custom).</para>
    /// </summary>
    public interface IL10nTableParser
    {
        #region Methods

        /// <summary>
        /// 尝试解析；数据不属于本格式（或布局损坏）时返回 false 交给下一个解析器，由门面统一兜底报错
        /// <para>Attempts to parse; returns false when the data does not match this format.</para>
        /// </summary>
        /// <param name="data">表文件原始字节<para>Raw bytes of the table file.</para></param>
        /// <param name="sourceName">来源名（用于错误定位）<para>Source name for error reports.</para></param>
        /// <param name="table">解析结果：key → 译文<para>Parsed table: key → translation.</para></param>
        /// <returns>是否由本解析器成功解析<para>Whether this parser accepted the data.</para></returns>
        bool TryParse(byte[] data, string sourceName, out Dictionary<string, string> table);

        #endregion
    }

    /// <summary>
    /// 语言表解析门面：按注册顺序逐个尝试，首个成功者生效；外部可注册自定义格式
    /// <para>Parser facade trying registered parsers in order; register custom formats via <see cref="Register"/>.</para>
    /// </summary>
    public static class L10nTableParser
    {
        #region Fields

        // 解析器链（顺序即优先级）：json 在前（文本嗅探成本低），Luban bytes 在后
        private static readonly List<IL10nTableParser> Parsers = new()
        {
            new L10nJsonTableParser(),
            new LubanBytesL10nTableParser(),
        };

        #endregion

        #region Methods

        /// <summary>
        /// 注册自定义格式解析器（插到队首，优先于内置格式尝试）
        /// <para>Registers a custom parser at the head of the chain.</para>
        /// </summary>
        /// <param name="parser">自定义解析器<para>The custom parser.</para></param>
        public static void Register(IL10nTableParser parser)
        {
            if (parser == null) return;
            Parsers.Insert(0, parser);
        }

        /// <summary>
        /// 解析入口：传入表文件原始字节（TextAsset.bytes / File.ReadAllBytes 均可），自动分发到匹配的解析器
        /// <para>Parses raw table bytes and dispatches to the matching parser.</para>
        /// </summary>
        /// <param name="data">表文件原始字节<para>Raw bytes of the table file.</para></param>
        /// <param name="sourceName">来源名（用于错误定位）<para>Source name for error reports.</para></param>
        /// <returns>key → 译文表；数据为空或格式不可识别时返回空表（调用方按空表语义处理）<para>The parsed table, or an empty one on failure.</para></returns>
        public static Dictionary<string, string> Parse(byte[] data, string sourceName)
        {
            var empty = new Dictionary<string, string>(StringComparer.Ordinal);
            if (data == null || data.Length == 0) return empty;

            foreach (var parser in Parsers)
            {
                if (!parser.TryParse(data, sourceName, out var table)) continue;
                return table ?? empty;
            }

            // 所有解析器都不认：真故障，明确报错（此前 bytes 走 JSON 通道静默失败，这里补上可见性）
            D.Error($"[ Localization ] 无法识别的语言表格式：{sourceName}");
            return empty;
        }

        #endregion
    }

    /// <summary>
    /// JSON 解析器：对象数组 [{key,value},...]，兼容 UTF-8 BOM
    /// <para>Parser for the JSON object-array export.</para>
    /// </summary>
    public sealed class L10nJsonTableParser : IL10nTableParser
    {
        #region Methods

        bool IL10nTableParser.TryParse(byte[] data, string sourceName, out Dictionary<string, string> table)
        {
            table = null;

            string text;
            try
            {
                text = Encoding.UTF8.GetString(data).TrimStart('\uFEFF');
            }
            catch
            {
                return false;
            }

            // 嗅探：JSON 必须以 { 或 [ 开头，二进制乱码直接交给下一个解析器
            var trimmed = text.TrimStart();
            if (trimmed.Length == 0 || (trimmed[0] != '{' && trimmed[0] != '[')) return false;

            try
            {
                var rows = JsonConvert.DeserializeObject<List<L10nJsonRow>>(trimmed);
                if (rows == null) return false;

                var result = new Dictionary<string, string>(StringComparer.Ordinal);
                foreach (var row in rows)
                {
                    if (string.IsNullOrEmpty(row?.key)) continue;
                    result[row.key] = row.value ?? string.Empty;
                }

                table = result;
                return true;
            }
            catch
            {
                // JSON 损坏或非本结构：交下一个解析器，全部失败时门面报错
                return false;
            }
        }

        #endregion

        // JSON 单行结构（与导出产物字段名一一对应，不可改）
        [Serializable]
        private sealed class L10nJsonRow
        {
            // 本地化 key
            public string key = string.Empty;

            // 译文
            public string value = string.Empty;
        }
    }

    /// <summary>
    /// Luban bytes 解析器：varint 整型；map 表 = varint 记录数 + N×(key:string, value:string)
    /// <para>Parser for Luban binary export (LEB128 varint layout), with full-consumption validation.
    /// 布局经实测验证（l10n_tbtext_en.bytes / example_tbitem.bytes）。</para>
    /// </summary>
    public sealed class LubanBytesL10nTableParser : IL10nTableParser
    {
        #region Fields

        // 记录数健全性上限：超过视为非本格式（语言表不可能有百万行）
        private const int MaxRecords = 1_000_000;

        #endregion

        #region Methods

        bool IL10nTableParser.TryParse(byte[] data, string sourceName, out Dictionary<string, string> table)
        {
            table = null;

            try
            {
                var buf = new ByteBuf(data);
                int size = buf.ReadInt();
                if (size < 0 || size > MaxRecords) return false;

                var result = new Dictionary<string, string>(size, StringComparer.Ordinal);
                for (int i = 0; i < size; i++)
                {
                    var key = buf.ReadString();
                    var value = buf.ReadString();
                    if (string.IsNullOrEmpty(key)) continue;
                    result[key] = value ?? string.Empty;
                }

                // 强校验：标准布局应恰好耗尽全部字节；有剩余说明布局与预期不符（如 Luban 版本差异），安全拒绝
                if (buf.Remaining != 0) return false;

                table = result;
                return true;
            }
            catch
            {
                // 非本格式或布局不符：交下一个解析器，全部失败时门面报错
                return false;
            }
        }

        #endregion
    }
}
