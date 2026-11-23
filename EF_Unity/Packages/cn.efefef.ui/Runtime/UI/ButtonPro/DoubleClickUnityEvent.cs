/*
 * ================================================
 * Describe:      UnityEvent with runtime listener count for double-click detection.
 * Author:        Alvin8412
 * CreationTime:  2026-11-23 15:12:00
 * ModifyAuthor:  Alvin8412
 * ModifyTime:    2026-11-23 15:12:00
 * ScriptVersion: 0.1
 * ===============================================
 */

using System;
using System.Collections.Generic;
using UnityEngine.Events;

namespace EasyFramework.Ui
{
    /// <summary>
    /// 可统计运行时监听者的 UnityEvent，用于双击判定。
    /// 请始终通过本类型的字段调用增删方法，禁止向上转型为 UnityEvent 后增删，否则计数会失同步。
    /// <para>UnityEvent that counts runtime listeners for double-click detection. Always call add/remove through
    /// a field of this type; upcasting to UnityEvent desynchronizes the count.</para>
    /// </summary>
    [Serializable]
    public class DoubleClickUnityEvent : UnityEvent
    {
        private readonly List<UnityAction> _runtimeListeners = new List<UnityAction>(); //  运行时监听者，与 UnityEvent 一样允许重复

        /// <summary>
        /// 是否存在运行时或 Inspector 监听者。
        /// <para>Whether any runtime or Inspector listener is registered.</para>
        /// </summary>
        public bool HasListener => _runtimeListeners.Count > 0 || GetPersistentEventCount() > 0;

        /// <summary>
        /// 添加监听者（隐藏基类实现以维护计数，调用方式与普通 UnityEvent 一致）。
        /// <para>Adds a listener (hides the base method to keep the count; usage matches a plain UnityEvent).</para>
        /// </summary>
        /// <param name="call">回调 <para>Callback</para></param>
        public new void AddListener(UnityAction call)
        {
            if (call == null)
                return;
            _runtimeListeners.Add(call);
            base.AddListener(call);
        }

        /// <summary>
        /// 移除监听者（隐藏基类实现以维护计数，仅移除首个匹配项）。
        /// <para>Removes a listener (hides the base method to keep the count; removes the first match only).</para>
        /// </summary>
        /// <param name="call">回调 <para>Callback</para></param>
        public new void RemoveListener(UnityAction call)
        {
            if (call == null || !_runtimeListeners.Remove(call))
                return;
            base.RemoveListener(call);
        }

        /// <summary>
        /// 移除所有监听者（隐藏基类实现以维护计数）。
        /// <para>Removes all listeners (hides the base method to keep the count).</para>
        /// </summary>
        public new void RemoveAllListeners()
        {
            _runtimeListeners.Clear();
            base.RemoveAllListeners();
        }
    }
}