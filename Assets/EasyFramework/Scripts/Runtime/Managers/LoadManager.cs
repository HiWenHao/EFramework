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
using System.Collections.Generic;
using UnityEngine;
using YooAsset;

namespace EasyFramework.Managers
{
    /// <summary>
    /// Loaded assets.
    /// </summary>
    public class LoadManager : Singleton<LoadManager>, IManager
    {
        int IManager.ManagerLevel => EF.Projects.AppConst.ManagerLevels.IndexOf("LoadManager");

        private string m_DefaultPackage;

        private Dictionary<string, ResourcePackage> m_ResourcePackageList;
        private Dictionary<string, List<AssetOperationHandle>> m_AssetOperationHandles;
        void ISingleton.Init()
        {
            m_ResourcePackageList = new Dictionary<string, ResourcePackage>();
            m_AssetOperationHandles = new Dictionary<string, List<AssetOperationHandle>>();
        }

        void ISingleton.Quit()
        {
            foreach (var handle in m_AssetOperationHandles)
            {
                for (int i = handle.Value.Count - 1; i >= 0; i--)
                {
                    handle.Value[i].Release();
                    handle.Value.RemoveAt(i);
                }
                handle.Value.Clear();
            }
            m_AssetOperationHandles.Clear();
            m_AssetOperationHandles = null;

            foreach (var package in m_ResourcePackageList)
            {
                package.Value.ForceUnloadAllAssets();
                package.Value.UnloadUnusedAssets();
            }
            m_ResourcePackageList.Clear();
            m_ResourcePackageList = null;

        }

        #region Added the ResourcePackage
        public void AddResourcePackage(ResourcePackage package)
        {
            m_ResourcePackageList.Add(package.PackageName, package);
        }
        #endregion

        /// <summary>
        /// Load the object in resources folder.
        /// 加载资源文件夹中的对象
        /// </summary>
        /// <param name="pathName">The object path in resources folder.对象在文件夹中的路径</param>
        /// <returns>Return the object typeof T. 返回T类型的对象</returns>
        public T LoadInResources<T>(string pathName) where T : Object
        {
            return Resources.Load<T>(pathName);
        }

        //public T LoadInYooAsset<T>()
        //{
        //
        //}


        public void LoadSceneAsyncInYooAsset(string pathName, string packageName = "DefaultPackage")
        {
            if (m_ResourcePackageList.TryGetValue(packageName, out ResourcePackage package))
            {
                package.LoadSceneAsync(pathName);
            }
            else
            {
                D.Error($"The [ {packageName} ] package not yet obtained. Please start updater.");
            }
        }
        public void LoadSceneAsyncInYooAsset(AssetInfo assetInfo, string packageName = "DefaultPackage")
        {
            if (m_ResourcePackageList.TryGetValue(packageName, out ResourcePackage package))
            {
                package.LoadSceneAsync(assetInfo);
            }
            else
            {
                D.Error($"The [ {packageName} ] package not yet obtained. Please start updater.");
            }
        }

        #region ClearMemory
        /// <summary>
        /// Clear the application all memory.
        /// 清理应用程序内存
        /// </summary>
        public void ClearAllMemory()
        {
            ClearYooAssetMemory();
            ClearResourcesMemory();
        }
        /// <summary>
        /// Clear the application memory for YooAsset.
        /// 清理应用程序中,YooAsset下引用计数为 0 的资源
        /// </summary>
        public void ClearYooAssetMemory()
        {
            foreach (var package in m_AssetOperationHandles)
                YooAssets.GetPackage(package.Key).UnloadUnusedAssets();
        }
        /// <summary>
        /// Clear the application memory for Resources.
        /// 清理应用程序中 Resources 资源
        /// </summary>
        public void ClearResourcesMemory()
        {
            Resources.UnloadUnusedAssets();
        }
        #endregion
    }
}
