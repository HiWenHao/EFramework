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

using Cysharp.Threading.Tasks;
using EasyFramework;
using EasyFramework.Managers.Ui;
using Luban;
using SimpleJSON;
using UnityEngine;
using YooAsset;

namespace EFExample
{
    /// <summary>
    /// Please modify the description。
    /// </summary>
    public class GameStart : MonoBehaviour
    {
        public EPlayMode PlayMode;
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

            UiSystem.Instance.OpenPageView<PatchUpdater>(PlayMode).Forget();
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

            //音频播放
            //EF.Sources.PlayBGMByName("You bgm`s name", true);


            //UiSystem.Instance.ShowTips("这是一个测试提示窗", new TipsViewExtraData()
            //{
            //    ConfirmName = "确定",
            //    CancelName = "取消",
            //    //ConfirmCallBack = delegate { D.Warning("ConfirmCallBack\t1"); },
            //    CancelCallBack = delegate { D.Warning("CancelCallBack\t2"); },
            //    //CloseCallBack = delegate { D.Warning("CloseCallBack\t3"); },
            //});


        }

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
