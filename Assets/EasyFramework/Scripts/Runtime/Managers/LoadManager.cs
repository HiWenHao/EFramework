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
        int m_managerLevel = -99;
        int IManager.ManagerLevel
        {
            get
            {
                if (m_managerLevel < -1)
                    m_managerLevel = EF.Projects.AppConst.ManagerLevels.IndexOf(Name);
                return m_managerLevel;
            }
        }

        private Dictionary<string, ResourcePackage> m_ResourcePackageList;
        void ISingleton.Init()
        {
            m_ResourcePackageList = new Dictionary<string, ResourcePackage>();
        }

        void ISingleton.Quit()
        {

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
        /// <para>加载资源文件夹中的对象</para>
        /// </summary>
        /// <param name="pathName">The object path in resources folder.<para>对象在文件夹中的路径</para></param>
        /// <returns>Return the object typeof T. <para>返回T类型的对象</para></returns>
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
        /// <para>清理应用程序内存</para>
        /// </summary>
        public void ClearAllMemory()
        {
            ClearYooAssetMemory();
            ClearResourcesMemory();
        }
        /// <summary>
        /// Clear the application memory for YooAsset.
        /// <para>清理应用程序中,YooAsset下引用计数为 0 的资源</para>
        /// </summary>
        public void ClearYooAssetMemory()
        {
            foreach (var package in m_ResourcePackageList)
                package.Value.UnloadUnusedAssets();
        }
        /// <summary>
        /// Clear the application memory for Resources.
        /// <para>清理应用程序中 Resources 资源</para>
        /// </summary>
        public void ClearResourcesMemory()
        {
            Resources.UnloadUnusedAssets();
        }
        #endregion
    }
}
