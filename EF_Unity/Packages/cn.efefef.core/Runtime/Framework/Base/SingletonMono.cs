/*
 * ================================================
 * Describe:        The class is monobehavior singleton base.
 * Author:          Xiaohei.Wang(Wenhao)
 * CreationTime:    2022-05-14:33:01
 * ModifyAuthor:    Alvin8412
 * ModifyTime:      2026-05-08 22:42:46
 * ScriptVersion:   1.0
 * ===============================================
 */

using System;
using System.Collections.Generic;
using EasyFramework.Managers;
using UnityEngine;

namespace EasyFramework
{
    public abstract class MonoSingleton<T> : MonoBehaviour where T : MonoSingleton<T>, ISingleton
    {
        public static T Instance => SelfLazy.Value;

        private static readonly HashSet<T> Registered = new();
        private static readonly Lazy<T> SelfLazy = new(CreateInstance);

        private static T CreateInstance()
        {
            T instance;
            var existing = FindObjectsOfType<T>();
            if (existing.Length > 0)
            {
                for (int i = 1; i < existing.Length; i++)
                    Destroy(existing[i].gameObject);
                instance = existing[0];
            }
            else
                instance = new GameObject().AddComponent<T>();

            instance.name = $"--------------- [ {typeof(T).Name} ] ---------------";
            RegisterAndInit(instance);
            return instance;
        }

        private static void RegisterAndInit(T instance)
        {
            bool ignore = Attribute.IsDefined(typeof(T), typeof(IgnoreAutoRegisterAttribute));
            if (ignore || !Registered.Add(instance)) return;

            instance.transform.SetParent(Attribute.IsDefined(typeof(T), typeof(ManagerAttribute))
                ? EFC.Managers
                : EFC.Singleton);

            EFC.Register(instance);
        }

        protected virtual void OnDestroy()
        {
            Registered.Remove((T)this);
            if (!Attribute.IsDefined(typeof(T), typeof(IgnoreAutoRegisterAttribute)))
                EFC.Unregister((ISingleton)this, false);
        }
    }
}