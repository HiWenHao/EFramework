/*
 * ================================================
 * Describe:      This script is used to .
 * Author:        Alvin5100(Wang)
 * CreationTime:  2025-12-10 09:55:52
 * ModifyAuthor:  Alvin5100(Wang)
 * ModifyTime:    2025-12-10 09:55:52
 * ScriptVersion: 0.1
 * ===============================================
 */

using UnityEngine;

namespace EasyFramework.Managers.Pool
{
    /// <summary>
    /// GameObject 对象池
    /// </summary>
    public class UnityGameObjectPool : UnityPoolBase<GameObject>
    {
        public override void Initialize(GameObject prefab, PoolConfig config)
        {
            if (prefab == null)
            {
                Debug.LogError($"GameObjectPool '{config.poolName}' has no prefab!");
                return;
            }

            if (IsInitialized)
            {
                if (Config.enableShowDebugInfo)
                    D.Warning($"Cannot change factory after pool [ {Config.poolName} ]  is initialized...");
                return;
            }
            
            // 设置工厂
            SetFactory(new GameObjectFactory(prefab, transform, config.poolName));

            base.Initialize(prefab, config);
        }

        /// <summary>
        /// 获取池化对象身上的<typeparamref name="T"/>类型组件
        /// </summary>
        /// <typeparam name="T">挂载类型</typeparam>
        /// <returns>类型组件</returns>
        public new T GetComponent<T>() where T : Component
        {
            T component = Get().GetComponent<T>();

            if (null == component && Config.enableShowDebugInfo)
                D.Warning($"Cannot get component of type [ {typeof(T).Name} ]  is null in pool [ {Config.poolName} ]...");
            
            return component;
        }
        
        /// <summary>
        /// 获取或增加一个<typeparamref name="T"/>类型组件到池化对象身上
        /// </summary>
        /// <typeparam name="T">挂载类型</typeparam>
        /// <returns>类型组件</returns>
        public T GetOrAddComponent<T>() where T : Component
        {
            GameObject go = Get();
            T component = go.GetComponent<T>();

            if (null == component)
                go.AddComponent<T>();
            
            return component;
        }
    }
}
