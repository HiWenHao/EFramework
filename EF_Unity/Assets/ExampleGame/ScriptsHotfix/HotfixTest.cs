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

using EasyFramework;
using EasyFramework.UI.Tips;
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
            EF.Ui.Push(new UiA());


            EF.Ui.ShowTips("这是一个测试提示窗", new TipsViewExtraData()
            {
                ConfirmName = "确定",
                CancelName = "取消",
                ConfirmCallBack = delegate { D.Warning("ConfirmCallBack\t1"); },
                CancelCallBack = delegate { D.Warning("CancelCallBack\t2"); },
                CloseCallBack = delegate { D.Warning("CloseCallBack\t3"); },
            });
        }
    }
}