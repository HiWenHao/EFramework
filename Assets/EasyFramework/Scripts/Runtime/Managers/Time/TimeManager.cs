/*
 * ================================================
 * Describe:        The class is Time managers controller.
 * Author:          Xiaohei.Wang(Wenhao)
 * CreationTime:    2022-06-07-14:20:19
 * ModifyAuthor:    Xiaohei.Wang(Wenhao)
 * ModifyTime:      2024-02-05-14:20:19
 * Version:         1.0
 * ===============================================
 */

using EasyFramework.Managers.Utility;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace EasyFramework.Managers
{
    public class TimeManager : Singleton<TimeManager>, IManager, IUpdate
    {
        /// <summary>
        /// Globally unique time.
        /// <para>全局唯一时间</para>
        /// </summary>
        public float TotalTime => m_GlobalTime;

        /// <summary>
        /// Get current time.
        /// <para>获取当前时间</para>
        /// </summary>
        public string CurrentTime => DateTime.Now.ToString("yyyy-MM-dd-HH:mm:ss");

        /// <summary>
        /// A power saving setting, allowing the screen to dim some time after the last active user interaction.
        /// <para>省电设置，允许屏幕在最后一次活跃用户交互后一段时间变暗</para>
        /// </summary>
        public int SleepTimeout
        {
            get { return m_SleepTimeout; }
            set {
                m_SleepTimeout = value;
                Screen.sleepTimeout = m_SleepTimeout;
            }
        }

        int m_managerLevel = -99;
        int IManager.ManagerLevel
        {
            get
            {
                if (m_managerLevel < -1)
                    m_managerLevel = EF.Projects.AppConst.ManagerLevels.IndexOf(Name);
                return m_managerLevel;
            }
        }

        private float m_GlobalTime;
        private int m_SleepTimeout;
        /// <summary>  Number of events to be processed. 待处理事件数量 </summary>
        private int m_HandleCount;
        /// <summary> Event self-increment index. 事件自增索引 </summary>
        private int m_KeyIndex;

        /// <summary> Event to be deleted. 待删除事件 </summary>
        private List<int> m_RemovedEvents;
        /// <summary> Event to be added. 待增加事件 </summary>
        private List<TimeEvent> m_AddedEvents;
        /// <summary> All events. 全部事件 </summary>
        private Dictionary<int, TimeEvent> m_Events;

        void ISingleton.Init()
        {
            m_GlobalTime = 0.0f;
            TimeEventInit();
        }

        void ISingleton.Quit()
        {
            TimeEventQuit();
        }

        void IUpdate.Update(float elapse, float realElapse)
        {
            m_GlobalTime += elapse;
            TimeEventUpdate(elapse);
        }

        #region Time event
        void TimeEventInit()
        {
            m_RemovedEvents = new List<int>();
            m_AddedEvents = new List<TimeEvent>();
            m_Events = new Dictionary<int, TimeEvent>();
        }
        int CreateTimeEvent(float firstDelayTime, float cycleTime, int cycleCount, Action callback)
        {
            m_KeyIndex++;
            m_AddedEvents.Add(new TimeEvent()
            {
                Id = m_KeyIndex,
                DelayTime = firstDelayTime,
                CycleCount = cycleCount,
                CycleTime = cycleTime,
                EndCallback = callback
            });
            return m_KeyIndex;
        }
        void TimeEventUpdate(float elapse)
        {
            if ((m_HandleCount = m_AddedEvents.Count) != 0)
            {
                for (int i = 0; i < m_HandleCount; i++)
                    m_Events.Add(m_AddedEvents[i].Id, m_AddedEvents[i]);
                m_AddedEvents.Clear();
            }

            foreach (var timer in m_Events.Values)
            {
                if (timer.IsCompleted)
                    continue;

                if ((timer.PassedTime += elapse) >= timer.DelayTime + timer.CycleTime)
                {
                    //执行
                    timer.EndCallback?.Invoke();
                    timer.PassedTime = 0.0f;
                    timer.DelayTime = 0.0f;
                    if (--timer.CycleCount == 0)
                    {
                        timer.IsCompleted = true;
                        m_RemovedEvents.Add(timer.Id);
                    }
                }
            }

            if ((m_HandleCount = m_RemovedEvents.Count) != 0)
            {
                for (int i = 0; i < m_HandleCount; i++)
                    m_Events.Remove(m_RemovedEvents[i]);
                m_RemovedEvents.Clear();
            }
        }
        void TimeEventQuit()
        {
            m_Events.Clear();
            m_AddedEvents.Clear();
            m_RemovedEvents.Clear();
            m_Events = null;
            m_AddedEvents = null;
            m_RemovedEvents = null;
        }
        #endregion

        #region Public function
        #region Time event
        /// <summary>
        /// Add an event that is executed only once. 
        /// <para>增加只执行一次的计时事件</para>
        /// </summary>
        /// <param name="delayTime">Delay time.<para>延时时间</para></param>
        /// <param name="callback">回调</param>
        /// <returns>The event id. <para>事件ID</para> </returns>
        public int AddOnce(float delayTime, Action callback)
        {
            return CreateTimeEvent(delayTime, 0, 1, callback);
        }
        /// <summary>
        /// Add a timing event. <para>增加一次计时事件</para>
        /// <para>If the event needs to be executed repeatedly, you can set the number of cycles to 0 or -1.</para>
        /// <para>如果事件需要一直重复执行，则可以设置循环次数为0或者-1</para>
        /// </summary>
        /// <param name="firstDelayTime">Execution of the first delay. <para>执行第一次前的延时</para></param>
        /// <param name="cycleTime">循环间隔</param>
        /// <param name="cycleCount">循环次数</param>
        /// <param name="callback">回调</param>
        /// <returns>The event id. <para>事件ID</para></returns>
        public int Add(float firstDelayTime, float cycleTime, int cycleCount, Action callback)
        {
            return CreateTimeEvent(firstDelayTime, cycleTime, cycleCount, callback);
        }

        /// <summary>
        /// Delete one of then events. 
        /// <para>删除其中一个</para>
        /// </summary>
        /// <param name="timeId">The event id. <para>事件ID</para></param>
        public void RemoveAt(int timeId)
        {
            m_RemovedEvents.Add(timeId);
        }

        /// <summary>
        /// Delete the all events.
        /// <para>删除全部事件</para>
        /// </summary>
        public void RemoveAll()
        {
            foreach (var e in m_Events)
            {
                e.Value.IsCompleted = true;
                m_RemovedEvents.Add(e.Key);
            }
        }
        #endregion

        #region Time scale
        /// <summary>
        /// Setting time scale.
        /// <para>设置时间速率</para>
        /// </summary>
        /// <param name="scale">Between 0.0f ~ 4.0f. <para>在0~4之间</para></param>
        public void SetTimeScale(float scale)
        {
            if (scale < 0.0f || scale > 4.0f)
            {
                D.Error("In order to ensure the smooth running of the game, please standardize the settings.");
                return;
            }
            Time.timeScale = scale;
        }
        #endregion
        #endregion
    }
}
