/*
 * ================================================
 * Describe:        更新面板.
 * Author:          Alvin8412
 * CreationTime:    2026-05-25 17:46:02
 * ModifyAuthor:    Alvin8412
 * ModifyTime:      2026-05-25 17:46:02
 * ScriptVersion:   0.1 
 * ================================================
 */

using System.Linq;
using Cysharp.Threading.Tasks;
using EasyFramework;
using EasyFramework.Managers.Ui;
using EasyFramework.Systems.Assets;
using YooAsset;

namespace EFExample
{
    /// <summary>
    /// 更新面板
    /// </summary>
    public partial class PatchUpdater
    {
        private bool _waitDownloading;
        private ResourceDownloaderOperation _downloaderOperation; 
        void IUiView.Awake()
        {
        }

        void IUiView.Enable(params object[] args)
        {
            //资源热更     仅支持Unity2019.4+      加载资源逻辑需要自己实现、根据项目的不同，逻辑也不同   已加入Load类计划
            // Yoo现在需要有首包资源，并且会产生[ BuildinCatalog ]文件
            // 在[ HostPlayMode ]模式下加载某个资源时，
            // 会先从[ StreamingAssetsPath ] 下寻找，找不到再去沙河路径下寻找
            // 如何测试:
            // 1. 先打一个空包或者必要资源包体[ Copy Buildin File Option ]选为[ ClearAndCopyAll ]
            // 2. 之后进行增量打包[ Copy Buildin File Option ]选为[ None ]，把出来的资源放置到远端或本地服务器
            // 3. 走下方更新函数，回调中可以加载增量的资源文件，这样测试完成
            CheckUpdate((EPlayMode)args[0]).Forget();
        }

        void IUiView.Quit()
        {
            _downloaderOperation = null;
        }

        private async UniTask CheckUpdate(EPlayMode playMode)
        {
            if (await EF.Patch.CheckForUpdatePatches(playMode))
            {
                _downloaderOperation = await EF.Patch.CreateDownloader();

                await UiSystem.Instance.ShowTipsView(
                    $"一共发现了{_downloaderOperation.TotalDownloadCount}个资源， " +
                    $"总大小为{_downloaderOperation.TotalDownloadBytes / 1048576f:F1}mb，需要下载，是否更新.", 
                    new TipsViewExtraData()
                    {
                        ConfirmName = "下载",
                        CancelName = "取消",
                        ConfirmCallBack = StartUpdateCallback,
                        CancelCallBack = CancelUpdateCallback,
                    });
            }
        }

        private void StartUpdateCallback()
        {
            _downloaderOperation.DownloadFileBeginCallback = OnStartDownloadFileFunction;
            _downloaderOperation.DownloadUpdateCallback = OnDownloadProgressUpdateFunction;
            _downloaderOperation.DownloadFinishCallback = OnDownloadOverFunction;
            _downloaderOperation.DownloadErrorCallback = OnDownloadErrorFunction;
            OnStartDownloadFile().Forget();
        }

        private void CancelUpdateCallback()
        {
            // Logic
            // 去往下一个流程
            Close().Forget();
        }

        private async UniTask OnStartDownloadFile()
        {
            await EF.Patch.StartDownloader(_downloaderOperation);
            await EF.Assets.ConfirmAssetsManagerType(AssetsSystemType.YooAsset);
            
            LoadMetadataForAOTAssemblies();
        }
        
        private void OnDownloadErrorFunction(DownloadErrorData errorData)
        {
            D.Error($"Download the file failed. The file name is {errorData.FileName} ,  Error info is {errorData.ErrorInfo}");
        }
        private void OnDownloadProgressUpdateFunction(DownloadUpdateData downloadData)
        {
            Sld_UpdaterSlider.value = (float)downloadData.CurrentDownloadBytes / downloadData.TotalDownloadBytes;

            string currentSizeMb = (downloadData.CurrentDownloadBytes / 1048576f).ToString("f1");
            string totalSizeMb = (downloadData.TotalDownloadBytes / 1048576f).ToString("f1");
            Txt_UpdaterTips.text =$"{downloadData.CurrentDownloadCount}/{downloadData.TotalDownloadCount} {currentSizeMb}MB/{totalSizeMb}MB";
        }
        private void OnStartDownloadFileFunction(DownloadFileData fileData)
        {
            D.Log($"当前下载：{fileData.FileName}, 大小为：{fileData.FileSize}");
        }
        private void OnDownloadOverFunction(DownloaderFinishData finishData)
        {
            D.Log($"{finishData.PackageName}下载完成，结果为：{finishData.Succeed}");
        }
        
        #region HybirdCLR
        /// <summary>
        /// 为aot assembly加载原始metadata， 这个代码放aot或者热更新都行。
        /// 一旦加载后，如果AOT泛型函数对应native实现不存在，则自动替换为解释模式执行
        /// </summary>
        void LoadMetadataForAOTAssemblies()
        {
#if UNITY_EDITOR
            // Editor下无需加载，直接查找获得HotUpdate程序集
            System.Reflection.Assembly _hotUpdateAss = System.AppDomain.CurrentDomain.GetAssemblies().First(a => a.GetName().Name == "ExampleGameHotfix");
            RunHotfixCode(_hotUpdateAss);
#else
            TextAsset handle = EF.Assets.Load<TextAsset>("Assets/AssetsHotfix/Code/ExampleGameHotfix.dll.bytes");
            RunHotfixCode(System.Reflection.Assembly.Load(handle.bytes));
#endif
        }

        void RunHotfixCode(System.Reflection.Assembly assembly)
        {
            System.Type _type = assembly.GetType("EFExample.HotfixTest");//找不到类型，加命名空间试试
            System.Reflection.MethodInfo _info = _type.GetMethod("Init");
            _info.Invoke(null, null);
        }
        #endregion

        #region Button invoke event. Do not change here.不要更改这行 -- Auto

        #endregion button invoke event. Do not change here.不要更改这行 -- Auto
    }
}
