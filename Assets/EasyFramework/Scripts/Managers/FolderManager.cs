/* 
 * ================================================
 * Describe:      This script is used to control folder.
 * Author:        Xiaohei.Wang(Wenhao)
 * CreationTime:  2022-06-17 11:01:10
 * ModifyAuthor:  Xiaohei.Wang(Wenhao)
 * ModifyTime:    2022-06-17 11:01:10
 * Version:       0.1 
 * ===============================================
 */

using System;
using System.IO;
using System.Security.Cryptography;
using UnityEngine;
using EasyFramework;

namespace EasyFramework.Managers
{
    /// <summary>
    /// Control all folder.
    /// </summary>
    public class FolderManager : Singleton<FolderManager>, IManager
    {
        int IManager.ManagerLevel => AppConst.ManagerLevel.FolderMgr;
        private string m_AssetsPath;
        void ISingleton.Init()
        {
            m_AssetsPath = Application.persistentDataPath + "/";
        }

        void ISingleton.Quit()
        {
            m_AssetsPath = default;
        }

        #region Target folder operation. 目标文件夹操作
        /// <summary>
        /// Create folder by name.创建该名字文件夹
        /// </summary>
        /// <param name="folderName">folder`s name. 文件夹名称.</param>
        public void CreatAssetsFolder(string folderName)
        {
            string path = m_AssetsPath + folderName;
            if (!Directory.Exists(path)) Directory.CreateDirectory(path);
        }

        /// <summary>
        /// Delete folder and children by name.删除该名字文件夹下的所有子文件夹
        /// </summary>
        /// <param name="folderName">folder`s name. 文件夹名称.</param>
        public void DeleteAssetsFolder(string folderName)
        {
            string path = m_AssetsPath + folderName;
            if (!Directory.Exists(path)) 
            {
                D.Error("This folder by name does not exist. 该名字的文件叫不存在.");
                return; 
            }
            for (int i = Directory.GetDirectories(path).Length - 1; i >= 0; i--)
            {
                Directory.Delete(Directory.GetDirectories(path)[i], true);
            }
        }

        /// <summary>
        /// Check the folder exist.检查文件夹是否存在.
        /// </summary>
        /// <param name="folderName">folder`s name. 文件夹名称</param>
        /// <returns></returns>
        public bool CheckFolderExist(string folderName)
        {
            return Directory.Exists(m_AssetsPath + folderName);
        }
        #endregion

        #region Files operations. 文件操作
        #region Create file. 创建文件
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

        #region Comparison MD5. MD5比对
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
        #endregion

    }
}
