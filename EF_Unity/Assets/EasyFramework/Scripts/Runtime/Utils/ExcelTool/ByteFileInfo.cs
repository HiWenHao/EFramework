/* 
 * ================================================
 * Describe:      This script is used to .
 * Author:        Xiaohei.Wang(Wenhao)
 * CreationTime:  2023-05-30 14:04:30
 * ModifyAuthor:  Xiaohei.Wang(Wenhao)
 * ModifyTime:    2023-05-30 14:04:30
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
        /// Get a byte file infomation.获取一个字节文件信息.
        /// </summary>
        /// <typeparam name="TIdType">IdType。 IdType的类型</typeparam>
        public sealed class ByteFileInfo<TIdType>
        {
            byte[] data;
            const int filter = 0xffff;

            /// <summary>
            /// The all index in current sheet data.
            /// </summary>
            public TIdType[] Ids { get { if (cacheIds == null) CacheAllIds(); return cacheIds; } }
            /// <summary>
            /// Judge the byte data is load.判断字节数据已经加载
            /// </summary>
            public bool ByteDataLoaded { get; private set; }
            /// <summary>
            /// Extra infomation.额外信息
            /// </summary>
            public Dictionary<string, string> ExtraInfo { get; }
            /// <summary>
            /// Current sheet name.当前数据的名字
            /// </summary>
            public string Name { get; }
            /// <summary>
            /// Index with id colum.
            /// </summary>
            public int IdColIndex { get; }
            /// <summary>
            /// 行数
            /// </summary>
            public int RowCount { get; }
            /// <summary>
            /// 横排长度
            /// </summary>
            public int RowLength { get; }
            /// <summary>
            /// 列数
            /// </summary>
            public int ColCount => typeToken.Count;
            /// <summary>
            /// Current id column name.当前ID列的名字
            /// </summary>
            public string IdColName => varNames[IdColIndex];
            /// <summary>
            /// 优化类型
            /// </summary>
            public OptimizeType OptimizeType { get; }
            /// <summary>
            /// Judge cache.判断是否缓存
            /// </summary>
            public bool Cache { get; }
            private readonly List<int> typeToken;
            private readonly List<int> colOff;
            private readonly List<string> varNames;
            Dictionary<TIdType, int> id2RowStartOff;    // id对应行起始偏移

            TIdType[] cacheIds;
            readonly int idColOff;

            /* ------ 优化 ------- */
            // 连续类型
            TIdType firstVal;
            readonly int step;
            // 分段类型
            readonly List<int> segmentList; // 每一段的长度
            List<TIdType> segmentStartList; // 每一段开始的元素值
            List<int> segmentStartOff;      // 每一段开始的偏移值
                                            // 部分连续
            readonly int continuityCnt;  // 连续部分个数
            readonly int continuityStartOff; // 连续部分起始偏移
            TIdType continuityStartVal; // 连续部分起始值
            /**********************/

            /// <summary>
            /// Parse the byte file data info.解析当前字节数据信息.
            /// </summary>
            /// <param name="param">参数</param>
            public ByteFileInfo(ByteFileParam param)
            {
                this.Name = param.fileName;
                this.IdColIndex = param.idColIndex;
                this.RowCount = param.rowCount;
                this.RowLength = param.rowLen;
                this.typeToken = param.types;
                this.colOff = param.colOff;
                this.varNames = param.varNames;
                this.OptimizeType = param.optimizeType;
                this.Cache = param.cache;
                this.idColOff = colOff[IdColIndex];
                this.ExtraInfo = param.extraInfo;
                if (OptimizeType == OptimizeType.Continuity)
                {
                    this.step = param.step;
                }
                else if (OptimizeType == OptimizeType.Segment)
                {
                    this.segmentList = param.segmentList;
                }
                else if (OptimizeType == OptimizeType.PartialContinuity)
                {
                    this.continuityStartOff = param.continuityStartOff;
                    this.continuityCnt = param.continuityCnt;
                }
                Parse();
            }

            private void Parse()
            {
                ByteDataLoaded = true;
                data = Resources.Load<TextAsset>(ExcelDataManager.AllByteFilePath + Name).bytes;
                if (data.Length > 0)
                {
                    switch (OptimizeType)
                    {
                        case OptimizeType.None:
                            {
                                id2RowStartOff = new Dictionary<TIdType, int>();
                                cacheIds = new TIdType[RowCount];
                                for (int i = 0; i < RowCount; i++)
                                {
                                    TIdType id = ByteReader.Read<TIdType>(data, i * RowLength + idColOff);
                                    cacheIds[i] = id;
                                    id2RowStartOff.Add(id, i * RowLength);
                                }
                                break;
                            }
                        case OptimizeType.Continuity:
                            firstVal = ByteReader.Read<TIdType>(data, colOff[IdColIndex]);
                            break;
                        case OptimizeType.Segment:
                            {
                                segmentStartOff = new List<int>(segmentList.Count);
                                segmentStartList = new List<TIdType>(segmentList.Count);
                                segmentStartOff.Add(0);
                                segmentStartList.Add(ByteReader.Read<TIdType>(data, idColOff));
                                int preCnt = segmentList[0];
                                for (int i = 1; i < segmentList.Count; i++)
                                {
                                    segmentStartOff.Add(RowLength * preCnt);
                                    segmentStartList.Add(ByteReader.Read<TIdType>(data, preCnt * RowLength + idColOff));
                                    preCnt += segmentList[i];
                                }
                                break;
                            }
                        case OptimizeType.PartialContinuity:
                            {
                                id2RowStartOff = new Dictionary<TIdType, int>();
                                int preCnt = continuityStartOff / RowLength;
                                for (int i = 0; i < preCnt; i++)
                                {
                                    TIdType id = ByteReader.Read<TIdType>(data, i * RowLength + idColOff);
                                    id2RowStartOff.Add(id, i * RowLength);
                                }
                                continuityStartVal = ByteReader.Read<TIdType>(data, preCnt * RowLength + idColOff);
                                var remainStart = preCnt + continuityCnt;
                                for (; remainStart < RowCount; remainStart++)
                                {
                                    TIdType id = ByteReader.Read<TIdType>(data, remainStart * RowLength + idColOff);
                                    id2RowStartOff.Add(id, remainStart * RowLength);
                                }
                                break;
                            }
                    }
                }
            }

            /// <summary>
            /// Unload byte data.卸载字节数据.
            /// </summary>
            public void UnloadByteData()
            {
                ByteDataLoaded = false;
                data = null;
            }

            /// <summary>
            /// Load byte data.加载字节shuju
            /// </summary>
            public void LoadByteData()
            {
                if (ByteDataLoaded) return;
                ByteDataLoaded = true;
                data = Resources.Load<TextAsset>(ExcelDataManager.AllByteFilePath + Name).bytes;
            }

            /// <summary>
            /// Get item`s index .获取变量在一行的索引，即第几个
            /// </summary>
            /// <param name="variableOff">变量偏移</param>
            public int GetIndex(int variableOff)
            {
                for (int i = 0; i < colOff.Count; i++)
                {
                    if (colOff[i] == variableOff) return i;
                }
                return -1;
            }

            /// <summary>
            /// 获取优化类型为连续时的信息（* 仅优化类型为连续时可用）
            /// </summary>
            /// <param name="step">步长</param>
            /// <param name="firstValue">第一个元素值</param>
            public void GetOptimizeInfo_Continuity(out int step, out TIdType firstValue)
            {
                step = this.step;
                firstValue = firstVal;
            }

            /// <summary>
            /// 获取优化类型为部分连续时的信息（* 仅优化类型为部分时连续可用）
            /// </summary>
            /// <param name="startVal">连续部分起始主列值</param>
            /// <param name="continuityCnt">连续部分长度</param>
            public void GetOptimizeInfo_PartialContinuity(out TIdType startVal, out int continuityCnt)
            {
                startVal = continuityStartVal;
                continuityCnt = this.continuityCnt;
            }

            /// <summary>
            /// 获取优化类型为分段时的信息（* 仅优化类型为分段时可用）
            /// </summary>
            /// <param name="segmentList"></param>
            /// <param name="segmentStartList"></param>
            public void GetOptimizeInfo_Segment(out List<int> segmentList, out List<TIdType> segmentStartList)
            {
                segmentList = this.segmentList;
                segmentStartList = this.segmentStartList;
            }

            /// <summary>
            /// Get one item of T type.获取T类型的一个元素
            /// </summary>
            /// <typeparam name="T">T Type</typeparam>
            /// <param name="id">id.</param>
            /// <param name="variableOff">Variable offset.变量便宜</param>
            /// <returns></returns>
            public T Get<T>(TIdType id, int variableOff)
            {
                var off = variableOff & filter;
                if (off >= RowLength)
                {
                    Debug.LogError($"{Name} 内不存在此变量: {variableOff >> 16}列");
                    return default(T);
                }
                switch (OptimizeType)
                {
                    case OptimizeType.None:
                        {
                            if (id2RowStartOff.TryGetValue(id, out int rowStart))
                            {
                                return ByteReader.Read<T>(data, rowStart + off);
                            }
                            break;
                        }
                    case OptimizeType.Continuity:
                        {
                            int diff = GenericCalc.SubToInt(id, firstVal);
                            int diffCnt = diff / step;  // 与第一个元素相差几个元素（包含自身）
                            if (diffCnt >= RowCount || ((diff % step) != 0))    // diffCnt最大值为RowCount - 1
                            {
                                break;
                            }
                            return ByteReader.Read<T>(data, diffCnt * RowLength + off);
                        }
                    case OptimizeType.Segment:
                        {
                            for (int i = 0; i < segmentStartList.Count; i++)
                            {
                                int cnt = segmentList[i];
                                int diff = GenericCalc.SubToInt(id, segmentStartList[i]);
                                if (diff >= cnt) continue; // diff最大值为cnt - 1
                                if (diff < 0) break;
                                return ByteReader.Read<T>(data, segmentStartOff[i] + diff * RowLength + off);
                            }
                            break;
                        }
                    case OptimizeType.PartialContinuity:
                        {
                            Debug.LogError(id);
                            Debug.LogError(continuityStartVal);
                            Debug.LogError(continuityCnt);
                            int diff = GenericCalc.SubToInt(id, continuityStartVal);
                            // 优先判断是否在连续范围内，因为至少80%概率是在连续范围内
                            if (diff >= 0 && diff < continuityCnt) // 在连续范围内
                            {
                                return ByteReader.Read<T>(data, continuityStartOff + diff * RowLength + off);
                            }
                            if (id2RowStartOff.TryGetValue(id, out int rowStart))
                            {
                                return ByteReader.Read<T>(data, rowStart + off);
                            }
                            break;
                        }
                }
                Debug.LogError($"{Name} 内不存在此id: {id}");
                return default(T);
            }

            /// <summary>
            /// 通过行数和列数获取数据：0 based
            /// </summary>
            public T GetByRowAndIndex<T>(int rowNum, int index)
            {
                // 此处主要用于缓存数据使用，就暂时不做有效验证了
                return ByteReader.Read<T>(data, rowNum * RowLength + colOff[index]);
            }

            /// <summary>
            /// 通过行数和列数获取数据：0 based
            /// </summary>
            public Dictionary<K, V> GetDictByRowAndIndex<K, V>(int rowNum, int index)
            {
                // 此处主要用于缓存数据使用，就暂时不做有效验证了
                return ByteReader.ReadDict<K, V>(data, rowNum * RowLength + colOff[index]);
            }

            /// <summary>
            /// 获取第x行的主列值
            /// </summary>
            /// <param name="rowNum">行数（0 based）</param>
            public TIdType GetKey(int rowNum)
            {
                if (OptimizeType == OptimizeType.None) return Ids[rowNum];
                if (rowNum >= 0 && rowNum < RowCount)
                {
                    return ByteReader.Read<TIdType>(data, rowNum * RowLength + idColOff);
                }
                Debug.LogError($"行数{rowNum}超出范围，必须属于{0}-{RowCount - 1}");
                return default(TIdType);
            }

            /// <summary>
            /// Get a column list data.获取一列T类型数据
            /// </summary>
            /// <typeparam name="T">T type.T类型</typeparam>
            /// <param name="variableOff">variable offset.变量偏移</param>
            /// <returns></returns>
            public List<T> GetOneCol<T>(int variableOff)
            {
                return GetOneCol<T>(variableOff, RowCount);
            }

            /// <summary>
            /// Get a column list data.获取一列T类型数据
            /// </summary>
            /// <typeparam name="T">T type.T类型</typeparam>
            /// <param name="variableOff">variable offset.变量偏移</param>
            /// <param name="cnt">count. 数量</param>
            public List<T> GetOneCol<T>(int variableOff, int cnt)
            {
                var off = variableOff & filter;
                if (off >= RowLength || RowCount <= 0)
                {
                    Debug.LogError($"{Name} 内不存在此变量: {variableOff >> 16}列");
                    return default(List<T>);
                }
                List<T> ls = new List<T>(cnt);
                int index = off;
                for (int i = 0; i < cnt; i++)
                {
                    ls.Add(ByteReader.Read<T>(data, index));
                    index += RowLength;
                }
                return ls;
            }

            /// <summary>
            /// Get a dictionary. 获取一个字典
            /// </summary>
            /// <param name="variableOff">variable offset.变量偏移</param>
            /// <returns></returns>
            public Dictionary<K, V> GetDict<K, V>(TIdType id, int variableOff)
            {
                var off = variableOff & filter;
                if (off >= RowLength)
                {
                    Debug.LogError($"{Name} 内不存在此变量: {variableOff >> 16}列");
                    return null;
                }
                switch (OptimizeType)
                {
                    case OptimizeType.None:
                        {
                            if (id2RowStartOff.TryGetValue(id, out int rowStart))
                            {
                                return ByteReader.ReadDict<K, V>(data, rowStart + off);
                            }
                            break;
                        }
                    case OptimizeType.Continuity:
                        {
                            int diff = GenericCalc.SubToInt(id, firstVal);
                            int diffCnt = diff / step;  // 与第一个元素相差几个元素（包含自身）
                            if (diffCnt >= RowCount || ((diff % step) != 0))    // diffCnt最大值为RowCount - 1
                            {
                                break;
                            }
                            return ByteReader.ReadDict<K, V>(data, diffCnt * RowLength + off);
                        }
                    case OptimizeType.Segment:
                        {
                            for (int i = 0; i < segmentStartList.Count; i++)
                            {
                                int cnt = segmentList[i];
                                int diff = GenericCalc.SubToInt(id, segmentStartList[i]);
                                if (diff >= cnt) continue; // diff最大值为cnt - 1
                                return ByteReader.ReadDict<K, V>(data, segmentStartOff[i] + diff * RowLength + off);
                            }
                            break;
                        }
                    case OptimizeType.PartialContinuity:
                        {
                            int diff = GenericCalc.SubToInt(id, continuityStartVal);
                            // 优先判断是否在连续范围内，因为至少80%概率是在连续范围内
                            if (diff >= 0 && diff < continuityCnt) // 在连续范围内
                            {
                                return ByteReader.ReadDict<K, V>(data, continuityStartOff + diff * RowLength + variableOff);
                            }
                            if (id2RowStartOff.TryGetValue(id, out int rowStart))
                            {
                                return ByteReader.ReadDict<K, V>(data, rowStart + off);
                            }
                            break;
                        }
                }
                Debug.LogError($"{Name} 内不存在此id: {id}");
                return null;
            }
            /// <summary>
            /// Reset byte file read manager. 重置字节文件读取
            /// </summary>
            public void ResetByteFileReader() => ByteFileReader.Reset(data, RowLength, colOff);

            private void CacheAllIds()
            {
                if (cacheIds != null) return;
                cacheIds = new TIdType[RowCount];
                for (int i = 0; i < RowCount; i++)
                {
                    cacheIds[i] = ByteReader.Read<TIdType>(data, i * RowLength + idColOff);
                }
            }
        }

        /// <summary>
        /// Byte file param.字节文件参数
        /// </summary>
        public struct ByteFileParam
        {
            public string fileName;
            public int idColIndex;
            public int rowCount;
            public int rowLen;
            public List<int> types;
            public List<int> colOff;
            public List<string> varNames;
            public bool cache;
            public Dictionary<string, string> extraInfo;

            // -----优化相关-----
            public OptimizeType optimizeType;
            // 连续类型
            public int step;
            // 分段类型
            public List<int> segmentList;
            // 部分连续
            public int continuityStartOff;  // 连续部分起始偏移
            public int continuityCnt;       // 连续部分个数
            public long startVal;           // 连续部分开始值
        }
    }
}
