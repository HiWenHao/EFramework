/*
 * ================================================
 * Describe:      This script is used to .
 * Author:        Alvin5100(Wang)
 * CreationTime:  2025-12-10 09:50:26
 * ModifyAuthor:  Alvin5100(Wang)
 * ModifyTime:    2025-12-10 09:50:26
 * ScriptVersion: 0.1
 * ===============================================
*/

using System.Collections.Generic;
using UnityEngine;

namespace EasyFramework.Managers.Pool
{
    /// <summary>
    /// 对象池监控器（用于调试）
    /// </summary>
    public class PoolMonitor : MonoBehaviour
    {
        [Header("监控设置")] [SerializeField] 
        private bool showGUI = true;
        [SerializeField] 
        private Vector2 guiPosition = new Vector2(10, 10);
        [SerializeField] 
        private float updateInterval = 1f;

        private Dictionary<string, PoolStatistics> _poolStats = new Dictionary<string, PoolStatistics>();
        private float _lastUpdateTime;
        
        private void OnGUI()
        {
            if (!showGUI || !Application.isPlaying) return;

            GUILayout.BeginArea(new Rect(guiPosition.x, guiPosition.y, 400, 600));
            GUILayout.BeginVertical("Box");

            GUILayout.Label("Object Pool Monitor",
                new GUIStyle(GUI.skin.label) { fontSize = 16, fontStyle = FontStyle.Bold });

            GUILayout.Space(10);

            foreach (var kvp in _poolStats)
            {
                var stat = kvp.Value;

                GUILayout.BeginVertical("Box");
                GUILayout.Label($"Pool: {stat.PoolName}");
                GUILayout.Label($"Available: {stat.AvailableCount}");
                GUILayout.Label($"Active: {stat.ActiveCount}");
                GUILayout.Label($"Total: {stat.TotalCreated}");
                GUILayout.Label($"Peak: {stat.PeakActiveCount}");
                GUILayout.EndVertical();
                GUILayout.Space(5);
            }

            if (_poolStats.Count == 0)
            {
                GUILayout.Label("No active pools found");
            }

            GUILayout.EndVertical();
            GUILayout.EndArea();
        }
    }
}
