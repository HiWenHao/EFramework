/*
 * ================================================
 * Describe:        The class to used clip animations.
 * Author:          Faquan.Xue
 * CreationTime:    2023-04-19-17:34:01
 * ModifyAuthor:    Alvin8412
 * ModifyTime:      2026-06-01 15:48:00
 * ScriptVersion:   0.2
 * ===============================================
 */

using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace EasyFramework.Edit.MenuToolkit
{
    internal static class AnimationsToolkit
    {
        [MenuItem("Assets/EF/Animation/Compress One Clip", false, 50)]
        private static void CompressAnimation()
        {
            foreach (Object obj in Selection.GetFiltered(typeof(Object), SelectionMode.Assets))
            {
                if (obj is DefaultAsset)
                    continue;
                AnimationClip clip = obj as AnimationClip;
                if (clip == null)
                {
                    D.Error($"The object: << {obj.name} >> is not a animation clip");
                    continue;
                }

                CullCurves(clip);
            }
        }

        [MenuItem("Assets/EF/Animation/Compress All Clips", false, 51)]
        private static void CompressAnimations()
        {
            foreach (var obj in Selection.GetFiltered<Object>(SelectionMode.Assets))
            {
                var path = AssetDatabase.GetAssetPath(obj);
                if (string.IsNullOrEmpty(path))
                    continue;

                if (!Directory.Exists(path))
                {
                    D.Error($"The << {obj.name} >> is not a folder, please reselection.");
                    continue;
                }

                DirectoryInfo directoryInfo = new DirectoryInfo(path);
                FileInfo[] files = directoryInfo.GetFiles("*.anim");
                foreach (FileInfo file in files)
                {
                    string relativePath = file.FullName.SafeSubstring("Assets");
                    if (string.IsNullOrEmpty(relativePath)) continue;
                    AnimationClip clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(relativePath);
                    if (clip != null) CullCurves(clip);
                }
            }
        }

        [MenuItem("Assets/EF/Animation/Extract Clips Compress", false, 52)]
        private static void GetAnimationClipAndCompress()
        {
            foreach (Object obj in Selection.GetFiltered<Object>(SelectionMode.Assets))
            {
                var path = AssetDatabase.GetAssetPath(obj);
                if (string.IsNullOrEmpty(path))
                    continue;

                if (Directory.Exists(path))
                {
                    DirectoryInfo directoryInfo = new DirectoryInfo(path);
                    string pt = directoryInfo.FullName.RemoveSafeAfter("Assets");
                    if (string.IsNullOrEmpty(pt)) continue;
                    pt += ConfigManager.Path.ExtractPath + directoryInfo.Name;

                    if (Directory.Exists(pt))
                        Directory.Delete(pt, true);
                    Directory.CreateDirectory(pt);

                    FileInfo[] files = directoryInfo.GetFiles("*.FBX");
                    foreach (FileInfo file in files)
                    {
                        if (!file.Extension.Equals(".fbx", StringComparison.OrdinalIgnoreCase))
                            continue;
                        string relativePath = file.FullName.SafeSubstring("Assets");
                        if (string.IsNullOrEmpty(relativePath)) continue;
                        AnimationClip clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(relativePath);
                        if (clip != null) ExtractClips(clip, directoryInfo.Name);
                    }
                }
                else
                {
                    string relativePath = path.SafeSubstring("Assets");
                    if (string.IsNullOrEmpty(relativePath)) continue;
                    AnimationClip clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(relativePath);
                    if (!clip)
                    {
                        D.Error($"The << {obj.name} >> is not a FBX/fbx file or folder, please reselection.");
                        continue;
                    }

                    if (!Directory.Exists(ConfigManager.Path.ExtractPath + "Extract"))
                        Directory.CreateDirectory(ConfigManager.Path.ExtractPath + "Extract");
                    ExtractClips(clip, "Extract");
                }
            }
        }

        /// <summary>
        /// 提取动画剪辑并压缩
        /// <para>Extract and compress animation clips from a FBX source</para>
        /// </summary>
        private static void ExtractClips(AnimationClip clip, string directoryName)
        {
            if (clip == null) return;

            AnimationClip tempClip = new AnimationClip();
            EditorUtility.CopySerialized(clip, tempClip);
            CullCurves(tempClip);

            string path = Path.Combine(
                ConfigManager.Path.ExtractPath + directoryName,
                EditorUtils.RemovePunctuation(tempClip.name));

            int clipIndex = 0;
            const int maxIndex = 10000;
            while (File.Exists($"{path}_{clipIndex}.anim") && clipIndex < maxIndex)
            {
                clipIndex++;
            }

            AssetDatabase.CreateAsset(tempClip, $"{path}_{clipIndex}.anim");
            EditorUtility.SetDirty(tempClip);
        }

        /// <summary>
        /// 压缩动画曲线：移除 Scale 曲线 + 浮点精度截断到 f3
        /// <para>Compress animation curves: remove Scale curves + truncate float precision to f3</para>
        /// </summary>
        private static void CullCurves(AnimationClip clip)
        {
            if (clip == null) return;
            EditorCurveBinding[] binds = AnimationUtility.GetCurveBindings(clip);
            const string floatFormat = "f3";

            foreach (var bind in binds)
            {
                if (bind.propertyName.Contains("Scale"))
                {
                    AnimationUtility.SetEditorCurve(clip, bind, null);
                    continue;
                }

                AnimationCurve curve = AnimationUtility.GetEditorCurve(clip, bind);
                if (curve == null) continue;
                var keys = curve.keys;
                for (int i = 0; i < keys.Length; i++)
                {
                    Keyframe keyframe = keys[i];
                    keyframe.time = float.Parse(keyframe.time.ToString(floatFormat));
                    keyframe.value = float.Parse(keyframe.value.ToString(floatFormat));
                    keyframe.inTangent = float.Parse(keyframe.inTangent.ToString(floatFormat));
                    keyframe.outTangent = float.Parse(keyframe.outTangent.ToString(floatFormat));
                    keyframe.inWeight = float.Parse(keyframe.inWeight.ToString(floatFormat));
                    keyframe.outWeight = float.Parse(keyframe.outWeight.ToString(floatFormat));
                    keys[i] = keyframe;
                }

                curve.keys = keys;
                AnimationUtility.SetEditorCurve(clip, bind, curve);
            }

            SerializedObject so = new SerializedObject(clip);
            so.FindProperty("m_EditorCurves").arraySize = 0;
            so.FindProperty("m_EulerEditorCurves").arraySize = 0;
            so.ApplyModifiedProperties();
            EditorUtility.SetDirty(clip);
            AssetDatabase.SaveAssets();
        }

        #region Helpers

        private static string SafeSubstring(this string fullPath, string marker)
        {
            int idx = fullPath.IndexOf(marker, StringComparison.Ordinal);
            return idx >= 0 ? fullPath[idx..] : null;
        }

        private static string RemoveSafeAfter(this string fullPath, string marker)
        {
            int idx = fullPath.IndexOf(marker, StringComparison.Ordinal);
            return idx >= 0 ? fullPath.Remove(idx) : null;
        }

        #endregion
    }
}
