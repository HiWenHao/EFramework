/* 
 * ================================================
 * Describe:      This script is used to .
 * Author:        Xiaohei.Wang(Wenhao)
 * CreationTime:  2023-05-30 14:05:01
 * ModifyAuthor:  Xiaohei.Wang(Wenhao)
 * ModifyTime:    2023-05-30 14:05:01
 * ScriptVersion: 0.1
 * ===============================================
*/
using System.Collections.Generic;

namespace EasyFramework
{
    namespace ExcelTool
    {
        /// <summary>
        /// All byte file read manager. 用于读取所有的数据，自动增长
        /// </summary>
        public class ByteFileReader
        {
            static byte[] data;
            static int row = 0;
            static int col = 0;
            static int colCnt;
            static int rowLength;
            static List<int> colOff;

            //public ByteFileReader(byte[] data, int rowLength, List<int> colOff)
            //{
            //    this.data = data;
            //    this.colCnt = colOff.Count;
            //    this.rowLength = rowLength;
            //    this.colOff = colOff;
            //}

            /// <summary>
            /// Reset the byte array data.重置字节数组数据
            /// </summary>
            /// <param name="data1">Byte array data。字节数组数据</param>
            /// <param name="rowLength1">The row length.行排长度</param>
            /// <param name="colOff1">The column length.竖排长度</param>
            public static void Reset(byte[] data1, int rowLength1, List<int> colOff1)
            {
                data = data1;
                colCnt = colOff1.Count;
                rowLength = rowLength1;
                colOff = colOff1;
                row = 0;
                col = 0;
            }

            /// <summary>
            /// Get one item type of T.获取一个T类型的元素.
            /// </summary>
            /// <typeparam name="T">The T type.</typeparam>
            public static T Get<T>()
            {
                var ret = ByteReader.Read<T>(data, row * rowLength + colOff[col]);
                col++;
                if (col >= colCnt)
                {
                    col = 0;
                    row++;
                }
                return ret;
            }

            /// <summary>
            /// Get a dictionary.获取一个字典
            /// </summary>
            public static Dictionary<K, V> GetDict<K, V>()
            {
                var ret = ByteReader.ReadDict<K, V>(data, row * rowLength + colOff[col]);
                col++;
                if (col >= colCnt)
                {
                    col = 0;
                    row++;
                }
                return ret;
            }

            /// <summary>
            /// Get a type of T data by row and index.通过行数和列数获取数据：0 based
            /// </summary>
            public T GetByRowAndIndex<T>(int rowNum, int index)
            {
                // 此处主要用于缓存数据使用，就暂时不做有效验证了
                return ByteReader.Read<T>(data, rowNum * rowLength + colOff[index]);
            }

            /// <summary>
            /// Get a dictionary data by row and index.通过行数和列数获取数据：0 based
            /// </summary>
            public Dictionary<K, V> GetDictByRowAndIndex<K, V>(int rowNum, int index)
            {
                // 此处主要用于缓存数据使用，就暂时不做有效验证了
                return ByteReader.ReadDict<K, V>(data, rowNum * rowLength + colOff[index]);
            }

            /// <summary>
            /// Skin one item data.跳过一个元素数据
            /// </summary>
            public static void SkipOne()
            {
                col++;
                if (col >= colCnt)
                {
                    col = 0;
                    row++;
                }
            }
        }
    }
}
