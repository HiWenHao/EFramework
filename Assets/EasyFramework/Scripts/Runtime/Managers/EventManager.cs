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

using System;
using System.Collections.Generic;

namespace EasyFramework.Managers
{
    public class EventManager : Singleton<EventManager>, IManager
    {
        private interface IEventHelp { }
        private class EventHelp : IEventHelp
        {
            private event Action _mAction;
            public EventHelp(Action action)
            {
                _mAction = action;
            }
            public void AddAction(Action action)
            {
                _mAction += action;
            }
            public void Call()
            {
                _mAction?.Invoke();
            }
            public void Remove(Action action)
            {
                _mAction -= action;
            }
        }
        private class EventHelp<T1> : IEventHelp
        {
            private event Action<T1> _mAction;
            public EventHelp(Action<T1> action)
            {
                _mAction = action;
            }
            public void AddAction(Action<T1> action)
            {
                _mAction += action;
            }
            public void Call(T1 value1)
            {
                _mAction?.Invoke(value1);
            }
            public void Remove(Action<T1> action)
            {
                _mAction -= action;
            }
        }
        private class EventHelp<T1, T2> : IEventHelp
        {
            private event Action<T1, T2> _mAction;
            public EventHelp(Action<T1, T2> action)
            {
                _mAction = action;
            }
            public void AddAction(Action<T1, T2> action)
            {
                _mAction += action;
            }
            public void Call(T1 value1, T2 value2)
            {
                _mAction?.Invoke(value1, value2);
            }
            public void Remove(Action<T1, T2> action)
            {
                _mAction -= action;
            }
        }

        private class EventHelp<T1, T2, T3> : IEventHelp
        {
            private event Action<T1, T2, T3> _mAction;
            public EventHelp(Action<T1, T2, T3> action)
            {
                _mAction = action;
            }
            public void AddAction(Action<T1, T2, T3> action)
            {
                _mAction += action;
            }
            public void Call(T1 value1, T2 value2, T3 value3)
            {
                _mAction?.Invoke(value1, value2, value3);
            }
            public void Remove(Action<T1, T2, T3> action)
            {
                _mAction -= action;
            }
        }
        Dictionary<string, IEventHelp> _eventCenter = new Dictionary<string, IEventHelp>();

        int IManager.ManagerLevel => 99999;

        public void AddEnvet(string actionName, Action action)
        {
            if (_eventCenter.TryGetValue(actionName, out var e))
            {
                (e as EventHelp).AddAction(action);
            }
            else
            {
                _eventCenter.Add(actionName, new EventHelp(action));
            }
        }
        public void AddEnvet<T1>(string actionName, Action<T1> action)
        {
            if (_eventCenter.TryGetValue(actionName, out var e))
            {
                (e as EventHelp<T1>).AddAction(action);
            }
            else
            {
                _eventCenter.Add(actionName, new EventHelp<T1>(action));
            }
        }
        public void AddEnvet<T1, T2>(string actionName, Action<T1, T2> action)
        {
            if (_eventCenter.TryGetValue(actionName, out var e))
            {
                (e as EventHelp<T1, T2>).AddAction(action);
            }
            else
            {
                _eventCenter.Add(actionName, new EventHelp<T1, T2>(action));
            }
        }
        public void AddEnvet<T1, T2, T3>(string actionName, Action<T1, T2, T3> action)
        {
            if (_eventCenter.TryGetValue(actionName, out var e))
            {
                (e as EventHelp<T1, T2, T3>).AddAction(action);
            }
            else
            {
                _eventCenter.Add(actionName, new EventHelp<T1, T2, T3>(action));
            }
        }

        public void CallEvent(string actionName)
        {
            if (_eventCenter.TryGetValue(actionName, out var e))
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
            if (_eventCenter.TryGetValue(actionName, out var e))
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
            if (_eventCenter.TryGetValue(actionName, out var e))
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
            if (_eventCenter.TryGetValue(actionName, out var e))
            {
                (e as EventHelp<T1, T2, T3>)?.Call(value1, value2, value3);
            }
            else
            {
                D.Log($"未找到{actionName}事件，无法执行！");
            }
        }

        //remove wait....x
        public void RemoveEvent(string actionName, Action action)
        {
            if (_eventCenter.TryGetValue(actionName, out var e))
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
            if (_eventCenter.TryGetValue(actionName, out var e))
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
            if (_eventCenter.TryGetValue(actionName, out var e))
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
            if (_eventCenter.TryGetValue(actionName, out var e))
            {
                (e as EventHelp<T1, T2, T3>).Remove(action);
            }
            else
            {
                D.Log($"未找到{actionName}事件，无法移除！");
            }
        }

        void ISingleton.Init()
        {

        }

        void ISingleton.Quit()
        {

        }
    }
}

