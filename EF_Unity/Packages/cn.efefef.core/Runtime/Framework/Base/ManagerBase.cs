/*
 * ================================================
 * Describe:      This script is used to .
 * Author:        Alvin5100
 * CreationTime:  2026-05-15 18:37:27
 * ModifyAuthor:  Alvin5100
 * ModifyTime:    2026-05-15 18:37:27
 * ScriptVersion: 0.1
 * ===============================================
 */

using System;
using System.Collections.Generic;

namespace EasyFramework
{
    /// <summary>
    /// Please modify the description。
    /// </summary>
    public abstract class ManagerBase : IManager, ISystemOwner
    {
        /// <summary>
        /// 被注册的系统
        /// <para>The registered system.</para>
        /// </summary>
        protected Dictionary<Type, ISystem> _systems = new();

        public abstract void Init();

        public abstract void Quit();


        public void RegisterSystem(ISystem system)
        {
            Type type = system.GetType();
            if (_systems.ContainsKey(type))
            {
                D.Error("[ ISystemOwner ] System already registered..");
                return;
            }

            _systems.Add(type, system);
        }

        public void UnregisterSystem<T>() where T : ISystem
        {
        }

        public void UnregisterSystem(ISystem system)
        {
        }

        public bool GetSystem<T>(out T system) where T : ISystem
        {
            if (_systems.TryGetValue(typeof(T), out var sys))
            {
                system = (T)sys;
                return true;
            }

            system = default;
            return false;
        }

        public void ChangeSystem<T>(T system) where T : ISystem
        {
            Type type = system.GetType();
            if (!_systems.ContainsKey(type))
                return;

            _systems[type] = system;
        }
    }
}