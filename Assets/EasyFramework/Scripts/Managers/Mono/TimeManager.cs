/*
 * ================================================
 * Describe:        The class is Time managers controller.
 * Author:          Xiaohei.Wang(Wenhao)
 * CreationTime:    2022-06-07-14:20:19
 * ModifyAuthor:    Xiaohei.Wang(Wenhao)
 * ModifyTime:      2022-06-07-14:20:19
 * Version:         1.0
 * ===============================================
 */

using System;
using System.Collections.Generic;
using UnityEngine;
using XHTools;

namespace EasyFramework.Managers
{
    /// <summary>
    /// 语言
    /// </summary>
    public enum Language
    {
        Chinese,
        English,
        Japanese,

    }

    public class TimeManager : MonoSingleton<TimeManager>, ISingleton, IManager
    {
        /// <summary>
        /// Globally unique time.全局唯一时间
        /// </summary>
        public float TotalTime => m_flt_GlobalTime;

        /// <summary>
        /// Get current time.获取当前时间
        /// </summary>
        public string CurrentTime => DateTime.Now.ToString("yyyy-MM-dd-HH:mm:ss");

        /// <summary>
        /// A power saving setting, allowing the screen to dim some time after the last active user interaction.
        /// </summary>
        public int SleepTimeout
        {
            get { return m_int_SleepTimeout; }
            set {
                m_int_SleepTimeout = value;
                Screen.sleepTimeout = m_int_SleepTimeout;
            }
        }

        private float m_flt_GlobalTime;
        private int m_int_SleepTimeout;
        //The time and countdown list count.
        private int m_int_timerListCount, m_int_countdownListCount;
        //The time and countdown timer list.
        private List<float> m_lst_EventTime, m_lst_CountdownTime;
        //The time and countdown callback list.
        private List<EAction> m_lst_TimeEventList, m_lst_CountdownEventList;

        void ISingleton.Init()
        {
            m_flt_GlobalTime = 0.0f;
            m_int_timerListCount = 0;
            m_int_countdownListCount = 0;

            m_lst_EventTime = new List<float>();
            m_lst_CountdownTime = new List<float>();
            m_lst_TimeEventList = new List<EAction>();
            m_lst_CountdownEventList = new List<EAction>();
        }

        void ISingleton.Quit()
        {
            for (int i = m_int_timerListCount - 1; i >= 0; i--)
            {
                m_lst_EventTime.RemoveAt(i);
                m_lst_TimeEventList.RemoveAt(i);
            }
            for (int i = m_int_countdownListCount - 1; i >= 0; i--)
            {
                m_lst_CountdownTime.RemoveAt(i);
                m_lst_CountdownEventList.RemoveAt(i);
            }

            m_lst_EventTime.Clear();
            m_lst_TimeEventList.Clear();
            m_int_timerListCount = 0;
            m_lst_CountdownTime.Clear();
            m_lst_CountdownEventList.Clear();
            m_int_countdownListCount = 0;
        }

        void FixedUpdate()
        {
            m_flt_GlobalTime += Time.deltaTime;
            InTimeIncreases();
            InCountDown();
        }

        private void InTimeIncreases()
        {
            for (int i = 0; i < m_int_timerListCount; i++)
            {
                if (m_flt_GlobalTime < m_lst_EventTime[i])
                    continue;

                m_int_timerListCount--;
                m_lst_EventTime.RemoveAt(i);
                m_lst_TimeEventList[i]?.Invoke();
                m_lst_TimeEventList.RemoveAt(i);
                m_lst_EventTime.TrimExcess();
                m_lst_TimeEventList.TrimExcess();
            }
        }

        private void InCountDown()
        {
            for (int i = 0; i < m_int_countdownListCount; i++)
            {
                if ((m_lst_CountdownTime[i] -= Time.deltaTime) > 0.0f)
                    continue;

                m_int_countdownListCount--;
                m_lst_CountdownTime.RemoveAt(i);
                m_lst_CountdownEventList[i]?.Invoke();
                m_lst_CountdownEventList.RemoveAt(i);
                m_lst_CountdownTime.TrimExcess();
                m_lst_CountdownEventList.TrimExcess();
            }
        }

        #region Public function
        #region Time event
        /// <summary>
        /// Add a time event.增加一个时间事件
        /// </summary>
        /// <param name="triggeringTimer">Triggering time. 触发时间</param>
        /// <param name="timeEvent">The time event. 时间事件</param>
        /// <param name="alsoAdd">The event presence also add it. 当前时间事件存在也要继续添加</param>
        public void AddTimeEvent(float triggeringTimer, EAction timeEvent, bool alsoAdd = false)
        {
            if (!alsoAdd && m_lst_TimeEventList.IndexOf(timeEvent) != -1)
            {
                D.Warning("The time event is exist, if you want to hond on add it, please set param alsoAdd is true..");
                return;
            }
            if (triggeringTimer <= m_flt_GlobalTime)
            {
                D.Warning($"The time has passed, total time is {m_flt_GlobalTime}s ..");
                return;
            }
            m_int_timerListCount++;
            m_lst_EventTime.Add(triggeringTimer);
            m_lst_TimeEventList.Add(timeEvent);
        }

        /// <summary>
        /// Remove a time event. 删除一个时间事件
        /// </summary>
        /// <param name="timeEvent">The time event.  时间事件</param>
        /// <returns>Remove succeed return true. 删除成功会返回 true</returns>
        public bool RemoveTimeEvent(EAction timeEvent)
        {
            if (m_lst_TimeEventList.IndexOf(timeEvent) == -1)
            {
                D.Error("The time event does not exist!!!");
                return false;
            }
            for (int i = 0; i < m_int_timerListCount; i++)
            {
                if (!m_lst_TimeEventList[i].Equals(timeEvent))
                    continue;

                m_int_timerListCount--;
                m_lst_EventTime.RemoveAt(i);
                m_lst_TimeEventList.RemoveAt(i);
                m_lst_EventTime.TrimExcess();
                m_lst_TimeEventList.TrimExcess();
                return true;
            }

            return true;
        }

        /// <summary>
        /// Add a countdown event.增加一个倒计时事件
        /// </summary>
        /// <param name="countTimer">Triggering time. 触发倒计时</param>
        /// <param name="countdownEvent">The countdown event. 倒计时事件</param>
        /// <param name="alsoAdd">The event presence also add it. 当前倒计时事件存在也要继续添加</param>
        public void AddCountdownEvent(float countTimer, EAction countdownEvent, bool alsoAdd = false)
        {
            if (!alsoAdd && m_lst_CountdownEventList.IndexOf(countdownEvent) != -1)
            {
                D.Warning("The countdown event is exist, if you want to hond on add it, please set param alsoAdd is true..");
                return;
            }
            m_int_countdownListCount++;
            m_lst_CountdownTime.Add(countTimer);
            m_lst_CountdownEventList.Add(countdownEvent);
        }

        /// <summary>
        /// Remove a countdown event. 删除一个时间事件
        /// </summary>
        /// <param name="countdownEvent">The countdown event. 倒计时事件</param>
        /// <returns>Remove succeed return true. 删除成功会返回 true</returns>
        public bool RemoveCountdownEvent(EAction countdownEvent)
        {
            if (m_lst_CountdownEventList.IndexOf(countdownEvent) == -1)
            {
                D.Error("The time event does not exist!!!");
                return false;
            }
            for (int i = 0; i < m_int_countdownListCount; i++)
            {
                if (!m_lst_CountdownEventList[i].Equals(countdownEvent))
                    continue;

                m_int_countdownListCount--;
                m_lst_CountdownTime.RemoveAt(i);
                m_lst_CountdownEventList.RemoveAt(i);
                m_lst_CountdownTime.TrimExcess();
                m_lst_CountdownEventList.TrimExcess();
                return true;
            }

            return true;
        }
        #endregion

        #region Time scale
        /// <summary>
        /// Setting time scale.设置时间速率
        /// </summary>
        /// <param name="scale">Between 0.0f ~ 4.0f. 在0~4之间</param>
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

        #region Cuttent Day
        readonly string[] m_WeekdaysOfChinese = { "星期日", "星期一", "星期二", "星期三", "星期四", "星期五", "星期六" };
        readonly string[] m_WeekdaysOfJapanese = { "にちようび", "げつようび", "かようび", "すいようび", "もくようび", "きんようび", "どようび" };
        /// <summary>
        /// Current day of week.获取当前周几
        /// </summary>
        /// <param name="language">Show language.显示语言</param>
        public string CurrentDayOfWeek(Language language)
        {
            if (language == Language.English)
            {
                return DateTime.Now.DayOfWeek.ToString();
            }
            else if (language == Language.Japanese)
            {
                return m_WeekdaysOfJapanese[(int)DateTime.Now.DayOfWeek];
            }
            else
                return m_WeekdaysOfChinese[(int)DateTime.Now.DayOfWeek];
        }
        #endregion
        #endregion
    }
}
