/*
 * ================================================
 * Describe:        This script is used to .
 * Author:          Alvin8412
 * CreationTime:    2026-11-14 19:04:45
 * ModifyAuthor:    Alvin8412
 * ModifyTime:      2026-11-14 19:04:45
 * ScriptVersion:   0.1
 * ================================================
 */

using System;
using EasyFramework.Launcher;
using UnityEngine;
using UnityEngine.UI;

namespace Launch
{
    /// <summary>
    /// 启动提示
    /// </summary>
    public class LaunchViewTips : MonoBehaviour, ILauncherView
    {
        [SerializeField] private Text Contents;
        [SerializeField] private Text Progress;
        public void OnStateChanged(LauncherState state, string tip)
        {
            Contents.text = tip;
        }

        public void OnProgress(long currentCount, long totalCount, long currentBytes, long totalBytes)
        {
            Progress.text = $"{currentCount}/{totalCount}";
        }

        public void OnError(LauncherErrorCode code, string message, Action retry)
        {
            
        }
    }
}