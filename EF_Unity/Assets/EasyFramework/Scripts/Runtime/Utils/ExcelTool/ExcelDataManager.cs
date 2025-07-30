/* 
 * ================================================
 * Describe:      This script is used to .
 * Author:        Xiaohei.Wang(Wenhao)
 * CreationTime:  2023-05-30 14:05:56
 * ModifyAuthor:  Xiaohei.Wang(Wenhao)
 * ModifyTime:    2023-05-30 14:05:56
 * ScriptVersion: 0.1
 * ===============================================
*/
using System.Collections.Generic;
using UnityEngine;

namespace EasyFramework
{
    namespace ExcelTool
    {
        /// <summary>
        /// The execl data manager.表格数据管理器
        /// </summary>
        public static class ExcelDataManager
        {
            /// <summary>
            /// The all byte file path.全部字节文件路径
            /// </summary>
            public static string AllByteFilePath => _byteDataPathPrefix;

            static string _byteDataPathPrefix = null;

            /// <summary>
            /// Initialize excel transition.
            /// 初始化表格转换器. 
            /// </summary>
            /// <param name="byteDataPath">Stash byte data file path in the resources folder.Bytes文件在Resources文件夹下的保存路径</param>
            public static void Init(string byteDataPath)
            {
                if (byteDataPath != null)
                    _byteDataPathPrefix = byteDataPath + "/";
                byte[] data = Resources.Load<TextAsset>(_byteDataPathPrefix + "manifest").bytes;
                if (data.Length > 0)
                {
                    int index = 0;
                    short fileCnt = ByteReader.ReadShort(data, index);
                    index += 2;
                    for (short i = 0; i < fileCnt; i++)
                    {
                        ByteFileParam param = new ByteFileParam();
                        param.FileName = ByteReader.ReadString(data, index, false);
                        index += param.FileName.Length + 2;
                        param.IdColIndex = ByteReader.ReadInt(data, index);
                        index += 4;
                        param.RowCount = ByteReader.ReadInt(data, index);
                        index += 4;
                        param.RowLen = ByteReader.ReadInt(data, index);
                        index += 4;
                        param.ColOff = ByteReader.ReadListInt(data, index, false);
                        index += 4 * param.ColOff.Count + 2;
                        param.Types = ByteReader.ReadListInt(data, index, false);
                        index += 4 * param.Types.Count + 2;
                        param.VarNames = ByteReader.ReadListString(data, index, false);
                        index += GetListStringLen(param.VarNames);
                        param.Cache = ByteReader.ReadBool(data, index);
                        index++;
                        param.OptimizeType = (OptimizeType)ByteReader.ReadByte(data, index);
                        index++;

                        switch (param.OptimizeType)
                        {
                            case OptimizeType.Continuity:
                                param.Step = ByteReader.ReadInt(data, index);
                                index += 4;
                                break;
                            case OptimizeType.Segment:
                                param.SegmentList = ByteReader.ReadListInt(data, index, false);
                                index += 4 * param.SegmentList.Count + 2;
                                break;
                            case OptimizeType.PartialContinuity:
                                param.ContinuityStartOff = ByteReader.ReadInt(data, index);
                                index += 4;
                                param.ContinuityCnt = ByteReader.ReadInt(data, index);
                                index += 4;
                                break;
                        }
                        param.ExtraInfo = ByteReader.ReadDict<string, string>(data, index, false);
                        index += GetDictStringLen(param.ExtraInfo);

                        object info = GetByteFileInfo(param);
                        byteFilefileInfoDict.Add(i, info);
                    }
                }
            }

            private static readonly Dictionary<short, object> byteFilefileInfoDict = new Dictionary<short, object>();

            private static int GetListStringLen(List<string> ls)
            {
                int len = 2;
                foreach (var s in ls)
                {
                    len += (s.Length + 2);
                }
                return len;
            }

            private static int GetDictStringLen(Dictionary<string, string> dict)
            {
                int len = 2;
                foreach (var s in dict)
                {
                    len += (s.Key.Length + s.Value.Length + 4);
                }
                return len;
            }

            private static object GetByteFileInfo(ByteFileParam param)
            {
                int idType = param.Types[param.IdColIndex];
                switch (idType)
                {
                    case (int)TypeToken.Bool: return new ByteFileInfo<bool>(param);
                    case (int)TypeToken.Sbyte: return new ByteFileInfo<sbyte>(param);
                    case (int)TypeToken.Byte: return new ByteFileInfo<byte>(param);
                    case (int)TypeToken.UShort: return new ByteFileInfo<ushort>(param);
                    case (int)TypeToken.Short: return new ByteFileInfo<short>(param);
                    case (int)TypeToken.UInt: return new ByteFileInfo<uint>(param);
                    case (int)TypeToken.Int: return new ByteFileInfo<int>(param);
                    case (int)TypeToken.Float: return new ByteFileInfo<float>(param);
                    case (int)TypeToken.ULong: return new ByteFileInfo<ulong>(param);
                    case (int)TypeToken.Long: return new ByteFileInfo<long>(param);
                    case (int)TypeToken.Double: return new ByteFileInfo<double>(param);
                    case (int)TypeToken.String: return new ByteFileInfo<string>(param);
                    // Vector Vector + Dimension * 100 + Type
                    case (int)TypeToken.Vector + 200: return new ByteFileInfo<Vector2>(param);
                    case (int)TypeToken.Vector + 300: return new ByteFileInfo<Vector3>(param);
                    case (int)TypeToken.Vector + 400: return new ByteFileInfo<Vector4>(param);
                    case (int)TypeToken.Vector + 200 + (int)TypeToken.Int: return new ByteFileInfo<Vector2Int>(param);
                    case (int)TypeToken.Vector + 300 + (int)TypeToken.Int: return new ByteFileInfo<Vector3Int>(param);
                    // List
                    case (int)TypeToken.List + (int)TypeToken.Sbyte: return new ByteFileInfo<List<sbyte>>(param);
                    case (int)TypeToken.List + (int)TypeToken.Byte: return new ByteFileInfo<List<byte>>(param);
                    case (int)TypeToken.List + (int)TypeToken.Bool: return new ByteFileInfo<List<bool>>(param);
                    case (int)TypeToken.List + (int)TypeToken.UShort: return new ByteFileInfo<List<ushort>>(param);
                    case (int)TypeToken.List + (int)TypeToken.Short: return new ByteFileInfo<List<short>>(param);
                    case (int)TypeToken.List + (int)TypeToken.UInt: return new ByteFileInfo<List<uint>>(param);
                    case (int)TypeToken.List + (int)TypeToken.Int: return new ByteFileInfo<List<int>>(param);
                    case (int)TypeToken.List + (int)TypeToken.Float: return new ByteFileInfo<List<float>>(param);
                    case (int)TypeToken.List + (int)TypeToken.Double: return new ByteFileInfo<List<double>>(param);
                    case (int)TypeToken.List + (int)TypeToken.ULong: return new ByteFileInfo<List<ulong>>(param);
                    case (int)TypeToken.List + (int)TypeToken.Long: return new ByteFileInfo<List<long>>(param);
                    case (int)TypeToken.List + (int)TypeToken.String: return new ByteFileInfo<List<string>>(param);
                    #region Dict
                    case (int)TypeToken.Dictionary + (int)TypeToken.Sbyte * 100 + (int)TypeToken.Sbyte: return new ByteFileInfo<Dictionary<sbyte, sbyte>>(param);
                    case (int)TypeToken.Dictionary + (int)TypeToken.Sbyte * 100 + (int)TypeToken.Byte: return new ByteFileInfo<Dictionary<sbyte, byte>>(param);
                    case (int)TypeToken.Dictionary + (int)TypeToken.Sbyte * 100 + (int)TypeToken.Bool: return new ByteFileInfo<Dictionary<sbyte, bool>>(param);
                    case (int)TypeToken.Dictionary + (int)TypeToken.Sbyte * 100 + (int)TypeToken.UShort: return new ByteFileInfo<Dictionary<sbyte, ushort>>(param);
                    case (int)TypeToken.Dictionary + (int)TypeToken.Sbyte * 100 + (int)TypeToken.Short: return new ByteFileInfo<Dictionary<sbyte, short>>(param);
                    case (int)TypeToken.Dictionary + (int)TypeToken.Sbyte * 100 + (int)TypeToken.UInt: return new ByteFileInfo<Dictionary<sbyte, uint>>(param);
                    case (int)TypeToken.Dictionary + (int)TypeToken.Sbyte * 100 + (int)TypeToken.Int: return new ByteFileInfo<Dictionary<sbyte, int>>(param);
                    case (int)TypeToken.Dictionary + (int)TypeToken.Sbyte * 100 + (int)TypeToken.Float: return new ByteFileInfo<Dictionary<sbyte, float>>(param);
                    case (int)TypeToken.Dictionary + (int)TypeToken.Sbyte * 100 + (int)TypeToken.Double: return new ByteFileInfo<Dictionary<sbyte, double>>(param);
                    case (int)TypeToken.Dictionary + (int)TypeToken.Sbyte * 100 + (int)TypeToken.ULong: return new ByteFileInfo<Dictionary<sbyte, ulong>>(param);
                    case (int)TypeToken.Dictionary + (int)TypeToken.Sbyte * 100 + (int)TypeToken.Long: return new ByteFileInfo<Dictionary<sbyte, long>>(param);
                    case (int)TypeToken.Dictionary + (int)TypeToken.Sbyte * 100 + (int)TypeToken.String: return new ByteFileInfo<Dictionary<sbyte, string>>(param);

                    case (int)TypeToken.Dictionary + (int)TypeToken.Byte * 100 + (int)TypeToken.Sbyte: return new ByteFileInfo<Dictionary<byte, sbyte>>(param);
                    case (int)TypeToken.Dictionary + (int)TypeToken.Byte * 100 + (int)TypeToken.Byte: return new ByteFileInfo<Dictionary<byte, byte>>(param);
                    case (int)TypeToken.Dictionary + (int)TypeToken.Byte * 100 + (int)TypeToken.Bool: return new ByteFileInfo<Dictionary<byte, bool>>(param);
                    case (int)TypeToken.Dictionary + (int)TypeToken.Byte * 100 + (int)TypeToken.UShort: return new ByteFileInfo<Dictionary<byte, ushort>>(param);
                    case (int)TypeToken.Dictionary + (int)TypeToken.Byte * 100 + (int)TypeToken.Short: return new ByteFileInfo<Dictionary<byte, short>>(param);
                    case (int)TypeToken.Dictionary + (int)TypeToken.Byte * 100 + (int)TypeToken.UInt: return new ByteFileInfo<Dictionary<byte, uint>>(param);
                    case (int)TypeToken.Dictionary + (int)TypeToken.Byte * 100 + (int)TypeToken.Int: return new ByteFileInfo<Dictionary<byte, int>>(param);
                    case (int)TypeToken.Dictionary + (int)TypeToken.Byte * 100 + (int)TypeToken.Float: return new ByteFileInfo<Dictionary<byte, float>>(param);
                    case (int)TypeToken.Dictionary + (int)TypeToken.Byte * 100 + (int)TypeToken.Double: return new ByteFileInfo<Dictionary<byte, double>>(param);
                    case (int)TypeToken.Dictionary + (int)TypeToken.Byte * 100 + (int)TypeToken.ULong: return new ByteFileInfo<Dictionary<byte, ulong>>(param);
                    case (int)TypeToken.Dictionary + (int)TypeToken.Byte * 100 + (int)TypeToken.Long: return new ByteFileInfo<Dictionary<byte, long>>(param);
                    case (int)TypeToken.Dictionary + (int)TypeToken.Byte * 100 + (int)TypeToken.String: return new ByteFileInfo<Dictionary<byte, string>>(param);

                    case (int)TypeToken.Dictionary + (int)TypeToken.Bool * 100 + (int)TypeToken.Sbyte: return new ByteFileInfo<Dictionary<bool, sbyte>>(param);
                    case (int)TypeToken.Dictionary + (int)TypeToken.Bool * 100 + (int)TypeToken.Byte: return new ByteFileInfo<Dictionary<bool, byte>>(param);
                    case (int)TypeToken.Dictionary + (int)TypeToken.Bool * 100 + (int)TypeToken.Bool: return new ByteFileInfo<Dictionary<bool, bool>>(param);
                    case (int)TypeToken.Dictionary + (int)TypeToken.Bool * 100 + (int)TypeToken.UShort: return new ByteFileInfo<Dictionary<bool, ushort>>(param);
                    case (int)TypeToken.Dictionary + (int)TypeToken.Bool * 100 + (int)TypeToken.Short: return new ByteFileInfo<Dictionary<bool, short>>(param);
                    case (int)TypeToken.Dictionary + (int)TypeToken.Bool * 100 + (int)TypeToken.UInt: return new ByteFileInfo<Dictionary<bool, uint>>(param);
                    case (int)TypeToken.Dictionary + (int)TypeToken.Bool * 100 + (int)TypeToken.Int: return new ByteFileInfo<Dictionary<bool, int>>(param);
                    case (int)TypeToken.Dictionary + (int)TypeToken.Bool * 100 + (int)TypeToken.Float: return new ByteFileInfo<Dictionary<bool, float>>(param);
                    case (int)TypeToken.Dictionary + (int)TypeToken.Bool * 100 + (int)TypeToken.Double: return new ByteFileInfo<Dictionary<bool, double>>(param);
                    case (int)TypeToken.Dictionary + (int)TypeToken.Bool * 100 + (int)TypeToken.ULong: return new ByteFileInfo<Dictionary<bool, ulong>>(param);
                    case (int)TypeToken.Dictionary + (int)TypeToken.Bool * 100 + (int)TypeToken.Long: return new ByteFileInfo<Dictionary<bool, long>>(param);
                    case (int)TypeToken.Dictionary + (int)TypeToken.Bool * 100 + (int)TypeToken.String: return new ByteFileInfo<Dictionary<bool, string>>(param);

                    case (int)TypeToken.Dictionary + (int)TypeToken.UShort * 100 + (int)TypeToken.Sbyte: return new ByteFileInfo<Dictionary<ushort, sbyte>>(param);
                    case (int)TypeToken.Dictionary + (int)TypeToken.UShort * 100 + (int)TypeToken.Byte: return new ByteFileInfo<Dictionary<ushort, byte>>(param);
                    case (int)TypeToken.Dictionary + (int)TypeToken.UShort * 100 + (int)TypeToken.Bool: return new ByteFileInfo<Dictionary<ushort, bool>>(param);
                    case (int)TypeToken.Dictionary + (int)TypeToken.UShort * 100 + (int)TypeToken.UShort: return new ByteFileInfo<Dictionary<ushort, ushort>>(param);
                    case (int)TypeToken.Dictionary + (int)TypeToken.UShort * 100 + (int)TypeToken.Short: return new ByteFileInfo<Dictionary<ushort, short>>(param);
                    case (int)TypeToken.Dictionary + (int)TypeToken.UShort * 100 + (int)TypeToken.UInt: return new ByteFileInfo<Dictionary<ushort, uint>>(param);
                    case (int)TypeToken.Dictionary + (int)TypeToken.UShort * 100 + (int)TypeToken.Int: return new ByteFileInfo<Dictionary<ushort, int>>(param);
                    case (int)TypeToken.Dictionary + (int)TypeToken.UShort * 100 + (int)TypeToken.Float: return new ByteFileInfo<Dictionary<ushort, float>>(param);
                    case (int)TypeToken.Dictionary + (int)TypeToken.UShort * 100 + (int)TypeToken.Double: return new ByteFileInfo<Dictionary<ushort, double>>(param);
                    case (int)TypeToken.Dictionary + (int)TypeToken.UShort * 100 + (int)TypeToken.ULong: return new ByteFileInfo<Dictionary<ushort, ulong>>(param);
                    case (int)TypeToken.Dictionary + (int)TypeToken.UShort * 100 + (int)TypeToken.Long: return new ByteFileInfo<Dictionary<ushort, long>>(param);
                    case (int)TypeToken.Dictionary + (int)TypeToken.UShort * 100 + (int)TypeToken.String: return new ByteFileInfo<Dictionary<ushort, string>>(param);

                    case (int)TypeToken.Dictionary + (int)TypeToken.Short * 100 + (int)TypeToken.Sbyte: return new ByteFileInfo<Dictionary<short, sbyte>>(param);
                    case (int)TypeToken.Dictionary + (int)TypeToken.Short * 100 + (int)TypeToken.Byte: return new ByteFileInfo<Dictionary<short, byte>>(param);
                    case (int)TypeToken.Dictionary + (int)TypeToken.Short * 100 + (int)TypeToken.Bool: return new ByteFileInfo<Dictionary<short, bool>>(param);
                    case (int)TypeToken.Dictionary + (int)TypeToken.Short * 100 + (int)TypeToken.UShort: return new ByteFileInfo<Dictionary<short, ushort>>(param);
                    case (int)TypeToken.Dictionary + (int)TypeToken.Short * 100 + (int)TypeToken.Short: return new ByteFileInfo<Dictionary<short, short>>(param);
                    case (int)TypeToken.Dictionary + (int)TypeToken.Short * 100 + (int)TypeToken.UInt: return new ByteFileInfo<Dictionary<short, uint>>(param);
                    case (int)TypeToken.Dictionary + (int)TypeToken.Short * 100 + (int)TypeToken.Int: return new ByteFileInfo<Dictionary<short, int>>(param);
                    case (int)TypeToken.Dictionary + (int)TypeToken.Short * 100 + (int)TypeToken.Float: return new ByteFileInfo<Dictionary<short, float>>(param);
                    case (int)TypeToken.Dictionary + (int)TypeToken.Short * 100 + (int)TypeToken.Double: return new ByteFileInfo<Dictionary<short, double>>(param);
                    case (int)TypeToken.Dictionary + (int)TypeToken.Short * 100 + (int)TypeToken.ULong: return new ByteFileInfo<Dictionary<short, ulong>>(param);
                    case (int)TypeToken.Dictionary + (int)TypeToken.Short * 100 + (int)TypeToken.Long: return new ByteFileInfo<Dictionary<short, long>>(param);
                    case (int)TypeToken.Dictionary + (int)TypeToken.Short * 100 + (int)TypeToken.String: return new ByteFileInfo<Dictionary<short, string>>(param);

                    case (int)TypeToken.Dictionary + (int)TypeToken.UInt * 100 + (int)TypeToken.Sbyte: return new ByteFileInfo<Dictionary<uint, sbyte>>(param);
                    case (int)TypeToken.Dictionary + (int)TypeToken.UInt * 100 + (int)TypeToken.Byte: return new ByteFileInfo<Dictionary<uint, byte>>(param);
                    case (int)TypeToken.Dictionary + (int)TypeToken.UInt * 100 + (int)TypeToken.Bool: return new ByteFileInfo<Dictionary<uint, bool>>(param);
                    case (int)TypeToken.Dictionary + (int)TypeToken.UInt * 100 + (int)TypeToken.UShort: return new ByteFileInfo<Dictionary<uint, ushort>>(param);
                    case (int)TypeToken.Dictionary + (int)TypeToken.UInt * 100 + (int)TypeToken.Short: return new ByteFileInfo<Dictionary<uint, short>>(param);
                    case (int)TypeToken.Dictionary + (int)TypeToken.UInt * 100 + (int)TypeToken.UInt: return new ByteFileInfo<Dictionary<uint, uint>>(param);
                    case (int)TypeToken.Dictionary + (int)TypeToken.UInt * 100 + (int)TypeToken.Int: return new ByteFileInfo<Dictionary<uint, int>>(param);
                    case (int)TypeToken.Dictionary + (int)TypeToken.UInt * 100 + (int)TypeToken.Float: return new ByteFileInfo<Dictionary<uint, float>>(param);
                    case (int)TypeToken.Dictionary + (int)TypeToken.UInt * 100 + (int)TypeToken.Double: return new ByteFileInfo<Dictionary<uint, double>>(param);
                    case (int)TypeToken.Dictionary + (int)TypeToken.UInt * 100 + (int)TypeToken.ULong: return new ByteFileInfo<Dictionary<uint, ulong>>(param);
                    case (int)TypeToken.Dictionary + (int)TypeToken.UInt * 100 + (int)TypeToken.Long: return new ByteFileInfo<Dictionary<uint, long>>(param);
                    case (int)TypeToken.Dictionary + (int)TypeToken.UInt * 100 + (int)TypeToken.String: return new ByteFileInfo<Dictionary<uint, string>>(param);

                    case (int)TypeToken.Dictionary + (int)TypeToken.Int * 100 + (int)TypeToken.Sbyte: return new ByteFileInfo<Dictionary<int, sbyte>>(param);
                    case (int)TypeToken.Dictionary + (int)TypeToken.Int * 100 + (int)TypeToken.Byte: return new ByteFileInfo<Dictionary<int, byte>>(param);
                    case (int)TypeToken.Dictionary + (int)TypeToken.Int * 100 + (int)TypeToken.Bool: return new ByteFileInfo<Dictionary<int, bool>>(param);
                    case (int)TypeToken.Dictionary + (int)TypeToken.Int * 100 + (int)TypeToken.UShort: return new ByteFileInfo<Dictionary<int, ushort>>(param);
                    case (int)TypeToken.Dictionary + (int)TypeToken.Int * 100 + (int)TypeToken.Short: return new ByteFileInfo<Dictionary<int, short>>(param);
                    case (int)TypeToken.Dictionary + (int)TypeToken.Int * 100 + (int)TypeToken.UInt: return new ByteFileInfo<Dictionary<int, uint>>(param);
                    case (int)TypeToken.Dictionary + (int)TypeToken.Int * 100 + (int)TypeToken.Int: return new ByteFileInfo<Dictionary<int, int>>(param);
                    case (int)TypeToken.Dictionary + (int)TypeToken.Int * 100 + (int)TypeToken.Float: return new ByteFileInfo<Dictionary<int, float>>(param);
                    case (int)TypeToken.Dictionary + (int)TypeToken.Int * 100 + (int)TypeToken.Double: return new ByteFileInfo<Dictionary<int, double>>(param);
                    case (int)TypeToken.Dictionary + (int)TypeToken.Int * 100 + (int)TypeToken.ULong: return new ByteFileInfo<Dictionary<int, ulong>>(param);
                    case (int)TypeToken.Dictionary + (int)TypeToken.Int * 100 + (int)TypeToken.Long: return new ByteFileInfo<Dictionary<int, long>>(param);
                    case (int)TypeToken.Dictionary + (int)TypeToken.Int * 100 + (int)TypeToken.String: return new ByteFileInfo<Dictionary<int, string>>(param);

                    case (int)TypeToken.Dictionary + (int)TypeToken.Float * 100 + (int)TypeToken.Sbyte: return new ByteFileInfo<Dictionary<float, sbyte>>(param);
                    case (int)TypeToken.Dictionary + (int)TypeToken.Float * 100 + (int)TypeToken.Byte: return new ByteFileInfo<Dictionary<float, byte>>(param);
                    case (int)TypeToken.Dictionary + (int)TypeToken.Float * 100 + (int)TypeToken.Bool: return new ByteFileInfo<Dictionary<float, bool>>(param);
                    case (int)TypeToken.Dictionary + (int)TypeToken.Float * 100 + (int)TypeToken.UShort: return new ByteFileInfo<Dictionary<float, ushort>>(param);
                    case (int)TypeToken.Dictionary + (int)TypeToken.Float * 100 + (int)TypeToken.Short: return new ByteFileInfo<Dictionary<float, short>>(param);
                    case (int)TypeToken.Dictionary + (int)TypeToken.Float * 100 + (int)TypeToken.UInt: return new ByteFileInfo<Dictionary<float, uint>>(param);
                    case (int)TypeToken.Dictionary + (int)TypeToken.Float * 100 + (int)TypeToken.Int: return new ByteFileInfo<Dictionary<float, int>>(param);
                    case (int)TypeToken.Dictionary + (int)TypeToken.Float * 100 + (int)TypeToken.Float: return new ByteFileInfo<Dictionary<float, float>>(param);
                    case (int)TypeToken.Dictionary + (int)TypeToken.Float * 100 + (int)TypeToken.Double: return new ByteFileInfo<Dictionary<float, double>>(param);
                    case (int)TypeToken.Dictionary + (int)TypeToken.Float * 100 + (int)TypeToken.ULong: return new ByteFileInfo<Dictionary<float, ulong>>(param);
                    case (int)TypeToken.Dictionary + (int)TypeToken.Float * 100 + (int)TypeToken.Long: return new ByteFileInfo<Dictionary<float, long>>(param);
                    case (int)TypeToken.Dictionary + (int)TypeToken.Float * 100 + (int)TypeToken.String: return new ByteFileInfo<Dictionary<float, string>>(param);

                    case (int)TypeToken.Dictionary + (int)TypeToken.Double * 100 + (int)TypeToken.Sbyte: return new ByteFileInfo<Dictionary<double, sbyte>>(param);
                    case (int)TypeToken.Dictionary + (int)TypeToken.Double * 100 + (int)TypeToken.Byte: return new ByteFileInfo<Dictionary<double, byte>>(param);
                    case (int)TypeToken.Dictionary + (int)TypeToken.Double * 100 + (int)TypeToken.Bool: return new ByteFileInfo<Dictionary<double, bool>>(param);
                    case (int)TypeToken.Dictionary + (int)TypeToken.Double * 100 + (int)TypeToken.UShort: return new ByteFileInfo<Dictionary<double, ushort>>(param);
                    case (int)TypeToken.Dictionary + (int)TypeToken.Double * 100 + (int)TypeToken.Short: return new ByteFileInfo<Dictionary<double, short>>(param);
                    case (int)TypeToken.Dictionary + (int)TypeToken.Double * 100 + (int)TypeToken.UInt: return new ByteFileInfo<Dictionary<double, uint>>(param);
                    case (int)TypeToken.Dictionary + (int)TypeToken.Double * 100 + (int)TypeToken.Int: return new ByteFileInfo<Dictionary<double, int>>(param);
                    case (int)TypeToken.Dictionary + (int)TypeToken.Double * 100 + (int)TypeToken.Float: return new ByteFileInfo<Dictionary<double, float>>(param);
                    case (int)TypeToken.Dictionary + (int)TypeToken.Double * 100 + (int)TypeToken.Double: return new ByteFileInfo<Dictionary<double, double>>(param);
                    case (int)TypeToken.Dictionary + (int)TypeToken.Double * 100 + (int)TypeToken.ULong: return new ByteFileInfo<Dictionary<double, ulong>>(param);
                    case (int)TypeToken.Dictionary + (int)TypeToken.Double * 100 + (int)TypeToken.Long: return new ByteFileInfo<Dictionary<double, long>>(param);
                    case (int)TypeToken.Dictionary + (int)TypeToken.Double * 100 + (int)TypeToken.String: return new ByteFileInfo<Dictionary<double, string>>(param);

                    case (int)TypeToken.Dictionary + (int)TypeToken.ULong * 100 + (int)TypeToken.Sbyte: return new ByteFileInfo<Dictionary<ulong, sbyte>>(param);
                    case (int)TypeToken.Dictionary + (int)TypeToken.ULong * 100 + (int)TypeToken.Byte: return new ByteFileInfo<Dictionary<ulong, byte>>(param);
                    case (int)TypeToken.Dictionary + (int)TypeToken.ULong * 100 + (int)TypeToken.Bool: return new ByteFileInfo<Dictionary<ulong, bool>>(param);
                    case (int)TypeToken.Dictionary + (int)TypeToken.ULong * 100 + (int)TypeToken.UShort: return new ByteFileInfo<Dictionary<ulong, ushort>>(param);
                    case (int)TypeToken.Dictionary + (int)TypeToken.ULong * 100 + (int)TypeToken.Short: return new ByteFileInfo<Dictionary<ulong, short>>(param);
                    case (int)TypeToken.Dictionary + (int)TypeToken.ULong * 100 + (int)TypeToken.UInt: return new ByteFileInfo<Dictionary<ulong, uint>>(param);
                    case (int)TypeToken.Dictionary + (int)TypeToken.ULong * 100 + (int)TypeToken.Int: return new ByteFileInfo<Dictionary<ulong, int>>(param);
                    case (int)TypeToken.Dictionary + (int)TypeToken.ULong * 100 + (int)TypeToken.Float: return new ByteFileInfo<Dictionary<ulong, float>>(param);
                    case (int)TypeToken.Dictionary + (int)TypeToken.ULong * 100 + (int)TypeToken.Double: return new ByteFileInfo<Dictionary<ulong, double>>(param);
                    case (int)TypeToken.Dictionary + (int)TypeToken.ULong * 100 + (int)TypeToken.ULong: return new ByteFileInfo<Dictionary<ulong, ulong>>(param);
                    case (int)TypeToken.Dictionary + (int)TypeToken.ULong * 100 + (int)TypeToken.Long: return new ByteFileInfo<Dictionary<ulong, long>>(param);
                    case (int)TypeToken.Dictionary + (int)TypeToken.ULong * 100 + (int)TypeToken.String: return new ByteFileInfo<Dictionary<ulong, string>>(param);

                    case (int)TypeToken.Dictionary + (int)TypeToken.Long * 100 + (int)TypeToken.Sbyte: return new ByteFileInfo<Dictionary<long, sbyte>>(param);
                    case (int)TypeToken.Dictionary + (int)TypeToken.Long * 100 + (int)TypeToken.Byte: return new ByteFileInfo<Dictionary<long, byte>>(param);
                    case (int)TypeToken.Dictionary + (int)TypeToken.Long * 100 + (int)TypeToken.Bool: return new ByteFileInfo<Dictionary<long, bool>>(param);
                    case (int)TypeToken.Dictionary + (int)TypeToken.Long * 100 + (int)TypeToken.UShort: return new ByteFileInfo<Dictionary<long, ushort>>(param);
                    case (int)TypeToken.Dictionary + (int)TypeToken.Long * 100 + (int)TypeToken.Short: return new ByteFileInfo<Dictionary<long, short>>(param);
                    case (int)TypeToken.Dictionary + (int)TypeToken.Long * 100 + (int)TypeToken.UInt: return new ByteFileInfo<Dictionary<long, uint>>(param);
                    case (int)TypeToken.Dictionary + (int)TypeToken.Long * 100 + (int)TypeToken.Int: return new ByteFileInfo<Dictionary<long, int>>(param);
                    case (int)TypeToken.Dictionary + (int)TypeToken.Long * 100 + (int)TypeToken.Float: return new ByteFileInfo<Dictionary<long, float>>(param);
                    case (int)TypeToken.Dictionary + (int)TypeToken.Long * 100 + (int)TypeToken.Double: return new ByteFileInfo<Dictionary<long, double>>(param);
                    case (int)TypeToken.Dictionary + (int)TypeToken.Long * 100 + (int)TypeToken.ULong: return new ByteFileInfo<Dictionary<long, ulong>>(param);
                    case (int)TypeToken.Dictionary + (int)TypeToken.Long * 100 + (int)TypeToken.Long: return new ByteFileInfo<Dictionary<long, long>>(param);
                    case (int)TypeToken.Dictionary + (int)TypeToken.Long * 100 + (int)TypeToken.String: return new ByteFileInfo<Dictionary<long, string>>(param);

                    case (int)TypeToken.Dictionary + (int)TypeToken.String * 100 + (int)TypeToken.Sbyte: return new ByteFileInfo<Dictionary<string, sbyte>>(param);
                    case (int)TypeToken.Dictionary + (int)TypeToken.String * 100 + (int)TypeToken.Byte: return new ByteFileInfo<Dictionary<string, byte>>(param);
                    case (int)TypeToken.Dictionary + (int)TypeToken.String * 100 + (int)TypeToken.Bool: return new ByteFileInfo<Dictionary<string, bool>>(param);
                    case (int)TypeToken.Dictionary + (int)TypeToken.String * 100 + (int)TypeToken.UShort: return new ByteFileInfo<Dictionary<string, ushort>>(param);
                    case (int)TypeToken.Dictionary + (int)TypeToken.String * 100 + (int)TypeToken.Short: return new ByteFileInfo<Dictionary<string, short>>(param);
                    case (int)TypeToken.Dictionary + (int)TypeToken.String * 100 + (int)TypeToken.UInt: return new ByteFileInfo<Dictionary<string, uint>>(param);
                    case (int)TypeToken.Dictionary + (int)TypeToken.String * 100 + (int)TypeToken.Int: return new ByteFileInfo<Dictionary<string, int>>(param);
                    case (int)TypeToken.Dictionary + (int)TypeToken.String * 100 + (int)TypeToken.Float: return new ByteFileInfo<Dictionary<string, float>>(param);
                    case (int)TypeToken.Dictionary + (int)TypeToken.String * 100 + (int)TypeToken.Double: return new ByteFileInfo<Dictionary<string, double>>(param);
                    case (int)TypeToken.Dictionary + (int)TypeToken.String * 100 + (int)TypeToken.ULong: return new ByteFileInfo<Dictionary<string, ulong>>(param);
                    case (int)TypeToken.Dictionary + (int)TypeToken.String * 100 + (int)TypeToken.Long: return new ByteFileInfo<Dictionary<string, long>>(param);
                    case (int)TypeToken.Dictionary + (int)TypeToken.String * 100 + (int)TypeToken.String: return new ByteFileInfo<Dictionary<string, string>>(param);
                    #endregion
                    default: return null;
                }
            }

            /// <summary>
            /// Get excel file infomation
            /// </summary>
            /// <typeparam name="IdType">The id type.</typeparam>
            /// <param name="excel">Excel name. Param is ExcelName.xxxxxx</param>
            public static ByteFileInfo<IdType> GetByteFileInfo<IdType>(short excel)
            {
                if (byteFilefileInfoDict.TryGetValue(excel, out var ret))
                {
                    return (ByteFileInfo<IdType>)ret;
                }
                D.Error($"未查找到excelId={excel}对应的ByteFileInfo信息");
                return null;
            }

            /// <summary>
            /// Get one item info with type of T.获取一个T类型的元素
            /// </summary>
            /// <typeparam name="T">return a item type of T.返回一个T类型的内容</typeparam>
            /// <typeparam name="IdType">The id type.id 的类型.</typeparam>
            /// <param name="excel">excel name. 表格名字</param>
            /// <param name="id">type of IdType`s id. ID</param>
            /// <param name="variableName">variable name.变量名.</param>
            public static T Get<T, IdType>(short excel, IdType id, int variableName)
            {
                if (byteFilefileInfoDict.TryGetValue(excel, out object fileInfo))
                {
                    return ((ByteFileInfo<IdType>)fileInfo).Get<T>(id, variableName);
                }
                else
                {
                    D.Error($"excelId={excel} 文件不存在");
                    return default(T);
                }
            }

            /// <summary>
            /// Get a dictionary.获取一个字典
            /// </summary>
            /// <typeparam name="K">The key type of dictionary.字典键的类型</typeparam>
            /// <typeparam name="V">The value type of dictionary.字典值的类型</typeparam>
            /// <typeparam name="IdType">The id type.id的类型</typeparam>
            /// <param name="excel">excel name.表格名字</param>
            /// <param name="id">id.ID</param>
            /// <param name="variableName">variable name.变量名</param>
            public static Dictionary<K, V> GetDict<K, V, IdType>(short excel, IdType id, int variableName)
            {
                if (byteFilefileInfoDict.TryGetValue(excel, out object fileInfo))
                {
                    return ((ByteFileInfo<IdType>)fileInfo).GetDict<K, V>(id, variableName);
                }
                else
                {
                    D.Error($"excelId={excel} 文件不存在");
                    return null;
                }
            }
        }

        /// <summary>
        /// Optimize type.优化类型
        /// </summary>
        public enum OptimizeType : byte
        {
            /// <summary>
            /// None
            /// </summary>
            None,
            /// <summary>
            /// 数据为等差数列形式，步长为固定值
            /// </summary>
            Continuity,
            /// <summary>
            /// 数据分为多段，每段内都是连续的，步长为1
            /// </summary>
            Segment,
            /// <summary>
            /// 连续部分为一段，占所有数据的80%以上
            /// </summary>
            PartialContinuity
        }
    }
}
