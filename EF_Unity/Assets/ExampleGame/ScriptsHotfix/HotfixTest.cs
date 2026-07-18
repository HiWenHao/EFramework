/*
 * ================================================
 * Describe:      This script is used to .
 * Author:        Xiaohei.Wang(Wenhao)
 * CreationTime:  2024-10-09 09:18:34
 * ModifyAuthor:  Xiaohei.Wang(Wenhao)
 * ModifyTime:    2024-10-09 09:18:34
 * ScriptVersion: 0.1
 * ===============================================
 */

using System;
using Cysharp.Threading.Tasks;
using EasyFramework;
using EasyFramework.Managers.Audio;
using EasyFramework.Managers.Ui;
using EFExample.UI.Tips;
using Luban;
using SimpleJSON;
using UnityEngine;

namespace EFExample
{
    /// <summary>
    /// Please modify the description。
    /// </summary>
    public class HotfixTest : MonoBehaviour
    {
        public static void Init()
        {
            D.Emphasize("Hello Game and HybridCLR");


            //FPS展示
            FPSOnGUI.Instance.AllowDrag = true;

            //UI进入
            UiSystem.Instance.OpenView<UiAView>().Forget();

            // 旧 TipsView 调用示例（已废弃，仅供参考）
            UiSystem.Instance.OpenViewOverlay<TipsView, string, TipsViewExtraData>("这是一个测试提示窗", new TipsViewExtraData()
            {
                ConfirmName = "确定",
                CancelName = "取消",
                ConfirmCallBack = delegate { D.Warning("ConfirmCallBack\t1"); },
                CancelCallBack = delegate { D.Warning("CancelCallBack\t2"); },
                CloseCallBack = delegate { D.Warning("CloseCallBack\t3"); },
            }).Forget();

            Test().Forget();

            AudioManager.Instance.Play2DEffect("Haoheng").Forget();

            //var tablesCtor = typeof(cfg.LC).GetConstructors()[0];
            //var loaderReturnType = tablesCtor.GetParameters()[0].ParameterType.GetGenericArguments()[1];
            //// 根据cfg.Tables的构造函数的Loader的返回值类型决定使用json还是ByteBuf
            //System.Delegate loader = loaderReturnType == typeof(ByteBuf) ?
            //    new System.Func<string, ByteBuf>(LoadByteBuf)
            //    : new System.Func<string, JSONNode>(LoadJson);
            //var tables = (cfg.LC)tablesCtor.Invoke(new object[] { loader });
            //foreach (var item in tables.TbItem.DataList)
            //{
            //    D.Warning("reward:\t" + item);
            //}
            //EF.Get();
        }

        static async UniTask Test()
        {
            for (int i = 0; i < 20; i++)
            {
                await UniTask.WaitForSeconds(0.1f);
                await UiSystem.Instance.OpenViewOverlay<PopupView, string>($"\t{i}\tIndex");
            }
        }

        #region Luban

        static JSONNode LoadJson(string file)
        {
            return JSON.Parse(Resources.Load<TextAsset>($"JsonData/{file}").text);
        }

        static ByteBuf LoadByteBuf(string file)
        {
            return new ByteBuf(Resources.Load<TextAsset>($"JsonData/{file}").bytes);
            //return new ByteBuf(System.IO.File.ReadAllBytes($"{Application.dataPath}/ExampleGame/Resources/JsonData/{file}.bytes"));
        }

        #endregion
    }
}