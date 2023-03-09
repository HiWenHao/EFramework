/*
 * ================================================
 * Describe:        The class is ui page base class.
 * Author:          Xiaohei.Wang(Wenhao)
 * CreationTime:    2022-05-14:33:01
 * ModifyAuthor:    Xiaohei.Wang(Wenhao)
 * ModifyTime:      2022-05-14:33:01
 * Version:         1.0
 * ===============================================
 */
using UnityEngine;

namespace EasyFramework.UI
{
    public abstract class UIPageBase : IUIPageBase
    {
        private bool m_Focus = true;

        /// <summary>
        /// Page serial number, do not change.页面序列号,请勿改动
        /// </summary>
        public int SerialId { get; set; }

        /// <summary>
        /// Focus or not.是否被聚焦
        /// </summary>
        public bool IsFocus => m_Focus;

        public abstract void Awake(GameObject obj, params object[] args);

        public virtual void Open(params object[] args) { }

        public virtual void Update() { }

        public virtual void OnFocus(bool focus, params object[] args) 
        {
            m_Focus = focus;
        }

        public virtual void Close() { }

        public abstract void Quit();
    }
}
