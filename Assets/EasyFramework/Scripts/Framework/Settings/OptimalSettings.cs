/* 
 * ================================================
 * Describe:      This script is used to .
 * Author:        Xiaohei.Wang(Wenhao)
 * CreationTime:  2023-04-20 19:35:11
 * ModifyAuthor:  Xiaohei.Wang(Wenhao)
 * ModifyTime:    2023-04-20 19:35:11
 * ScriptVersion: 0.1
 * ===============================================
*/
using System;
using UnityEngine;

namespace EasyFramework.Framework
{
    /// <summary>
    /// Used to optimal the project
    /// </summary>
    [Serializable]
    public class OptimalSettings
    {
        [SerializeField]
        [Tooltip("优化类型")]
        private string m_OptimalType = "000000000";
        public string OptimalType => m_OptimalType;
    }
}
