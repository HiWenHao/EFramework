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
        private Dictionary<string, ResourcePackage> _resourcePackageList;

        void ISingleton.Init()
        {
            _resourcePackageList = new Dictionary<string, ResourcePackage>();
        }

        void ISingleton.Quit()
        {
            foreach (var package in _resourcePackageList)
            {
                package.Value.UnloadAllAssetsAsync();
            }

            _resourcePackageList.Clear();
            _resourcePackageList = null;
        }

        #region Added the ResourcePackage - 增加资源包

        public void AddResourcePackage(ResourcePackage package)
        {
            _resourcePackageList.TryAdd(package.PackageName, package);
        }

        #endregion

        #region Check - 查询

        /// <summary>
        /// 查询资源是否在具体的包中
        /// </summary>
        /// <param name="location">资源的定位地址， 如果勾选[ Enable Addressable ] 则只需要提供名字即可.
        /// <para>否则需要提供项目下的具体路径 eg: Assets/ExampleGame/Sources/xxxxx.mp3</para></param>
        /// <param name="packageName">资源包提名称</param>
        /// <returns></returns>
        public bool ResourceInPackage(string location, string packageName)
        {
            if (_resourcePackageList.TryGetValue(packageName, out var package))
                return package.CheckLocationValid(location);

            D.Error($"The [ {packageName} ] package not yet obtained. Please start updater.");
            return false;
        }

        #endregion

        #region Load resources - 加载资源
        /// <summary>
        /// 同步加载资源
        /// </summary>
        /// <param name="location">资源的定位地址， 如果勾选[ Enable Addressable ] 则只需要提供名字即可.
        /// <para>否则需要提供项目下的具体路径 eg: Assets/ExampleGame/Sources/xxxxx.mp3</para></param>
        /// <param name="packageName">资源包提名称</param>
        /// <typeparam name="T">对象类型</typeparam>
        /// <returns>T类型对象</returns>
        public T LoadInYooSync<T>(string location, string packageName = "DefaultPackage") where T : Object
            => LoadInYooAsset<T>(location, packageName, false);

        /// <summary>
        /// 异步加载资源
        /// </summary>
        /// <param name="location">资源的定位地址， 如果勾选[ Enable Addressable ] 则只需要提供名字即可.
        /// <para>否则需要提供项目下的具体路径 eg: Assets/ExampleGame/Sources/xxxxx.mp3</para></param>
        /// <param name="packageName">资源包提名称</param>
        /// <typeparam name="T">对象类型</typeparam>
        /// <returns>T类型对象</returns>
        public T LoadInYooAsync<T>(string location, string packageName = "DefaultPackage") where T : Object
            => LoadInYooAsset<T>(location, packageName, true);

        private T LoadInYooAsset<T>(string location, string packageName, bool useAsync) where T : Object
        {
            T resources = null;
            if (!EF.Patch.IsUse)
            {
                D.Error($"Please use [ EF.Patch.StartUpdatePatch() ] initialize YooAsset.!");
                return resources;
            }

            if (_resourcePackageList.TryGetValue(packageName, out var package))
            {
                if (package.CheckLocationValid(location))
                    resources = useAsync ? package.LoadAssetAsync<T>(location).GetAssetObject<T>()
                        : package.LoadAssetSync<T>(location).GetAssetObject<T>();
                else
                    D.Warning($"Current resource [ {location} ] doesn't exist in [ {packageName} ]..");
            }
            else
                D.Error($"The [ {packageName} ] package not yet obtained. Please use function [EF.Patch.StartUpdatePatch()] to update.");

            return resources;
        }

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

        #endregion

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
            foreach (var package in _resourcePackageList)
                package.Value.UnloadUnusedAssetsAsync();
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
