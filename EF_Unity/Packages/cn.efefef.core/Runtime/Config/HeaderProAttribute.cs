/*
 * ================================================
 * Describe:      用来拓展HeaderAttribute，能随意切换中英文，后面还可以拓展
 * Author:        Alvin8412
 * CreationTime:  2026-04-26 00:11:48
 * ModifyAuthor:  Alvin8412
 * ModifyTime:    2026-04-26 00:11:48
 * ScriptVersion: 0.1
 * ===============================================
 */

using System;
using UnityEngine;

namespace EasyFramework.Edit
{
    /// <summary>
    ///	使用此“属性属性”来在“检查器”中的某些字段上方添加标题。
    /// <para>Use this PropertyAttribute to add a header above some fields in the Inspector.</para>
    /// </summary>
    [AttributeUsage(AttributeTargets.Field, Inherited = true, AllowMultiple = true)]
    public class HeaderProAttribute : PropertyAttribute
    {
        public string Chinese;
        public string English;
        public HeaderProAttribute(string chinese, string english)
        {
            Chinese = chinese;
            English = english;
        }
    }
}