/*
 * ================================================
 * Describe:      EF.YooAsset 包启动注册 —— 向 AssetsSystem 注册 YooAssetsSystem 工厂
 * Author:        Alvin8412
 * CreationTime:  2026-05-29 16:20:00
 * ModifyAuthor:  Alvin8412
 * ModifyTime:    2026-05-29 16:20:00
 * ScriptVersion: 0.1
 * ================================================
 */

using UnityEngine;

namespace EasyFramework.Managers.Assets
{
    /// <summary>
    /// EF.YooAsset 包初始化 —— 自动注册 YooAssetsSystem
    /// <para>EF.YooAsset package initializer — auto-registers YooAssetsSystem factory</para>
    /// </summary>
    /// <remarks>
    /// 通过 RuntimeInitializeOnLoadMethod 在游戏启动最早阶段执行，
    /// 确保 AssetsSystem.ConfirmAssetsManagerType(YooAsset) 可用。
    /// </remarks>
    public static class YooAssetPackageInitializer
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Register()
        {
            AssetsManager.RegisterAssetSystem(AssetsSystemType.YooAsset, () => new YooAssetsSystem());
        }
    }
}
