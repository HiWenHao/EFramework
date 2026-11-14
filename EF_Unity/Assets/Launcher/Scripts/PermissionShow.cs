/*
 * ================================================
 * Describe:        This script is used to .
 * Author:          Alvin8412
 * CreationTime:    2026-11-14 19:29:47
 * ModifyAuthor:    Alvin8412
 * ModifyTime:      2026-11-14 19:29:47
 * ScriptVersion:   0.1
 * ================================================
 */

using System;
using UnityEngine;
using EasyFramework.Launcher;

namespace Launch
{
    /// <summary>
    /// 需用到的权限使用说明
    /// </summary>
    public class PermissionShow : MonoBehaviour, ILauncherHook
    {
        private Action _onComplete;

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.R))
            {
                _onComplete?.Invoke();
            }
        }

        public void OnBoot(Action onComplete)
        {
            _onComplete = onComplete;
        }
    }
}