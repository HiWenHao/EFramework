using UnityEngine;

namespace CustomUwrDownload
{
    /// <summary>
    /// 自定义下载管理配置类
    /// </summary>
    internal class UwrConfig
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void CreatManager()
        {
            GameObject obj;
            obj = GameObject.Find("DownloadUtils");
            if (obj == null)
                obj = new GameObject("DownloadUtils");
            if (!obj.GetComponent<DownloadUtils>())
                obj.AddComponent<DownloadUtils>();
            GameObject.DontDestroyOnLoad(obj);
            UwrFolderHandling.CreatDownloadAssetsFolder(DownloadTools.PorjectName ?? "unityAssets");
        }
    }
}