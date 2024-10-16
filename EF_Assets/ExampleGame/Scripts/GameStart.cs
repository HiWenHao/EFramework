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

using EasyFramework;
using Luban;
using SimpleJSON;
using System.Linq;
using UnityEngine;

namespace EFExample
{
    /// <summary>
    /// Please modify the description。
    /// </summary>
    public class GameStart : MonoBehaviour
    {
        public bool StartWebSocket;
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

            //FPS展示
            FPSOnGUI.Instance.AllowDrag = true;

            //当然，你也可以随时卸载你不需要的单例
            //EF.Timer.AddOnce(2.0f, delegate
            //{
            //    EF.Unregister(FPSOnGUI.Instance);
            //});

            //读表工具初始化
            //EasyFramework.ExcelTool.ExcelDataManager.Init("JsonData");
            //ExcelDataCacheManager.CacheAllData();
            //for (int i = 0; i < EDC_Example.Ids.Length; i++)
            //    EasyFramework.D.Emphasize(EDC_Example.Get(EDC_Example.Ids[i]).name);

            //网络部分，还待完善
            if (StartWebSocket)
            {
                BestHTTP.WebSocket.WebSocket _ws = EF.Socket.CreateAndOpenWebSocket(
                    new System.Uri("wss://echo.websocket.events"),
                    onOpen: (ws) =>
                    {
                        D.Log("Socket onOpen !!!");
                    },
                    onMessage: (ws, msg) =>
                    {
                        D.Log("Socket onMessage !!!   msg = " + msg);
                    },
                    onBinary: (ws, bytes) =>
                    {
                        D.Log("Socket onBinary !!!   bytes length = " + bytes.Length);
                    },
                    onError: (ws, error) =>
                    {
                        D.Error("Socket onError !!!   error = " + error);
                    },
                    onClosed: (ws, code, msg) =>
                    {
                        D.Log("Socket onClosed !!!");
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
            //EF.Patch.StartUpdatePatch(EasyFramework.Managers.EFPlayMode.HostPlayMode, callback: LoadMetadataForAOTAssemblies);


            //var tablesCtor = typeof(EasyFramework.LC).GetConstructors()[0];
            //var loaderReturnType = tablesCtor.GetParameters()[0].ParameterType.GetGenericArguments()[1];
            //// 根据cfg.Tables的构造函数的Loader的返回值类型决定使用json还是ByteBuf
            //System.Delegate loader = loaderReturnType == typeof(ByteBuf) ?
            //    new System.Func<string, ByteBuf>(LoadByteBuf)
            //    : new System.Func<string, JSONNode>(LoadJson);
            //EasyFramework.LC tables = (EasyFramework.LC)tablesCtor.Invoke(new object[] { loader });

            //foreach (var item in tables.TbItem.DataList)
            //{
            //    EasyFramework.D.Warning("reward:\t" + item.ToString());
            //}

            //UI进入
            EF.Ui.Push(new UiA());

            //音频播放
            //EF.Sources.PlayBGMByName("You bgm`s name", true);
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
            YooAsset.AssetHandle _handle = EF.Load.LoadInYooAsset("Assets/AssetsHotfix/Code/ExampleGameHotfix.dll.bytes");
            _handle.Completed += delegate
            {
                byte[] _dllBytes = (_handle.AssetObject as TextAsset).bytes;
                RunHotfixCode(System.Reflection.Assembly.Load(_dllBytes));
            };
#endif
        }

        void RunHotfixCode(System.Reflection.Assembly assembly)
        {
            System.Type _type = assembly.GetType("Example.HotfixTest");//找不到类型，加命名空间试试
            System.Reflection.MethodInfo _info = _type.GetMethod("RunTest");
            _info.Invoke(null, null);
        }
        #endregion

        #region Luban
        JSONNode LoadJson(string file)
        {
            return JSON.Parse(System.IO.File.ReadAllText($"{Application.dataPath}/Luban/Json/{file}.json", System.Text.Encoding.UTF8));
        }

        ByteBuf LoadByteBuf(string file)
        {
            return new ByteBuf(System.IO.File.ReadAllBytes($"{Application.dataPath}/Luban/Json/{file}.bytes"));
        }
        #endregion
    }
}
