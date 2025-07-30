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
            byte[] _data;
            const int _filter = 0xffff;

            /// <summary>
            /// The all index in current sheet data.
            /// </summary>
            public TIdType[] Ids { get { if (_cacheIds == null) CacheAllIds(); return _cacheIds; } }
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
            public int ColCount => _typeToken.Count;
            /// <summary>
            /// Current id column name.当前ID列的名字
            /// </summary>
            public string IdColName => _varNames[IdColIndex];
            /// <summary>
            /// 优化类型
            /// </summary>
            public OptimizeType OptimizeType { get; }
            /// <summary>
            /// Judge cache.判断是否缓存
            /// </summary>
            public bool Cache { get; }
            private readonly List<int> _typeToken;
            private readonly List<int> _colOff;
            private readonly List<string> _varNames;
            Dictionary<TIdType, int> _id2RowStartOff;    // id对应行起始偏移

            TIdType[] _cacheIds;
            readonly int _idColOff;

            /* ------ 优化 ------- */
            // 连续类型
            TIdType _firstVal;
            readonly int _step;
            // 分段类型
            readonly List<int> _segmentList;    // 每一段的长度
            List<TIdType> _segmentStartList;    // 每一段开始的元素值
            List<int> _segmentStartOff;         // 每一段开始的偏移值
                                                // 部分连续
            readonly int _continuityCnt;        // 连续部分个数
            readonly int _continuityStartOff;   // 连续部分起始偏移
            TIdType _continuityStartVal;        // 连续部分起始值
            /**********************/

            /// <summary>
            /// Parse the byte file data info.解析当前字节数据信息.
            /// </summary>
            /// <param name="param">参数</param>
            public ByteFileInfo(ByteFileParam param)
            {
                this.Name = param.FileName;
                this.IdColIndex = param.IdColIndex;
                this.RowCount = param.RowCount;
                this.RowLength = param.RowLen;
                this._typeToken = param.Types;
                this._colOff = param.ColOff;
                this._varNames = param.VarNames;
                this.OptimizeType = param.OptimizeType;
                this.Cache = param.Cache;
                this._idColOff = _colOff[IdColIndex];
                this.ExtraInfo = param.ExtraInfo;
                if (OptimizeType == OptimizeType.Continuity)
                {
                    this._step = param.Step;
                }
                else if (OptimizeType == OptimizeType.Segment)
                {
                    this._segmentList = param.SegmentList;
                }
                else if (OptimizeType == OptimizeType.PartialContinuity)
                {
                    this._continuityStartOff = param.ContinuityStartOff;
                    this._continuityCnt = param.ContinuityCnt;
                }
                Parse();
            }

            private void Parse()
            {
                ByteDataLoaded = true;
                _data = Resources.Load<TextAsset>(ExcelDataManager.AllByteFilePath + Name).bytes;
                if (_data.Length > 0)
                {
                    switch (OptimizeType)
                    {
                        case OptimizeType.None:
                            {
                                _id2RowStartOff = new Dictionary<TIdType, int>();
                                _cacheIds = new TIdType[RowCount];
                                for (int i = 0; i < RowCount; i++)
                                {
                                    TIdType id = ByteReader.Read<TIdType>(_data, i * RowLength + _idColOff);
                                    _cacheIds[i] = id;
                                    _id2RowStartOff.Add(id, i * RowLength);
                                }
                                break;
                            }
                        case OptimizeType.Continuity:
                            _firstVal = ByteReader.Read<TIdType>(_data, _colOff[IdColIndex]);
                            break;
                        case OptimizeType.Segment:
                            {
                                _segmentStartOff = new List<int>(_segmentList.Count);
                                _segmentStartList = new List<TIdType>(_segmentList.Count);
                                _segmentStartOff.Add(0);
                                _segmentStartList.Add(ByteReader.Read<TIdType>(_data, _idColOff));
                                int preCnt = _segmentList[0];
                                for (int i = 1; i < _segmentList.Count; i++)
                                {
                                    _segmentStartOff.Add(RowLength * preCnt);
                                    _segmentStartList.Add(ByteReader.Read<TIdType>(_data, preCnt * RowLength + _idColOff));
                                    preCnt += _segmentList[i];
                                }
                                break;
                            }
                        case OptimizeType.PartialContinuity:
                            {
                                _id2RowStartOff = new Dictionary<TIdType, int>();
                                int preCnt = _continuityStartOff / RowLength;
                                for (int i = 0; i < preCnt; i++)
                                {
                                    TIdType id = ByteReader.Read<TIdType>(_data, i * RowLength + _idColOff);
                                    _id2RowStartOff.Add(id, i * RowLength);
                                }
                                _continuityStartVal = ByteReader.Read<TIdType>(_data, preCnt * RowLength + _idColOff);
                                var remainStart = preCnt + _continuityCnt;
                                for (; remainStart < RowCount; remainStart++)
                                {
                                    TIdType id = ByteReader.Read<TIdType>(_data, remainStart * RowLength + _idColOff);
                                    _id2RowStartOff.Add(id, remainStart * RowLength);
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
                _data = null;
            }

            /// <summary>
            /// Load byte data.加载字节shuju
            /// </summary>
            public void LoadByteData()
            {
                if (ByteDataLoaded) return;
                ByteDataLoaded = true;
                _data = Resources.Load<TextAsset>(ExcelDataManager.AllByteFilePath + Name).bytes;
            }

            /// <summary>
            /// Get item`s index .获取变量在一行的索引，即第几个
            /// </summary>
            /// <param name="variableOff">变量偏移</param>
            public int GetIndex(int variableOff)
            {
                for (int i = 0; i < _colOff.Count; i++)
                {
                    if (_colOff[i] == variableOff) return i;
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
                step = this._step;
                firstValue = _firstVal;
            }

            /// <summary>
            /// 获取优化类型为部分连续时的信息（* 仅优化类型为部分时连续可用）
            /// </summary>
            /// <param name="startVal">连续部分起始主列值</param>
            /// <param name="continuityCnt">连续部分长度</param>
            public void GetOptimizeInfo_PartialContinuity(out TIdType startVal, out int continuityCnt)
            {
                startVal = _continuityStartVal;
                continuityCnt = this._continuityCnt;
            }

            /// <summary>
            /// 获取优化类型为分段时的信息（* 仅优化类型为分段时可用）
            /// </summary>
            /// <param name="segmentList"></param>
            /// <param name="segmentStartList"></param>
            public void GetOptimizeInfo_Segment(out List<int> segmentList, out List<TIdType> segmentStartList)
            {
                segmentList = this._segmentList;
                segmentStartList = this._segmentStartList;
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
                var off = variableOff & _filter;
                if (off >= RowLength)
                {
                    D.Error($"{Name} 内不存在此变量: {variableOff >> 16}列");
                    return default(T);
                }
                switch (OptimizeType)
                {
                    case OptimizeType.None:
                        {
                            if (_id2RowStartOff.TryGetValue(id, out int rowStart))
                            {
                                return ByteReader.Read<T>(_data, rowStart + off);
                            }
                            break;
                        }
                    case OptimizeType.Continuity:
                        {
                            int diff = GenericCalc.SubToInt(id, _firstVal);
                            int diffCnt = diff / _step;  // 与第一个元素相差几个元素（包含自身）
                            if (diffCnt >= RowCount || ((diff % _step) != 0))    // diffCnt最大值为RowCount - 1
                            {
                                break;
                            }
                            return ByteReader.Read<T>(_data, diffCnt * RowLength + off);
                        }
                    case OptimizeType.Segment:
                        {
                            for (int i = 0; i < _segmentStartList.Count; i++)
                            {
                                int cnt = _segmentList[i];
                                int diff = GenericCalc.SubToInt(id, _segmentStartList[i]);
                                if (diff >= cnt) continue; // diff最大值为cnt - 1
                                if (diff < 0) break;
                                return ByteReader.Read<T>(_data, _segmentStartOff[i] + diff * RowLength + off);
                            }
                            break;
                        }
                    case OptimizeType.PartialContinuity:
                        {
                            D.Error(id);
                            D.Error(_continuityStartVal);
                            D.Error(_continuityCnt);
                            int diff = GenericCalc.SubToInt(id, _continuityStartVal);
                            // 优先判断是否在连续范围内，因为至少80%概率是在连续范围内
                            if (diff >= 0 && diff < _continuityCnt) // 在连续范围内
                            {
                                return ByteReader.Read<T>(_data, _continuityStartOff + diff * RowLength + off);
                            }
                            if (_id2RowStartOff.TryGetValue(id, out int rowStart))
                            {
                                return ByteReader.Read<T>(_data, rowStart + off);
                            }
                            break;
                        }
                }
                D.Error($"{Name} 内不存在此id: {id}");
                return default;
            }

            /// <summary>
            /// 通过行数和列数获取数据：0 based
            /// </summary>
            public T GetByRowAndIndex<T>(int rowNum, int index)
            {
                // 此处主要用于缓存数据使用，就暂时不做有效验证了
                return ByteReader.Read<T>(_data, rowNum * RowLength + _colOff[index]);
            }

            /// <summary>
            /// 通过行数和列数获取数据：0 based
            /// </summary>
            public Dictionary<K, V> GetDictByRowAndIndex<K, V>(int rowNum, int index)
            {
                // 此处主要用于缓存数据使用，就暂时不做有效验证了
                return ByteReader.ReadDict<K, V>(_data, rowNum * RowLength + _colOff[index]);
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
                    return ByteReader.Read<TIdType>(_data, rowNum * RowLength + _idColOff);
                }
                D.Error($"行数{rowNum}超出范围，必须属于{0}-{RowCount - 1}");
                return default;
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
                var off = variableOff & _filter;
                if (off >= RowLength || RowCount <= 0)
                {
                    D.Error($"{Name} 内不存在此变量: {variableOff >> 16}列");
                    return default;
                }
                List<T> ls = new List<T>(cnt);
                int index = off;
                for (int i = 0; i < cnt; i++)
                {
                    ls.Add(ByteReader.Read<T>(_data, index));
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
                var off = variableOff & _filter;
                if (off >= RowLength)
                {
                    D.Error($"{Name} 内不存在此变量: {variableOff >> 16}列");
                    return null;
                }
                switch (OptimizeType)
                {
                    case OptimizeType.None:
                        {
                            if (_id2RowStartOff.TryGetValue(id, out int rowStart))
                            {
                                return ByteReader.ReadDict<K, V>(_data, rowStart + off);
                            }
                            break;
                        }
                    case OptimizeType.Continuity:
                        {
                            int diff = GenericCalc.SubToInt(id, _firstVal);
                            int diffCnt = diff / _step;  // 与第一个元素相差几个元素（包含自身）
                            if (diffCnt >= RowCount || ((diff % _step) != 0))    // diffCnt最大值为RowCount - 1
                            {
                                break;
                            }
                            return ByteReader.ReadDict<K, V>(_data, diffCnt * RowLength + off);
                        }
                    case OptimizeType.Segment:
                        {
                            for (int i = 0; i < _segmentStartList.Count; i++)
                            {
                                int cnt = _segmentList[i];
                                int diff = GenericCalc.SubToInt(id, _segmentStartList[i]);
                                if (diff >= cnt) continue; // diff最大值为cnt - 1
                                return ByteReader.ReadDict<K, V>(_data, _segmentStartOff[i] + diff * RowLength + off);
                            }
                            break;
                        }
                    case OptimizeType.PartialContinuity:
                        {
                            int diff = GenericCalc.SubToInt(id, _continuityStartVal);
                            // 优先判断是否在连续范围内，因为至少80%概率是在连续范围内
                            if (diff >= 0 && diff < _continuityCnt) // 在连续范围内
                            {
                                return ByteReader.ReadDict<K, V>(_data, _continuityStartOff + diff * RowLength + variableOff);
                            }
                            if (_id2RowStartOff.TryGetValue(id, out int rowStart))
                            {
                                return ByteReader.ReadDict<K, V>(_data, rowStart + off);
                            }
                            break;
                        }
                }
                D.Error($"{Name} 内不存在此id: {id}");
                return null;
            }
            /// <summary>
            /// Reset byte file read manager. 重置字节文件读取
            /// </summary>
            public void ResetByteFileReader() => ByteFileReader.Reset(_data, RowLength, _colOff);

            private void CacheAllIds()
            {
                if (_cacheIds != null) return;
                _cacheIds = new TIdType[RowCount];
                for (int i = 0; i < RowCount; i++)
                {
                    _cacheIds[i] = ByteReader.Read<TIdType>(_data, i * RowLength + _idColOff);
                }
            }
        }

        /// <summary>
        /// Byte file param.字节文件参数
        /// </summary>
        public struct ByteFileParam
        {
            public string FileName;
            public int IdColIndex;
            public int RowCount;
            public int RowLen;
            public List<int> Types;
            public List<int> ColOff;
            public List<string> VarNames;
            public bool Cache;
            public Dictionary<string, string> ExtraInfo;

            // -----优化相关-----
            public OptimizeType OptimizeType;
            // 连续类型
            public int Step;
            // 分段类型
            public List<int> SegmentList;
            // 部分连续
            public int ContinuityStartOff;  // 连续部分起始偏移
            public int ContinuityCnt;       // 连续部分个数
            public long StartVal;           // 连续部分开始值
        }
    }
}
