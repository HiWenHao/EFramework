using System.IO;
using UnityEngine;

namespace CustomUwrDownload
{
    /// <summary>
    /// 文件夹处理
    /// </summary>
    internal class UwrFolderHandling
    {
        #region 创建专属目录
        /// <summary>
        /// 创建该名字文件夹，和其子文件夹（下载缓存目录）
        /// </summary>
        internal static void CreatDownloadAssetsFolder(string porjectName)
        {
            string path = Application.persistentDataPath + $"/{porjectName}";
            if (!Directory.Exists(path)) Directory.CreateDirectory(path);

            if (!Directory.Exists(path + "/video")) Directory.CreateDirectory(path + "/video");

            if (!Directory.Exists(path + "/audio")) Directory.CreateDirectory(path + "/audio");
        }
        #endregion

        #region 清除下载资源
        /// <summary>
        /// 删除该名字文件夹下的所有子文件夹
        /// </summary>
        /// <param name="_porjectName">想要删除子文件夹的，文件夹名字</param>
        internal static void DeleteDownloadAssets(string _porjectName)
        {
            string path = Application.persistentDataPath + $"/{_porjectName}";
            for (int i = Directory.GetDirectories(path).Length - 1; i >= 0; i--)
            {
                Directory.Delete(Directory.GetDirectories(path)[i], true);
            }
        }
        #endregion
    }
}