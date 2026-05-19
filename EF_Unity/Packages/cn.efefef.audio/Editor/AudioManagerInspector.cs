/*
 * ================================================
 * Describe:      AudioSystem 自定义 Inspector，配置隐藏字段并提供运行时调试。
 * Author:        EasyFramework
 * CreationTime:  2026-05-10
 * ModifyAuthor:  Alvin5100
 * ModifyTime:    2026-05-10
 * ScriptVersion: 1.1
 * ================================================
 */

#if UNITY_EDITOR
using EasyFramework.Edit;
using EasyFramework.Edit.Windows;
using UnityEditor;
using UnityEngine;

namespace EasyFramework.Managers.Audio.Editor
{
    [CustomEditor(typeof(AudioManager))]
    public class AudioManagerInspector : UnityEditor.Editor
    {
        private SerializedProperty audioMixerProp;
        private SerializedProperty defaultBgmVolumeProp;
        private SerializedProperty defaultSfxVolumeProp;
        private SerializedProperty maxSfxPoolSizeProp;
        private SerializedProperty prewarmCountProp;

        private AudioClip testClip;
        private bool showRuntimeInfo = true;

        private void OnEnable()
        {
            audioMixerProp = serializedObject.FindProperty("audioMixer");
            defaultBgmVolumeProp = serializedObject.FindProperty("defaultBgmVolume");
            defaultSfxVolumeProp = serializedObject.FindProperty("defaultSfxVolume");
            maxSfxPoolSizeProp = serializedObject.FindProperty("maxSfxPoolSize");
            prewarmCountProp = serializedObject.FindProperty("prewarmCount");
        }

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            
            EditorGUILayout.Space(10);
            var titleRect = GUILayoutUtility.GetRect(GUIContent.none, GUIUtils.InspectorTitle());
            EditorGUI.DrawRect(titleRect, new Color(0.2f, 0.2f, 0.2f, 0.3f));
            EditorGUI.LabelField(titleRect, LC.Combine(Lc.Audio, Lc.Data, Lc.Panel ), GUIUtils.InspectorTitle());

            
            var sys = (AudioManager)target;
            serializedObject.Update();

            // 混音器
            EditorGUILayout.LabelField("混音器 / Mixer", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(audioMixerProp, new GUIContent("AudioMixer", "主混音器资源"));

            EditorGUILayout.Space();

            // 默认音量
            EditorGUILayout.LabelField("默认音量 / Default Volume", EditorStyles.boldLabel);
            EditorGUILayout.Slider(defaultBgmVolumeProp, 0f, 1f, "BGM 音量");
            EditorGUILayout.Slider(defaultSfxVolumeProp, 0f, 1f, "SFX 音量");

            EditorGUILayout.Space();

            // 对象池
            EditorGUILayout.LabelField("音频池 / Audio Source Pool", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(maxSfxPoolSizeProp, new GUIContent("最大空闲数", "对象池最多保留的空闲 AudioSource 数量"));
            EditorGUILayout.PropertyField(prewarmCountProp, new GUIContent("预热数量", "游戏启动时预先创建的音效播放器数量"));

            serializedObject.ApplyModifiedProperties();

            // 运行时信息
            if (Application.isPlaying && sys.gameObject.activeInHierarchy)
            {
                EditorGUILayout.Space();
                showRuntimeInfo = EditorGUILayout.Foldout(showRuntimeInfo, "运行时 / Runtime", true);
                if (showRuntimeInfo)
                {
                    EditorGUI.indentLevel++;
                    GUI.enabled = false;
                    EditorGUILayout.Toggle("全局静音", sys.IsMuted);
                    EditorGUILayout.FloatField("BGM 音量", sys.GetBgmVolume());
                    EditorGUILayout.FloatField("SFX 音量", sys.GetSfxVolume());
                    EditorGUILayout.FloatField("Master 音量", sys.GetMasterVolume());
                    EditorGUILayout.IntField("活跃音效数", GetActiveEffectCount(sys));
                    GUI.enabled = true;
                    EditorGUI.indentLevel--;
                }

                EditorGUILayout.Space();
            }

            if (GUI.changed)
                EditorUtility.SetDirty(target);
        }

        private int GetActiveEffectCount(AudioManager sys)
        {
            var field = typeof(AudioManager).GetField("_activeEffects",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (field != null)
            {
                var list = field.GetValue(sys);
                if (list is System.Collections.IList iList)
                    return iList.Count;
            }
            return -1;
        }
    }
}
#endif