/*
 * ================================================
 * Describe:      This script is used to .
 * Author:        Alvin8412
 * CreationTime:  2026-05-18 18:17:14
 * ModifyAuthor:  Alvin8412
 * ModifyTime:    2026-05-18 18:17:14
 * ScriptVersion: 0.1
 * ===============================================
 */

using Luban;
using SimpleJSON;
using System.Linq;
using UnityEngine;

namespace EasyFramework.Launcher
{
    /// <summary>
    /// 游戏启动器 —— Build-in 壳层唯一入口
    /// <para>Game launcher — the sole entry point of the build-in shell.</para>
    /// <br/>EF.Launcher 程序集聚合所有 EF.* 包，热更 DLL 只引此程序集即可访问全部管理器。
    /// </summary>
    public class GameLauncher : MonoBehaviour
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
