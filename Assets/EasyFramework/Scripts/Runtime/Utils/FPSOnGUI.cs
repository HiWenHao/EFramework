/* 
 * ================================================
 * Describe:      This script is used to show the fps . 
 * Author:        Xiaohei.Wang(Wenhao)
 * CreationTime:  2022-09-16 17:41:05
 * ModifyAuthor:  Xiaohei.Wang(Wenhao)
 * ModifyTime:    2022-09-16 17:41:05
 * ScriptVersion: 0.1
 * ===============================================
*/

using UnityEngine;
using EasyFramework;

namespace EasyFramework.Utils
{
    /// <summary>
    /// Show the fps
    /// </summary>
    public class FPSOnGUI : MonoSingleton<FPSOnGUI>, ISingleton, IUpdate
    {
        //fps 显示的初始位置和大小
        public Rect startRect = new Rect(0f, 150f, 200f, 80f);
        //fps UI 是否允许拖动 
        public bool allowDrag = true;
        //GUI 的样式
        private GUIStyle style;

        FpsCounter m_FpsCounter;
        void ISingleton.Init()
        {
            m_FpsCounter = new FpsCounter(0.5f);
        }

        void IUpdate.Update(float elapse, float realElapse)
        {
            m_FpsCounter.Update(elapse, realElapse);
        }

        void OnGUI()
        {
            if (style == null)
            {
                style = new GUIStyle(GUI.skin.label);
                style.normal.textColor = Color.white;
                style.alignment = TextAnchor.MiddleCenter;
                style.fontSize = 40;
            }

            GUI.color = Color.white;
            startRect = GUI.Window(0, startRect, DoMyWindow, "");
        }

        void ISingleton.Quit()
        {

        }

        //Window窗口
        void DoMyWindow(int windowID)
        {
            GUI.Label(new Rect(0, 0, startRect.width, startRect.height), $"FPS {(int)m_FpsCounter.CurrentFps}", style);
            if (allowDrag) GUI.DragWindow(new Rect(10, 10, Screen.width - 10, Screen.height - 10));
        }
    }


    public sealed class FpsCounter
    {
        private float m_UpdateInterval;
        private float m_CurrentFps;
        private int m_Frames;
        private float m_Accumulator;
        private float m_TimeLeft;

        public FpsCounter(float updateInterval)
        {
            if (updateInterval <= 0f)
            {
                D.Error("Update interval is invalid.");
                return;
            }

            m_UpdateInterval = updateInterval;
            Reset();
        }

        public float UpdateInterval
        {
            get
            {
                return m_UpdateInterval;
            }
            set
            {
                if (value <= 0f)
                {
                    D.Error("Update interval is invalid.");
                    return;
                }

                m_UpdateInterval = value;
                Reset();
            }
        }

        public float CurrentFps
        {
            get
            {
                return m_CurrentFps;
            }
        }

        public void Update(float elapseSeconds, float realElapseSeconds)
        {
            m_Frames++;
            m_Accumulator += realElapseSeconds;
            m_TimeLeft -= realElapseSeconds;

            if (m_TimeLeft <= 0f)
            {
                m_CurrentFps = m_Accumulator > 0f ? m_Frames / m_Accumulator : 0f;
                m_Frames = 0;
                m_Accumulator = 0f;
                m_TimeLeft += m_UpdateInterval;
            }
        }

        private void Reset()
        {
            m_CurrentFps = 0f;
            m_Frames = 0;
            m_Accumulator = 0f;
            m_TimeLeft = 0f;
        }
    }
}
