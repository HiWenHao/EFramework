/* 
 * ================================================
 * Describe:      This script is used to show the fps . 
 * Author:        Xiaohei.Wang(Wenhao)
 * CreationTime:  2022-09-16 17:41:05
 * ModifyAuthor:  Xiaohei.Wang(Wenhao)
 * ModifyTime:    2024-07-05 17:17:30
 * ScriptVersion: 0.1
 * ===============================================
*/

using EasyFramework;
using UnityEngine;

namespace EFExample
{
    /// <summary>
    /// Show the fps
    /// </summary>
    public class FPSOnGUI : MonoSingleton<FPSOnGUI>, ISingleton, IUpdate
    {
        //fps 显示的初始位置和大小
        Rect m_startRect;
        //fps UI 是否允许拖动 
        public bool AllowDrag;
        //GUI 的样式
        private GUIStyle style;

        FpsCounter m_FpsCounter;
        void ISingleton.Init()
        {
            m_startRect = new Rect(0f, 150f, 200f, 80f);
            m_FpsCounter = new FpsCounter(0.5f);
        }

        void IUpdate.Update(float elapse, float realElapse)
        {
            m_FpsCounter.Update(realElapse);
        }

        void OnGUI()
        {
            if (null == m_FpsCounter) return;
            GUI.color = Color.white;
            m_startRect = GUI.Window(0, m_startRect, DoMyWindow, "");
        }

        void ISingleton.Quit()
        {
            style = null;
            m_FpsCounter = null;
        }

        //Window窗口
        void DoMyWindow(int windowID)
        {
            if (style == null)
            {
                style = new GUIStyle(GUI.skin.label);
                style.normal.textColor = Color.white;
                style.alignment = TextAnchor.MiddleCenter;
                style.fontSize = 40;
            }
            GUI.Label(new Rect(0, 0, m_startRect.width, m_startRect.height), $"FPS {(int)m_FpsCounter.CurrentFps}", style);
            if (AllowDrag) GUI.DragWindow(new Rect(10, 10, Screen.width - 10, Screen.height - 10));
        }
    }
}
