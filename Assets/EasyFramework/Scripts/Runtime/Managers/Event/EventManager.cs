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

        public void AddEnvet(string actionName, Action action)
        {
            if (m_EventCenter.TryGetValue(actionName, out var e))
            {
                (e as EventHelp).AddAction(action);
            }
            else
            {
                m_EventCenter.Add(actionName, new EventHelp(action));
            }
        }
        public void AddEnvet<T1>(string actionName, Action<T1> action)
        {
            if (m_EventCenter.TryGetValue(actionName, out var e))
            {
                (e as EventHelp<T1>).AddAction(action);
            }
            else
            {
                m_EventCenter.Add(actionName, new EventHelp<T1>(action));
            }
        }
        public void AddEnvet<T1, T2>(string actionName, Action<T1, T2> action)
        {
            if (m_EventCenter.TryGetValue(actionName, out var e))
            {
                (e as EventHelp<T1, T2>).AddAction(action);
            }
            else
            {
                m_EventCenter.Add(actionName, new EventHelp<T1, T2>(action));
            }
        }
        public void AddEnvet<T1, T2, T3>(string actionName, Action<T1, T2, T3> action)
        {
            if (m_EventCenter.TryGetValue(actionName, out var e))
            {
                (e as EventHelp<T1, T2, T3>).AddAction(action);
            }
            else
            {
                m_EventCenter.Add(actionName, new EventHelp<T1, T2, T3>(action));
            }
        }

        public void CallEvent(string actionName)
        {
            if (m_EventCenter.TryGetValue(actionName, out var e))
            {
                (e as EventHelp)?.Call();
            }
            else
            {
                D.Log($"未找到{actionName}事件，无法执行！");
            }
        }
        public void CallEvent<T>(string actionName, T value)
        {
            if (m_EventCenter.TryGetValue(actionName, out var e))
            {
                (e as EventHelp<T>)?.Call(value);
            }
            else
            {
                D.Log($"未找到{actionName}事件，无法执行！");
            }
        }
        public void CallEvent<T1, T2>(string actionName, T1 value1, T2 value2)
        {
            if (m_EventCenter.TryGetValue(actionName, out var e))
            {
                (e as EventHelp<T1, T2>)?.Call(value1, value2);
            }
            else
            {
                D.Log($"未找到{actionName}事件，无法执行！");
            }
        }
        public void CallEvent<T1, T2, T3>(string actionName, T1 value1, T2 value2, T3 value3)
        {
            if (m_EventCenter.TryGetValue(actionName, out var e))
            {
                (e as EventHelp<T1, T2, T3>)?.Call(value1, value2, value3);
            }
            else
            {
                D.Log($"未找到{actionName}事件，无法执行！");
            }
        }

        public void RemoveEvent(string actionName, Action action)
        {
            if (m_EventCenter.TryGetValue(actionName, out var e))
            {
                (e as EventHelp).Remove(action);
            }
            else
            {
                D.Log($"未找到{actionName}事件，无法移除！");
            }
        }
        public void RemoveEvent<T1>(string actionName, Action<T1> action)
        {
            if (m_EventCenter.TryGetValue(actionName, out var e))
            {
                (e as EventHelp<T1>).Remove(action);
            }
            else
            {
                D.Log($"未找到{actionName}事件，无法移除！");
            }
        }
        public void RemoveEvent<T1, T2>(string actionName, Action<T1, T2> action)
        {
            if (m_EventCenter.TryGetValue(actionName, out var e))
            {
                (e as EventHelp<T1, T2>).Remove(action);
            }
            else
            {
                D.Log($"未找到{actionName}事件，无法移除！");
            }
        }
        public void RemoveEvent<T1, T2, T3>(string actionName, Action<T1, T2, T3> action)
        {
            if (m_EventCenter.TryGetValue(actionName, out var e))
            {
                (e as EventHelp<T1, T2, T3>).Remove(action);
            }
            else
            {
                D.Log($"未找到{actionName}事件，无法移除！");
            }
        }
    }
}

