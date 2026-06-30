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

using Cysharp.Threading.Tasks;
using EasyFramework;
using EasyFramework.Managers.Audio;
using EasyFramework.Managers.Ui;
using EFExample.UI.Tips;
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
            UiSystem.Instance.OpenPageView<UiAView>().Forget();

            // 旧 TipsView 调用示例（已废弃，仅供参考）
            UiSystem.Instance.OpenViewOverlay<TipsView>(new UiViewArgs<string, TipsViewExtraData>()
            {
                Args1 = "这是一个测试提示窗",
                Args2 = new TipsViewExtraData()
                {
                    ConfirmName = "确定",
                    CancelName = "取消",
                    ConfirmCallBack = delegate { D.Warning("ConfirmCallBack\t1"); },
                    CancelCallBack = delegate { D.Warning("CancelCallBack\t2"); },
                    CloseCallBack = delegate { D.Warning("CloseCallBack\t3"); },
                }
            }).Forget();

            Test().Forget();

            AudioManager.Instance.Play2DEffect("Haoheng").Forget();

            //EF.Get();
        }

        static async UniTask Test()
        {
            for (int i = 0; i < 20; i++)
            {
                await UniTask.WaitForSeconds(0.1f);
                await UiSystem.Instance.OpenViewOverlay<PopupView>(new UiViewArgs<string>($"\t{i}\tIndex"));
            }
        }
    }
}