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
    public class EventsHelp : IEventsHelp
    {
        public int ParamsNumber => 0;

        private event Action _action;
        public EventsHelp(Action action)
        {
            _action = action;
        }
        ~EventsHelp()
        {
            _action = null;
        }
        public void AddAction(Action action)
        {
            _action += action;
        }
        public void Call()
        {
            _action?.Invoke();
        }
        public void Remove(Action action)
        {
            _action -= action;
        }
    }
    public class EventsHelp<T1> : IEventsHelp
    {
        public int ParamsNumber => 1;

        private event Action<T1> _action;
        public EventsHelp(Action<T1> action)
        {
            _action = action;
        }
        ~EventsHelp()
        {
            _action = null;
        }
        public void AddAction(Action<T1> action)
        {
            _action += action;
        }
        public void Call(T1 value1)
        {
            _action?.Invoke(value1);
        }
        public void Remove(Action<T1> action)
        {
            _action -= action;
        }
    }
    public class EventsHelp<T1, T2> : IEventsHelp
    {
        public int ParamsNumber => 2;

        private event Action<T1, T2> _action;
        public EventsHelp(Action<T1, T2> action)
        {
            _action = action;
        }
        ~EventsHelp()
        {
            _action = null;
        }
        public void AddAction(Action<T1, T2> action)
        {
            _action += action;
        }
        public void Call(T1 value1, T2 value2)
        {
            _action?.Invoke(value1, value2);
        }
        public void Remove(Action<T1, T2> action)
        {
            _action -= action;
        }
    }
    public class EventsHelp<T1, T2, T3> : IEventsHelp
    {
        public int ParamsNumber => 3;

        private event Action<T1, T2, T3> _action;
        public EventsHelp(Action<T1, T2, T3> action)
        {
            _action = action;
        }
        ~EventsHelp()
        {
            _action = null;
        }
        public void AddAction(Action<T1, T2, T3> action)
        {
            _action += action;
        }
        public void Call(T1 value1, T2 value2, T3 value3)
        {
            _action?.Invoke(value1, value2, value3);
        }
        public void Remove(Action<T1, T2, T3> action)
        {
            _action -= action;
        }
    }
}
