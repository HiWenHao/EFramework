/*
 * ================================================
 * Describe:        The class is ui page base class.
 * Author:          Xiaohei.Wang(Wenhao)
 * CreationTime:    2022-05-01 14:33:01
 * ModifyAuthor:    Xiaohei.Wang(Wenhao)
 * ModifyTime:      2024-07-08 17:06:55
 * Version:         2.0
 * ===============================================
 */

using EasyFramework.Manager.UI;
using UnityEngine;

namespace EasyFramework.UI
{
    /// <summary>
    /// UI界面接口
    /// </summary>
    public abstract class UIPageView : IUIPageView
    {
        public int SerialId { get; set; }
        public bool IsFocus { get; set; }

        public abstract void Awake(GameObject obj, params object[] args);

        public virtual void Open(params object[] args) { }

        public virtual void Update(float elapse, float realElapse) { }

        public virtual void OnFocus(bool focus, params object[] args) 
        {
            IsFocus = focus;
        }

        public virtual void Close() { }

        public abstract void Quit();
    }
}
