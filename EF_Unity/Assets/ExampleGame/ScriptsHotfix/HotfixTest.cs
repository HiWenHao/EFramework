/*
 * ================================================
 * Describe:        This script is used to .
 * Author:          Xiaohei.Wang(Wenhao)
 * CreationTime:    2024-10-09 09:18:34
 * ModifyAuthor:    Alvin8412
 * ModifyTime:      2026-11-18 21:17:38
 * ScriptVersion:   0.1
 * ===============================================
 */

using Cysharp.Threading.Tasks;
using EasyFramework;
using EasyFramework.Managers.Assets;
using EasyFramework.Managers.Audio;
using EasyFramework.Managers.Ui;
using EFExample.UI.Tips;
using Luban;
using SimpleJSON;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace EFExample
{
    /// <summary>
    /// Please modify the description。
    /// </summary>
    public class HotfixTest : MonoBehaviour
    {
        /// <summary>
        /// 保证 EF 框架已初始化。
        /// <para>Classic 形态下 EFC 由 [RuntimeInitializeOnLoadMethod] 自动拉起；</para>
        /// <para>Full 形态下 EF.Core 位于热更层，该特性不会触发，必须在这里补一次（已初始化则跳过）。</para>
        /// </summary>
        private static void EnsureFrameworkInitialized()
        {
            if (EFC.Managers != null)
                return;

            var method = typeof(EFC).GetMethod("Initialize",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);

            if (method == null)
            {
                D.Error("[Hotfix] 找不到 EFC.Initialize，框架未初始化，后续流程会失败");
                return;
            }

            method.Invoke(null, null);
        }

        public static void Init()
        {
            EnsureFrameworkInitialized();

            D.Emphasize("Hello Game and HybridCLR");
            DontDestroyOnLoad(Camera.main);
            SceneManager.LoadScene("Empty");
            Run().Forget();
        }

        private static async UniTaskVoid Run()
        {
            await EF.Assets.ConfirmAssetsManagerType(AssetsSystemType.YooAsset);

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
