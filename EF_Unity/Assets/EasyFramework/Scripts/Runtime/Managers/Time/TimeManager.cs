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
        public float TotalTime => _globalTime;

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
            get { return _sleepTimeout; }
            set {
                _sleepTimeout = value;
                Screen.sleepTimeout = _sleepTimeout;
            }
        }

        private float _globalTime;
        private int _sleepTimeout;
        /// <summary>  Number of events to be processed. 待处理事件数量 </summary>
        private int _handleCount;
        /// <summary> Event self-increment index. 事件自增索引 </summary>
        private int _keyIndex;

        /// <summary> Event to be deleted. 待删除事件 </summary>
        private List<int> _removedEvents;
        /// <summary> Event to be added. 待增加事件 </summary>
        private List<TimeEvent> _addedEvents;
        /// <summary> All events. 全部事件 </summary>
        private Dictionary<int, TimeEvent> _events;

        void ISingleton.Init()
        {
            _globalTime = 0.0f;

            _removedEvents = new List<int>();
            _addedEvents = new List<TimeEvent>();
            _events = new Dictionary<int, TimeEvent>();
        }

        void ISingleton.Quit()
        {
            _events.Clear();
            _addedEvents.Clear();
            _removedEvents.Clear();
            _events = null;
            _addedEvents = null;
            _removedEvents = null;
        }

        void IUpdate.Update(float elapse, float realElapse)
        {
            _globalTime += elapse;

            if ((_handleCount = _addedEvents.Count) != 0)
            {
                for (int i = 0; i < _handleCount; i++)
                    _events.Add(_addedEvents[i].Id, _addedEvents[i]);
                _addedEvents.Clear();
            }

            foreach (var timer in _events.Values)
            {
                if (timer.IsCompleted)
                    continue;

                if ((timer.PassedTime += elapse) >= timer.DelayTime + timer.CycleTime)
                {
                    timer.PassedTime = 0.0f;
                    timer.DelayTime = 0.0f;
                    if (--timer.CycleCount == 0)
                    {
                        timer.IsCompleted = true;
                        _removedEvents.Add(timer.Id);
                    }
                    //执行
                    timer.EndCallback?.Invoke(timer.IsCompleted);
                }
            }

            if ((_handleCount = _removedEvents.Count) != 0)
            {
                for (int i = 0; i < _handleCount; i++)
                {
                    int _idx = _removedEvents[i];
                    if (_events.ContainsKey(_idx))
                        _events.Remove(_removedEvents[i]);
                }
                _removedEvents.Clear();
            }
        }

        /// <summary>
        /// Create a time event
        /// <para>创建一个时间事件</para>
        /// </summary>
        /// <param name="firstDelayTime">第一次处理延时</param>
        /// <param name="cycleTime">每次循环时间</param>
        /// <param name="cycleCount">循环次数</param>
        /// <param name="callback">回调</param>
        /// <returns>时间事件ID</returns>
        int CreateTimeEvent(float firstDelayTime, float cycleTime, int cycleCount, Action<bool> callback)
        {
            _keyIndex++;
            _addedEvents.Add(new TimeEvent()
            {
                Id = _keyIndex,
                DelayTime = firstDelayTime,
                CycleCount = cycleCount,
                CycleTime = cycleTime,
                EndCallback = callback
            });
            return _keyIndex;
        }

        #region Public function
        #region Time event
        /// <summary>
        /// Add an event that is executed only once. 
        /// <para>增加只执行一次的计时事件</para>
        /// </summary>
        /// <param name="delayTime">Delay time.<para>延时时间</para></param>
        /// <param name="callback">回调 参数: 是否结束</param>
        /// <returns>The event id. <para>事件ID</para> </returns>
        public int AddOnce(float delayTime, Action<bool> callback)
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
        /// <param name="callback">回调 参数: 是否结束</param>
        /// <returns>The event id. <para>事件ID</para></returns>
        public int Add(float firstDelayTime, float cycleTime, int cycleCount, Action<bool> callback)
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
            if (timeId < 0)
                return;

            _removedEvents.Add(timeId);
        }

        /// <summary>
        /// Delete the all events.
        /// <para>删除全部事件</para>
        /// </summary>
        public void RemoveAll()
        {
            foreach (var e in _events)
            {
                e.Value.IsCompleted = true;
                _removedEvents.Add(e.Key);
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
