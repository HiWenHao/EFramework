/* 
 * ================================================
 * Describe:      This script is used to loaded assets in unity.
 * Author:        Xiaohei.Wang(Wenhao)
 * CreationTime:  2022-06-08 14:43:47
 * ModifyAuthor:  Xiaohei.Wang(Wenhao)
 * ModifyTime:    2022-06-08 14:43:47
 * Version:       0.1 
 * ===============================================
 */
using UnityEngine;


public enum LoadType
{
    Sources,
    UIPerfabs,
}

namespace EasyFramework.Managers
{
    /// <summary>
    /// Loaded assets.
    /// </summary>
    public class LoadManager : Singleton<LoadManager>, IManager
    {
        int IManager.ManagerLevel => EF.Projects.AppConst.ManagerLevels.IndexOf("LoadManager");

        private string str_AssetsPath = "";
        private string str_StreamingPath = "";
        void ISingleton.Init()
        {
            str_AssetsPath = Application.dataPath;
            str_StreamingPath = Application.streamingAssetsPath;
        }

        void ISingleton.Quit()
        {
            str_AssetsPath = default;
            str_StreamingPath = default;
        }

        public T Load<T>(string patghName) where T : Object
        {
            return Resources.Load<T>(patghName);
        }

        public T Download<T>() where T : Object
        {
            return Resources.Load<T>("");
        }
    }
}
