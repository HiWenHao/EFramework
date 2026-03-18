/* 
 * ================================================
 * Describe:      This script is used to .
 * Author:        Qian.cao
 * CreationTime:  2023-05-26 14:15:44
 * ModifyAuthor:  Qian.cao
 * ModifyTime:    2024-07-10 16:02:26
 * ScriptVersion: 0.3
 * ===============================================
*/

using EasyFramework.Managers.Utility;
using System;
using System.Collections.Generic;

namespace EasyFramework.Managers
{
    public class EventManager : Singleton<EventManager>, IManager
    {
        /// <summary>
        /// 操作类型
        /// </summary>
        enum OperationType
        {
            /// <summary> 增加 </summary>
            Add,
            /// <summary> 调用 </summary>
            Call,
            /// <summary> 删除 </summary>
            Remove,
            /// <summary> 释放 </summary>
            Release
        }

        Dictionary<string, IEventHelp> _eventCenter;
        void ISingleton.Init()
        {
            _eventCenter = new Dictionary<string, IEventHelp>();
        }

        void ISingleton.Quit()
        {
            _eventCenter.Clear();
            _eventCenter = null;
        }

        #region Private Function
        /// <summary>
        /// When event not found.
        /// <para>当事件没找到</para>
        /// </summary>
        /// <param name="eventName">事件名</param>
        /// <param name="operation">操作类型</param>
        void EventNotFound(string eventName, OperationType operation)
        {
            if (0 == EF.Projects.LanguageIndex)
                D.Log($"Event [ {eventName} ] not found. Unable to {operation}.");
            else
                D.Log($"未找到事件 [ {eventName} ] 。 无法 {GetChinese(operation)}.");
        }

        /// <summary>
        /// When params number error.
        /// <para>当参数数量错误</para>
        /// </summary>
        /// <param name="eventName">事件名</param>
        /// <param name="par1">本身支持的参数数量</param>
        /// <param name="par2">调用操作的参数数量</param>
        /// <param name="operation">操作类型</param>
        void ParamsNumberError(string eventName, int par1, int par2, OperationType operation)
        {
            if (0 == EF.Projects.LanguageIndex)
                D.Warning($"Event [ {eventName} ] takes {par1} arguments, but the function you're {operation} requires {par2} ! Try another name.");
            else
                D.Warning($"事件 [ {eventName} ] 支持 {par1} 个参数，而你 {GetChinese(operation)} 的函数需要 {par2} 个参数！换个名字试试。");
        }

        /// <summary>
        /// 获取中文
        /// </summary>
        /// <param name="operation">操作类型</param>
        string GetChinese(OperationType operation)
        {
            return operation switch
            {
                OperationType.Add => "增加",
                OperationType.Call => "调用",
                OperationType.Remove => "删除",
                OperationType.Release => "释放",
                _ => "",
            };
        }
        #endregion

        /// <summary>
        /// Added a event
        /// <para>增加一个事件</para>
        /// </summary>
        /// <param name="eventName">The event name.<para>事件名称</para></param>
        /// <param name="action">The event executing action.<para>事件执行函数</para></param>
        public void AddEvent(string eventName, Action action)
        {
            if (_eventCenter.TryGetValue(eventName, out var e))
            {
                if (e.ParamsNumber == 0)
                {
                    (e as EventHelp).AddAction(action);
                }
                else
                    ParamsNumberError(eventName, e.ParamsNumber, 0, OperationType.Add);
            }
            else
            {
                _eventCenter.Add(eventName, new EventHelp(action));
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
            if (_eventCenter.TryGetValue(eventName, out var e))
            {
                if (e.ParamsNumber == 1)
                {
                    (e as EventHelp<T1>).AddAction(action);
                }
                else
                    ParamsNumberError(eventName, e.ParamsNumber, 1, OperationType.Add);
            }
            else
            {
                _eventCenter.Add(eventName, new EventHelp<T1>(action));
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
            if (_eventCenter.TryGetValue(eventName, out var e))
            {
                if (e.ParamsNumber == 2)
                {
                    (e as EventHelp<T1, T2>).AddAction(action);
                }
                else
                    ParamsNumberError(eventName, e.ParamsNumber, 2, OperationType.Add);
            }
            else
            {
                _eventCenter.Add(eventName, new EventHelp<T1, T2>(action));
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
            if (_eventCenter.TryGetValue(eventName, out var e))
            {
                if (e.ParamsNumber == 3)
                {
                    (e as EventHelp<T1, T2, T3>).AddAction(action);
                }
                else
                    ParamsNumberError(eventName, e.ParamsNumber, 3, OperationType.Add);
            }
            else
            {
                _eventCenter.Add(eventName, new EventHelp<T1, T2, T3>(action));
            }
        }

        /// <summary>
        /// Call the event by name
        /// <para>通过名字调用事件</para>
        /// </summary>
        /// <param name="eventName">The event name.<para>事件名称</para></param>
        public void CallEvent(string eventName)
        {
            if (_eventCenter.TryGetValue(eventName, out var e))
            {
                if (e.ParamsNumber == 0)
                {
                    (e as EventHelp)?.Call();
                }
                else
                    ParamsNumberError(eventName, e.ParamsNumber, 0, OperationType.Call);
            }
            else
                EventNotFound(eventName, OperationType.Call);
        }

        /// <summary>
        /// Call the event by name
        /// <para>通过名字调用事件</para>
        /// </summary>
        /// <param name="eventName">The event name.<para>事件名称</para></param>
        public void CallEvent<T>(string eventName, T value)
        {
            if (_eventCenter.TryGetValue(eventName, out var e))
            {
                if (e.ParamsNumber == 1)
                    (e as EventHelp<T>)?.Call(value);
                else
                    ParamsNumberError(eventName, e.ParamsNumber, 1, OperationType.Call);
            }
            else
                EventNotFound(eventName, OperationType.Call);
        }

        /// <summary>
        /// Call the event by name
        /// <para>通过名字调用事件</para>
        /// </summary>
        /// <param name="eventName">The event name.<para>事件名称</para></param>
        public void CallEvent<T1, T2>(string eventName, T1 value1, T2 value2)
        {
            if (_eventCenter.TryGetValue(eventName, out var e))
            {
                if (e.ParamsNumber == 2)
                    (e as EventHelp<T1, T2>)?.Call(value1, value2);
                else
                    ParamsNumberError(eventName, e.ParamsNumber, 2, OperationType.Call);
            }
            else
                EventNotFound(eventName, OperationType.Call);
        }

        /// <summary>
        /// Call the event by name
        /// <para>通过名字调用事件</para>
        /// </summary>
        /// <param name="eventName">The event name.<para>事件名称</para></param>
        public void CallEvent<T1, T2, T3>(string eventName, T1 value1, T2 value2, T3 value3)
        {
            if (_eventCenter.TryGetValue(eventName, out var e))
            {
                if (e.ParamsNumber == 3)
                    (e as EventHelp<T1, T2, T3>)?.Call(value1, value2, value3);
                else
                    ParamsNumberError(eventName, e.ParamsNumber, 3, OperationType.Call);
            }
            else
                EventNotFound(eventName, OperationType.Call);
        }

        /// <summary>
        /// Remove the event function by name
        /// <para>通过名字调用事件</para>
        /// </summary>
        /// <param name="eventName">The event name.<para>事件名称</para></param>
        /// <param name="action">The event function.<para>事件函数</para></param>
        public void RemoveEvent(string eventName, Action action)
        {
            if (_eventCenter.TryGetValue(eventName, out var e))
            {
                if (e.ParamsNumber == 0)
                    (e as EventHelp).Remove(action);
                else
                    ParamsNumberError(eventName, e.ParamsNumber, 0, OperationType.Remove);
            }
            else
                EventNotFound(eventName, OperationType.Remove);
        }

        /// <summary>
        /// Remove the event function by name
        /// <para>通过名字调用事件</para>
        /// </summary>
        /// <param name="eventName">The event name.<para>事件名称</para></param>
        /// <param name="action">The event function.<para>事件函数</para></param>
        public void RemoveEvent<T1>(string eventName, Action<T1> action)
        {
            if (_eventCenter.TryGetValue(eventName, out var e))
            {
                if (e.ParamsNumber == 1)
                    (e as EventHelp<T1>).Remove(action);
                else
                    ParamsNumberError(eventName, e.ParamsNumber, 1, OperationType.Remove);
            }
            else
                EventNotFound(eventName, OperationType.Remove);
        }

        /// <summary>
        /// Remove the event function by name
        /// <para>通过名字调用事件</para>
        /// </summary>
        /// <param name="eventName">The event name.<para>事件名称</para></param>
        /// <param name="action">The event function.<para>事件函数</para></param>
        public void RemoveEvent<T1, T2>(string eventName, Action<T1, T2> action)
        {
            if (_eventCenter.TryGetValue(eventName, out var e))
            {
                if (e.ParamsNumber == 2)
                    (e as EventHelp<T1, T2>).Remove(action);
                else
                    ParamsNumberError(eventName, e.ParamsNumber, 2, OperationType.Remove);
            }
            else
                EventNotFound(eventName, OperationType.Remove);
        }

        /// <summary>
        /// Remove the event function by name
        /// <para>通过名字调用事件</para>
        /// </summary>
        /// <param name="eventName">The event name.<para>事件名称</para></param>
        /// <param name="action">The event function.<para>事件函数</para></param>
        public void RemoveEvent<T1, T2, T3>(string eventName, Action<T1, T2, T3> action)
        {
            if (_eventCenter.TryGetValue(eventName, out var e))
            {
                if (e.ParamsNumber == 3)
                    (e as EventHelp<T1, T2, T3>).Remove(action);
                else
                    ParamsNumberError(eventName, e.ParamsNumber, 3, OperationType.Remove);
            }
            else
                EventNotFound(eventName, OperationType.Remove);
        }

        /// <summary>
        /// Release the event by name.
        /// <para>根据名字释放事件</para>
        /// </summary>
        /// <param name="eventName">事件名</param>
        public void ReleaseEvent(string eventName)
        {
            if (_eventCenter.ContainsKey(eventName))
            {
                _eventCenter[eventName] = null;
                _eventCenter.Remove(eventName);
            }
            else
                EventNotFound(eventName, OperationType.Release);
        }
    }
}

