/* 
 * ================================================
 * Describe:      This script is used to .
 * Author:        Qian.cao
 * CreationTime:  2023-05-26 14:15:44
 * ModifyAuthor:  Qian.cao
 * ModifyTime:    2023-05-26 14:15:44
 * ScriptVersion: 0.1
 * ===============================================
*/

using EasyFramework.Managers.Utility;
using System;
using System.Collections.Generic;

namespace EasyFramework.Managers
{
    public class EventManager : Singleton<EventManager>, IManager
    {
        Dictionary<string, IEventHelp> m_EventCenter;
        void ISingleton.Init()
        {
            m_EventCenter = new Dictionary<string, IEventHelp>();
        }

        void ISingleton.Quit()
        {
            m_EventCenter.Clear();
            m_EventCenter = null;
        }

        /// <summary>
        /// Added a event
        /// <para>增加一个事件</para>
        /// </summary>
        /// <param name="eventName">The event name.<para>事件名称</para></param>
        /// <param name="action">The event executing action.<para>事件执行函数</para></param>
        public void AddEvent(string eventName, Action action)
        {
            if (m_EventCenter.TryGetValue(eventName, out var e))
            {
                (e as EventHelp).AddAction(action);
            }
            else
            {
                m_EventCenter.Add(eventName, new EventHelp(action));
            }
        }

        /// <summary>
        /// Added a event
        /// <para>增加一个事件</para>
        /// </summary>
        /// <param name="eventName">The event name.<para>事件名称</para></param>
        /// <param name="action">The event executing action.<para>事件执行函数</para></param>
        public void AddEvent<T1>(string eventName, Action<T1> action)
        {
            if (m_EventCenter.TryGetValue(eventName, out var e))
            {
                (e as EventHelp<T1>).AddAction(action);
            }
            else
            {
                m_EventCenter.Add(eventName, new EventHelp<T1>(action));
            }
        }

        /// <summary>
        /// Added a event
        /// <para>增加一个事件</para>
        /// </summary>
        /// <param name="eventName">The event name.<para>事件名称</para></param>
        /// <param name="action">The event executing action.<para>事件执行函数</para></param>
        public void AddEvent<T1, T2>(string eventName, Action<T1, T2> action)
        {
            if (m_EventCenter.TryGetValue(eventName, out var e))
            {
                (e as EventHelp<T1, T2>).AddAction(action);
            }
            else
            {
                m_EventCenter.Add(eventName, new EventHelp<T1, T2>(action));
            }
        }

        /// <summary>
        /// Added a event
        /// <para>增加一个事件</para>
        /// </summary>
        /// <param name="eventName">The event name.<para>事件名称</para></param>
        /// <param name="action">The event executing action.<para>事件执行函数</para></param>
        public void AddEvent<T1, T2, T3>(string eventName, Action<T1, T2, T3> action)
        {
            if (m_EventCenter.TryGetValue(eventName, out var e))
            {
                (e as EventHelp<T1, T2, T3>).AddAction(action);
            }
            else
            {
                m_EventCenter.Add(eventName, new EventHelp<T1, T2, T3>(action));
            }
        }

        /// <summary>
        /// Call the event by name
        /// <para>通过名字调用事件</para>
        /// </summary>
        /// <param name="eventName">The event name.<para>事件名称</para></param>
        public void CallEvent(string eventName)
        {
            if (m_EventCenter.TryGetValue(eventName, out var e))
            {
                (e as EventHelp)?.Call();
            }
            else
            {
                D.Log($"未找到{eventName}事件，无法执行！");
            }
        }

        /// <summary>
        /// Call the event by name
        /// <para>通过名字调用事件</para>
        /// </summary>
        /// <param name="eventName">The event name.<para>事件名称</para></param>
        public void CallEvent<T>(string eventName, T value)
        {
            if (m_EventCenter.TryGetValue(eventName, out var e))
            {
                (e as EventHelp<T>)?.Call(value);
            }
            else
            {
                D.Log($"未找到{eventName}事件，无法执行！");
            }
        }

        /// <summary>
        /// Call the event by name
        /// <para>通过名字调用事件</para>
        /// </summary>
        /// <param name="eventName">The event name.<para>事件名称</para></param>
        public void CallEvent<T1, T2>(string eventName, T1 value1, T2 value2)
        {
            if (m_EventCenter.TryGetValue(eventName, out var e))
            {
                (e as EventHelp<T1, T2>)?.Call(value1, value2);
            }
            else
            {
                D.Log($"未找到{eventName}事件，无法执行！");
            }
        }

        /// <summary>
        /// Call the event by name
        /// <para>通过名字调用事件</para>
        /// </summary>
        /// <param name="eventName">The event name.<para>事件名称</para></param>
        public void CallEvent<T1, T2, T3>(string eventName, T1 value1, T2 value2, T3 value3)
        {
            if (m_EventCenter.TryGetValue(eventName, out var e))
            {
                (e as EventHelp<T1, T2, T3>)?.Call(value1, value2, value3);
            }
            else
            {
                D.Log($"未找到{eventName}事件，无法执行！");
            }
        }

        /// <summary>
        /// Remove the event function by name
        /// <para>通过名字调用事件</para>
        /// </summary>
        /// <param name="eventName">The event name.<para>事件名称</para></param>
        /// <param name="action">The event function.<para>事件函数</para></param>
        public void RemoveEvent(string eventName, Action action)
        {
            if (m_EventCenter.TryGetValue(eventName, out var e))
            {
                (e as EventHelp).Remove(action);
            }
            else
            {
                D.Log($"未找到{eventName}事件，无法移除！");
            }
        }

        /// <summary>
        /// Remove the event function by name
        /// <para>通过名字调用事件</para>
        /// </summary>
        /// <param name="eventName">The event name.<para>事件名称</para></param>
        /// <param name="action">The event function.<para>事件函数</para></param>
        public void RemoveEvent<T1>(string eventName, Action<T1> action)
        {
            if (m_EventCenter.TryGetValue(eventName, out var e))
            {
                (e as EventHelp<T1>).Remove(action);
            }
            else
            {
                D.Log($"未找到{eventName}事件，无法移除！");
            }
        }

        /// <summary>
        /// Remove the event function by name
        /// <para>通过名字调用事件</para>
        /// </summary>
        /// <param name="eventName">The event name.<para>事件名称</para></param>
        /// <param name="action">The event function.<para>事件函数</para></param>
        public void RemoveEvent<T1, T2>(string eventName, Action<T1, T2> action)
        {
            if (m_EventCenter.TryGetValue(eventName, out var e))
            {
                (e as EventHelp<T1, T2>).Remove(action);
            }
            else
            {
                D.Log($"未找到{eventName}事件，无法移除！");
            }
        }

        /// <summary>
        /// Remove the event function by name
        /// <para>通过名字调用事件</para>
        /// </summary>
        /// <param name="eventName">The event name.<para>事件名称</para></param>
        /// <param name="action">The event function.<para>事件函数</para></param>
        public void RemoveEvent<T1, T2, T3>(string eventName, Action<T1, T2, T3> action)
        {
            if (m_EventCenter.TryGetValue(eventName, out var e))
            {
                (e as EventHelp<T1, T2, T3>).Remove(action);
            }
            else
            {
                D.Log($"未找到{eventName}事件，无法移除！");
            }
        }
    }
}

