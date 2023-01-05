using System;
using System.IO;
using System.Security.Cryptography;
using UnityEngine;

namespace CustomUwrDownload
{
    /// <summary>
    /// 单个文件处理
    /// </summary>
    internal class UwrFileHandling
    {
        #region 创建文件
        /// <summary>
        /// 创建文件
        /// </summary>
        /// <param name="path">保存文件的路径</param>
        /// <param name="bytes">文件的字节数组</param>
        /// <param name="callback">创建完成后的回调</param>
        /// <param name="md5">当前要下载文件的MD5</param>
        internal static void CreateFile(string path, byte[] bytes, Action callback = null, string md5 = default)
        {
            bool exist = false;
            if (File.Exists(path))
            {
                exist = ComparisonMD5(path, bytes, md5);
                if (exist) return;
            }
            File.WriteAllBytes(path, bytes);
            callback?.Invoke();
        }
        #endregion

        #region 比对MD5
        /// <summary>
        /// 比对MD5，判断是否需要替换文件
        /// </summary>
        /// <param name="filePath">已存在的文件路径</param>
        /// <param name="newDate">需要比对的新下载数据</param>
        /// <param name="md5">可用来做对比的MD5</param>
        /// <returns>两个文件MD5是否相同</returns>
        internal static bool ComparisonMD5(string filePath, byte[] newDate = null, string md5 = null)
        {
            try
            {                
                FileStream fs = File.OpenRead(filePath);
                int length = (int)fs.Length;
                byte[] old = new byte[length];

                fs.Read(old, 0, length);
                fs.Close();

                MD5 mD5 = new MD5CryptoServiceProvider();
                byte[] oldMd5 = mD5.ComputeHash(old);
                string oldmD5 = "";
                foreach (var item in oldMd5)
                {
                    oldmD5 += Convert.ToString(item, 16);
                }
                if (!string.IsNullOrEmpty(md5)) return oldmD5 == md5;

                byte[] newMd5 = mD5.ComputeHash(newDate);

                return oldMd5 == newMd5;
            }
            catch (FileNotFoundException e)
            {
                Debug.LogError(e.Message);
                return false;
            }
        }
        #endregion
    }
}