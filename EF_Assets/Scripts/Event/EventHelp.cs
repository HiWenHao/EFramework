/*
 * ================================================
 * Describe:      This script is used to .
 * Author:        Xiaohei.Wang(Wenhao)
 * CreationTime:  2024-04-28 15:18:39
 * ModifyAuthor:  Xiaohei.Wang(Wenhao)
 * ModifyTime:    2024-04-28 15:18:39
 * ScriptVersion: 0.1
 * ===============================================
*/
using System;

namespace EasyFramework.Managers.Utility
{
    public class EventHelp : IEventHelp
    {
        public int ParamsNumber => 0;

        private event Action m_Action;
        public EventHelp(Action action)
        {
            m_Action = action;
        }
        ~EventHelp()
        {
            m_Action = null;
        }
        public void AddAction(Action action)
        {
            m_Action += action;
        }
        public void Call()
        {
            m_Action?.Invoke();
        }
        public void Remove(Action action)
        {
            m_Action -= action;
        }
    }
    public class EventHelp<T1> : IEventHelp
    {
        public int ParamsNumber => 1;

        private event Action<T1> m_Action;
        public EventHelp(Action<T1> action)
        {
            m_Action = action;
        }
        ~EventHelp()
        {
            m_Action = null;
        }
        public void AddAction(Action<T1> action)
        {
            m_Action += action;
        }
        public void Call(T1 value1)
        {
            m_Action?.Invoke(value1);
        }
        public void Remove(Action<T1> action)
        {
            m_Action -= action;
        }
    }
    public class EventHelp<T1, T2> : IEventHelp
    {
        public int ParamsNumber => 2;

        private event Action<T1, T2> m_Action;
        public EventHelp(Action<T1, T2> action)
        {
            m_Action = action;
        }
        ~EventHelp()
        {
            m_Action = null;
        }
        public void AddAction(Action<T1, T2> action)
        {
            m_Action += action;
        }
        public void Call(T1 value1, T2 value2)
        {
            m_Action?.Invoke(value1, value2);
        }
        public void Remove(Action<T1, T2> action)
        {
            m_Action -= action;
        }
    }
    public class EventHelp<T1, T2, T3> : IEventHelp
    {
        public int ParamsNumber => 3;

        private event Action<T1, T2, T3> m_Action;
        public EventHelp(Action<T1, T2, T3> action)
        {
            m_Action = action;
        }
        ~EventHelp()
        {
            m_Action = null;
        }
        public void AddAction(Action<T1, T2, T3> action)
        {
            m_Action += action;
        }
        public void Call(T1 value1, T2 value2, T3 value3)
        {
            m_Action?.Invoke(value1, value2, value3);
        }
        public void Remove(Action<T1, T2, T3> action)
        {
            m_Action -= action;
        }
    }
}
