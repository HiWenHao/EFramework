/*
 * ================================================
 * Describe:        Luban 二进制读取缓冲（整型采用 Luban 官方前缀编码，与 Luban.ByteBuf.ReadUint 逐位一致）
 * Author:          Alvin5100
 * CreationTime:    2026-10-30 14:30:00
 * ModifyAuthor:    Alvin5100
 * ModifyTime:      2026-10-31 21:55:00
 * ScriptVersion:   0.3
 * ================================================
 */

using System;
using System.Text;

namespace EasyFramework.Localization
{
    /// <summary>
    /// Luban 二进制读取缓冲：只实现语言表解析所需的最小读取集
    /// <para>Minimal read buffer compatible with Luban's bytes export layout.
    /// 整型/长度 = Luban 前缀编码（大端）：首字节高 1/2/3 位标识总字节数，后续字节为原值高位在前的补齐形式；
    /// string = 长度前缀 + UTF-8 内容。布局以项目内 Luban 运行时
    /// <c>Library/PackageCache/com.code-philosophy.luban/Runtime/ByteBuf.cs</c> 的
    /// <c>WriteUint/ReadUint</c> 为唯一权威依据（切勿按标准 LEB128 解读，两者仅在数值 &lt; 128 时巧合一致）。
    /// 已实测：l10n_tbtext_en.bytes（68 记录，含 159 字节长译文）/ l10n_tbtext_zh.bytes（71 记录）均完全消费。</para>
    /// </summary>
    internal sealed class ByteBuf
    {
        #region Fields

        // 底层字节数组（表文件原始内容）
        private readonly byte[] _bytes;

        // 当前读取位置
        private int _readerIndex;

        #endregion

        #region Properties

        /// <summary>
        /// 剩余未读字节数（标准布局解析完成后应为 0，否则说明数据与预期布局不符）
        /// <para>Unread byte count; must be zero after a standard-layout parse.</para>
        /// </summary>
        public int Remaining => _bytes.Length - _readerIndex;

        #endregion

        #region Methods

        /// <summary>
        /// 构造：绑定表文件原始字节
        /// <para>Binds the raw bytes of the table file.</para>
        /// </summary>
        /// <param name="bytes">表文件原始字节<para>Raw bytes of the table file.</para></param>
        public ByteBuf(byte[] bytes)
            => _bytes = bytes ?? throw new ArgumentNullException(nameof(bytes));

        /// <summary>
        /// 读取整型（Luban 前缀编码，与 <c>Luban.ByteBuf.ReadUint</c> 完全一致）：
        /// <list type="bullet">
        /// <item><description>首字节 &lt; 0x80：1 字节原值</description></item>
        /// <item><description>首字节 &lt; 0xC0：2 字节，值 = ((首字节 &amp; 0x3F) &lt;&lt; 8) | 次字节</description></item>
        /// <item><description>首字节 &lt; 0xE0：3 字节，值 = ((首字节 &amp; 0x1F) &lt;&lt; 16) | 后续 2 字节</description></item>
        /// <item><description>首字节 &lt; 0xF0：4 字节，值 = ((首字节 &amp; 0x0F) &lt;&lt; 24) | 后续 3 字节</description></item>
        /// <item><description>首字节 == 0xF0：5 字节，值 = 后续 4 字节</description></item>
        /// </list>
        /// <para>Reads an integer using Luban's prefix encoding (identical to <c>Luban.ByteBuf.ReadUint</c>).</para>
        /// </summary>
        public int ReadInt()
        {
            if (Remaining < 1)
                throw new ArgumentOutOfRangeException(nameof(Remaining), "[ Localization ] varint 数据不足（无首字节）");

            uint h = _bytes[_readerIndex++];

            if (h < 0x80)
            {
                // 0 111 1111：单字节原值
                return (int)h;
            }

            if (h < 0xC0)
            {
                // 10 11 1111：2 字节
                if (Remaining < 1)
                    throw new ArgumentOutOfRangeException(nameof(Remaining), "[ Localization ] varint 数据不足（2 字节编码缺第 2 字节）");

                uint x = ((h & 0x3F) << 8) | _bytes[_readerIndex++];
                return (int)x;
            }

            if (h < 0xE0)
            {
                // 110 1 1111：3 字节
                if (Remaining < 2)
                    throw new ArgumentOutOfRangeException(nameof(Remaining), "[ Localization ] varint 数据不足（3 字节编码缺后续字节）");

                uint x = ((h & 0x1F) << 16) | ((uint)_bytes[_readerIndex] << 8) | _bytes[_readerIndex + 1];
                _readerIndex += 2;
                return (int)x;
            }

            if (h < 0xF0)
            {
                // 1110 1111：4 字节
                if (Remaining < 3)
                    throw new ArgumentOutOfRangeException(nameof(Remaining), "[ Localization ] varint 数据不足（4 字节编码缺后续字节）");

                uint x = ((h & 0x0F) << 24) | ((uint)_bytes[_readerIndex] << 16) | ((uint)_bytes[_readerIndex + 1] << 8) | _bytes[_readerIndex + 2];
                _readerIndex += 3;
                return (int)x;
            }

            // 1111 0000：5 字节
            if (Remaining < 4)
                throw new ArgumentOutOfRangeException(nameof(Remaining), "[ Localization ] varint 数据不足（5 字节编码缺后续字节）");

            uint v = ((uint)_bytes[_readerIndex] << 24) | ((uint)_bytes[_readerIndex + 1] << 16) | ((uint)_bytes[_readerIndex + 2] << 8) | _bytes[_readerIndex + 3];
            _readerIndex += 4;
            return (int)v;
        }

        /// <summary>
        /// 读取字符串（长度前缀 + UTF-8 内容，长度不含前缀自身）
        /// <para>Reads a string (length prefix + UTF-8 content).</para>
        /// </summary>
        public string ReadString()
        {
            int len = ReadInt();
            if (len < 0 || len > Remaining)
                throw new ArgumentOutOfRangeException(nameof(len), $"[ Localization ] 字符串长度非法：{len}（剩余 {Remaining} 字节）");

            if (len == 0) return string.Empty;

            var text = Encoding.UTF8.GetString(_bytes, _readerIndex, len);
            _readerIndex += len;
            return text;
        }

        #endregion
    }
}
