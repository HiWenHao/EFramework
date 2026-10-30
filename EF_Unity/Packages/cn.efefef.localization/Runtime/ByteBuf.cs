/*
 * ================================================
 * Describe:        Luban 二进制读取缓冲（varint/LEB128 整型，经实测导表产物验证）
 * Author:          Alvin5100
 * CreationTime:    2026-10-30 14:30:00
 * ModifyAuthor:    Alvin5100
 * ModifyTime:      2026-10-30 14:45:00
 * ScriptVersion:   0.2
 * ================================================
 */

using System;
using System.Text;

namespace EasyFramework.Localization
{
    /// <summary>
    /// Luban 二进制读取缓冲：只实现语言表解析所需的最小读取集
    /// <para>Minimal read buffer compatible with Luban's bytes export layout:
    /// 整型 = 无符号 LEB128 varint（≤127 单字节原值，>127 多字节低位在前）；
    /// string = varint 字节长度前缀 + UTF-8 内容。布局已实测验证：
    /// l10n_tbtext_en.bytes（4 记录 119 字节）与 example_tbitem.bytes（大值字段 a7 10 = 2087）均吻合。</para>
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
        /// <para>Binds the raw bytes of a table file.</para>
        /// </summary>
        /// <param name="bytes">表文件原始字节<para>Raw bytes of the table file.</para></param>
        public ByteBuf(byte[] bytes)
            => _bytes = bytes ?? throw new ArgumentNullException(nameof(bytes));

        /// <summary>
        /// 读取无符号 LEB128 varint（每字节低 7 位承载数据，最高位为继续标志）
        /// <para>Reads an unsigned LEB128 varint (7 data bits per byte, high bit as continuation flag).</para>
        /// </summary>
        public int ReadInt()
        {
            uint result = 0;
            int shift = 0;

            while (true)
            {
                if (Remaining < 1)
                    throw new ArgumentOutOfRangeException(nameof(Remaining), $"[ Localization ] varint 数据不足（已连续读取 {shift / 7} 字节）");

                byte b = _bytes[_readerIndex++];
                result |= (uint)(b & 0x7F) << shift;
                if ((b & 0x80) == 0) break;

                shift += 7;
                if (shift > 28)
                    throw new InvalidCastException("[ Localization ] varint 超过 5 字节，数据布局与预期不符");
            }

            return unchecked((int)result);
        }

        /// <summary>
        /// 读取字符串（varint 字节长度前缀 + UTF-8 内容，长度不含前缀自身）
        /// <para>Reads a string (varint byte-length prefix + UTF-8 content).</para>
        /// </summary>
        public string ReadString()
        {
            int len = ReadInt();
            if (len < 0 || len > Remaining)
                throw new ArgumentOutOfRangeException(nameof(len), $"[ Localization ] 字符串长度非法：{len}（剩余 {Remaining} 字节）");

            var text = Encoding.UTF8.GetString(_bytes, _readerIndex, len);
            _readerIndex += len;
            return text;
        }

        #endregion
    }
}
