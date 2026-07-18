/*
 * ================================================
 * Describe:      This script is used to .
 * Author:        Xiaohei.Wang(Wenhao)
 * CreationTime:  2023-05-26 14:15:44
 * ModifyAuthor:  Xiaohei.Wang(Wenhao)
 * ModifyTime:    2023-05-26 14:15:44
 * ScriptVersion: 0.1
 * ===============================================
 */

using System.Linq;
using Cysharp.Threading.Tasks;
using EasyFramework;
using EasyFramework.Edit;
using EasyFramework.Managers.Assets;
using Luban;
using SimpleJSON;
using UnityEngine;
using UnityEngine.UI;
using YooAsset;

namespace EFExample
{
    /// <summary>
    /// Please modify the description。
    /// </summary>
    public class GameStart : MonoBehaviour
    {
        [HeaderPro("使用哪种更新模式进入游戏", "Which update mode to use to enter the game.")]
        public EPlayMode PlayMode;

        private Text _txtUpdaterTips;
        private Slider _sldUpdaterSlider;

        private void Start()
        {
            D.Init();
#if UNITY_EDITOR
            EF.ClearConsole();
#endif

            #region Set the game run time info

            //Application.targetFrameRate = 60;
            Application.runInBackground = true;

            #endregion

            D.Log("======================Initialize======================");
            //在这里写初始化内容，音频播放、首页UI进入、数据初始化、各类管理器初始化都可以在此

            EF.Timer.SleepTimeout = SleepTimeout.NeverSleep;

            CheckUpdate(PlayMode).Forget();
        }

        // 检查更新
        private async UniTask CheckUpdate(EPlayMode playMode)
        {
            if (!await EF.Patch.CheckForUpdatePatches(playMode))
            {
                D.Log("没有需要更新的资源, 直接启动");
                OnStartDownloadFile().Forget();
                return;
            }

            Transform uiViewRect = EF.Assets.Load<Transform>(EFC.Projects.AppConst.UIPrefabsPath + "PatchUpdater").transform;
            _sldUpdaterSlider = EF.Tool.Find<Slider>(uiViewRect.transform, "Sld_UpdaterSlider");
            _txtUpdaterTips = EF.Tool.Find<Text>(uiViewRect.transform, "Txt_UpdaterTips");
            await EF.Patch.StartUpdatePatches(OnStartDownloadFileFunction,
                OnDownloadProgressUpdateFunction, OnDownloadOverFunction, OnDownloadErrorFunction);
        }

        private async UniTask OnStartDownloadFile()
        {
            await EF.Assets.ConfirmAssetsManagerType(AssetsSystemType.YooAsset);
            LoadMetadataForAOTAssemblies();
        }

        #region YooAsset

        private void OnStartDownloadFileFunction(DownloadFileData fileData)
        {
            D.Log($"当前下载：{fileData.FileName}, 大小为：{fileData.FileSize}");
        }

        private void OnDownloadProgressUpdateFunction(DownloadUpdateData downloadData)
        {
            _sldUpdaterSlider.value = (float)downloadData.CurrentDownloadBytes / downloadData.TotalDownloadBytes;

            string currentSizeMb = (downloadData.CurrentDownloadBytes / 1048576f).ToString("f1");
            string totalSizeMb = (downloadData.TotalDownloadBytes / 1048576f).ToString("f1");
            _txtUpdaterTips.text =
                $"{downloadData.CurrentDownloadCount}/{downloadData.TotalDownloadCount} {currentSizeMb}MB/{totalSizeMb}MB";
        }

        private void OnDownloadOverFunction(DownloaderFinishData finishData)
        {
            D.Log($"{finishData.PackageName}下载完成，结果为：{finishData.Succeed}");
            OnStartDownloadFile().Forget();
        }

        private void OnDownloadErrorFunction(DownloadErrorData errorData)
        {
            D.Error(
                $"Download the file failed. The file name is {errorData.FileName} ,  Error info is {errorData.ErrorInfo}");
        }

        #endregion

        #region HybirdCLR

        /// <summary>
        /// 为AOT Assembly加载原始metadata， 这个代码放AOT或者热更新都行。
        /// <br/>一旦加载后，如果AOT泛型函数对应native实现不存在，则自动替换为解释模式执行
        /// <para>Load the original metadata for the AOT Assembly.<br/>
        /// This code can be placed in either the AOT or hot update mode.<br/>
        /// Once loaded, if the native implementation corresponding to the AOT generic function does not exist.<br/>
        /// It will automatically be replaced with interpreter mode execution.</para>
        /// </summary>
        void LoadMetadataForAOTAssemblies()
        {
            string assemblyName = "ExampleGameHotfix";
#if UNITY_EDITOR
            // Editor下无需加载，直接查找获得HotUpdate程序集
            System.Reflection.Assembly hotUpdateAss = System.AppDomain.CurrentDomain.GetAssemblies()
                .First(a => a.GetName().Name == assemblyName);
            RunHotfixCode(hotUpdateAss);
#else
            TextAsset handle = EF.Assets.Load<TextAsset>($"Assets/AssetsHotfix/Code/{assemblyName}.dll.bytes");
            RunHotfixCode(System.Reflection.Assembly.Load(handle.bytes));
#endif
        }

        // 运行热更代码 - Run the thermal correction code
        void RunHotfixCode(System.Reflection.Assembly assembly)
        {
            //具体类型名，找不到加命名空间试试
            //For the specific type name, if you can't find it, try adding the namespace.
            System.Type type = assembly.GetType("EFExample.HotfixTest");
            
            //具体要执行的方法名
            //Specific method name to execute
            System.Reflection.MethodInfo info = type.GetMethod("Init");
            info?.Invoke(null, null);
        }

        #endregion
    }
}