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
using UnityEngine;

namespace EFExample
{
    /// <summary>
    /// Please modify the description。
    /// </summary>
    public class GameStart : MonoBehaviour
    {
        private void Start()
        {
            EasyFramework.D.Init();
#if UNITY_EDITOR
            EF.ClearConsole();
#endif
            #region Set the game run time info
            //Application.targetFrameRate = 60;
            Application.runInBackground = true;
            #endregion

            #region Show platform infomation.展示平台信息
            EasyFramework.D.Correct($"CPU: {SystemInfo.processorType}({SystemInfo.processorCount}cores核心数)   " +
                $"  RAM = {Mathf.RoundToInt(SystemInfo.systemMemorySize / 1024f)}G     " +
                $"  GPU: {SystemInfo.graphicsDeviceName}   " +
                $"  VRAM = {Mathf.RoundToInt(SystemInfo.graphicsMemorySize / 1024f)}G        " +
                Screen.currentResolution.ToString());
            #endregion

            EasyFramework.D.Log("======================Initialize======================");
            //在这里写初始化内容，音频播放、首页UI进入、数据初始化、各类管理器初始化都可以在此

            EF.Timer.SleepTimeout = SleepTimeout.NeverSleep;


            Camera.main.gameObject.AddComponent<GMTest.Test>();

            //FPS展示
            EasyFramework.Utils.FPSOnGUI.Instance.allowDrag = true;

            //读表工具初始化
            //EasyFramework.ExcelTool.ExcelDataManager.Init("JsonData");
            //AimGame.ExcelDataCacheManager.CacheAllData();
            //EasyFramework.D.Correct(ETB.EDC_Example.Get(1).name);

            //网络部分，还待完善
            if (false)
            {
                BestHTTP.WebSocket.WebSocket _ws = EF.Socket.CreateAndOpenWebSocket(
                    new System.Uri("wss://echo.websocket.events"),
                    onOpen: (ws) =>
                    {
                        EasyFramework.D.Log("Socket onOpen !!!");
                    },
                    onMessage: (ws, msg) =>
                    {
                        EasyFramework.D.Log("Socket onMessage !!!   msg = " + msg);
                    },
                    onBinary: (ws, bytes) =>
                    {
                        EasyFramework.D.Log("Socket onBinary !!!   bytes length = " + bytes.Length);
                    },
                    onError: (ws, error) =>
                    {
                        EasyFramework.D.Error("Socket onError !!!   error = " + error);
                    },
                    onClosed: (ws, code, msg) =>
                    {
                        EasyFramework.D.Log("Socket onClosed !!!");
                    },
                    onErrorDescription: null,
                    onIncompleteFrame: null
                );
                EF.Timer.AddOnce(1.0f, delegate
                {
                    _ws.Send("Hello gamer..");
                });
                EF.Timer.AddOnce(3.0f, delegate
                {
                    EF.Socket.DisposeDesignation(_ws);
                });
            }

            //资源热更     仅支持Unity2019.4+      加载资源逻辑需要自己实现、根据项目的不同，逻辑也不同   已加入Load类计划
            //EF.Patch.StartUpdatePatch(EasyFramework.Managers.EFPlayMode.HostPlayMode);

            //UI进入
            //EF.Ui.Push(new You Class());

            //音频播放
            //EF.Sources.PlayBGMByName("You bgm`s name", true);
        }

        #region RunDllCode
        /// <summary>
        /// 为aot assembly加载原始metadata， 这个代码放aot或者热更新都行。
        /// 一旦加载后，如果AOT泛型函数对应native实现不存在，则自动替换为解释模式执行
        /// </summary>
        //IEnumerator LoadMetadataForAOTAssemblies()
        //{
        //    YooAsset.RawFileOperationHandle _handle = m_Package.LoadRawFileAsync("ExampleGame.dll");
        //    _handle.Completed += delegate
        //    {
        //        byte[] dllBytes = _handle.GetRawFileData();
        //        System.Reflection.Assembly hotUpdateAss = System.Reflection.Assembly.Load(dllBytes);
        //        System.Type type = hotUpdateAss.GetType("EFExample.APPMain");//找不到类型，加命名空间试试
        //        System.Reflection.MethodInfo _info = type.GetMethod("Run");
        //        _info.Invoke(null, null);
        //    };
        //    yield return _handle;
        //}
        #endregion
    }
}
