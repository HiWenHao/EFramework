/*
 * ================================================
 * Describe:      This script is used to .
 * Author:        Xiaohei.Wang(Wenhao)
 * CreationTime:  2023-11-14 11:12:28
 * ModifyAuthor:  Xiaohei.Wang(Wenhao)
 * ModifyTime:    2023-11-14 11:12:28
 * ScriptVersion: 0.1
 * ===============================================
*/

namespace EasyFramework
{
    /// <summary>
    /// FPS 计算中心
    /// </summary>
    public sealed class FpsCounter
    {
        private int m_Frames;
        private float m_TimeLeft;
        private float m_CurrentFps;
        private float m_Accumulator;
        private float m_UpdateInterval;

        /// <summary>
        /// FPS 计算中心
        /// </summary>
        /// <param name="updateInterval">计算间隔</param>
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

        /// <summary>
        /// 计算间隔
        /// </summary>
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

        /// <summary>
        /// 当前FPS
        /// </summary>
        public float CurrentFps
        {
            get
            {
                return m_CurrentFps;
            }
        }

        public void Update(float realElapseSeconds)
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
