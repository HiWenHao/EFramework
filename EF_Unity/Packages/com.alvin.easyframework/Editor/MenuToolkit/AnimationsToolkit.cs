/*
 * ================================================
 * Describe:        The class to used clip animations.
 * Author:          Faquan.Xue
 * CreationTime:    2023-04-19-17:34:01
 * ModifyAuthor:  Alvin5100
 * ModifyTime:    2026-04-01 15:42:00
 * ScriptVersion: 0.1
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
            Object[] selection = Selection.GetFiltered(typeof(Object), SelectionMode.DeepAssets);
            foreach (Object obj in selection)
            {
                if (obj is DefaultAsset)
                    continue;
                AnimationClip clip = obj as AnimationClip;
                if (clip == null)
                {
                    D.Error($"The object: <<{obj.name}>> is not a animation clip");
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
                    D.Error($"The <<{obj.name}>> is not a folder, please reselection.");
                    continue;
                }

                DirectoryInfo directoryInfo = new DirectoryInfo(path);
                FileInfo[] files = directoryInfo.GetFiles("*.anim");
                foreach (FileInfo file in files)
                {
                    string fbxPath = file.FullName[file.FullName.IndexOf("Assets", StringComparison.Ordinal)..];
                    AnimationClip clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(fbxPath);
                    CullCurves(clip);
                }
            }
        }

        [MenuItem("Assets/EF/Animation/Extract Clips Compress", false, 52)]
        private static void GetAnimationClipAndCompress()
        {
            Object[] objects = Selection.GetFiltered<Object>(SelectionMode.Assets);
            foreach (var obj in objects)
            {
                var path = AssetDatabase.GetAssetPath(obj);
                if (string.IsNullOrEmpty(path))
                    continue;
                if (Directory.Exists(path))
                {
                    DirectoryInfo directoryInfo = new DirectoryInfo(path);
                    string pt = directoryInfo.FullName.Remove(
                        directoryInfo.FullName.IndexOf("Assets", StringComparison.Ordinal));
                    pt += ConfigManager.Path.ExtractPath + directoryInfo.Name;

                    if (Directory.Exists(pt))
                        Directory.Delete(pt, true);
                    Directory.CreateDirectory(pt);

                    FileInfo[] files = directoryInfo.GetFiles("*.FBX");
                    foreach (FileInfo file in files)
                    {
                        if (file.Extension == ".fbx" || file.Extension == ".FBX")
                        {
                            AnimationClip clip =
                                AssetDatabase.LoadAssetAtPath<AnimationClip>(
                                    file.FullName[file.FullName.IndexOf("Assets")..]);
                            ExtractClips(clip, directoryInfo.Name);
                        }
                    }
                }
                else
                {
                    AnimationClip clip =
                        AssetDatabase.LoadAssetAtPath<AnimationClip>(
                            path[path.IndexOf("Assets", StringComparison.Ordinal)..]);
                    if (!clip)
                    {
                        D.Error($"The <<{obj.name}>> is not a FBX/fbx file or folder, please reselection.");
                        continue;
                    }

                    if (!Directory.Exists(ConfigManager.Path.ExtractPath + "Extract"))
                        Directory.CreateDirectory(ConfigManager.Path.ExtractPath + "Extract");
                    ExtractClips(clip, "Extract");
                }
            }
        }

        private static void ExtractClips(AnimationClip clip, string directoryName)
        {
            AnimationClip tempAC = new AnimationClip();
            EditorUtility.CopySerialized(clip, tempAC);
            CullCurves(tempAC);
            string path = ConfigManager.Path.ExtractPath + directoryName + "/" +
                          EditorUtils.RemovePunctuation(tempAC.name);
            int clipIndex = 0;
            while (File.Exists($"{path}_{clipIndex}.anim"))
            {
                clipIndex++;
            }

            AssetDatabase.CreateAsset(tempAC, $"{path}_{clipIndex}.anim");
            EditorUtility.SetDirty(tempAC);
        }

        private static void CullCurves(AnimationClip clip)
        {
            if (clip == null) return;
            // 获取Animation的所有Curve
            EditorCurveBinding[] binds = AnimationUtility.GetCurveBindings(clip);
            string floatFormat = "f3";

            foreach (var bind in binds)
            {
                // 通常名称都是m_LocalScale.(x/y/z),如果是就置空
                if (bind.propertyName.Contains("Scale"))
                    AnimationUtility.SetEditorCurve(clip, bind, null);
                else
                {
                    AnimationCurve curve = AnimationUtility.GetEditorCurve(clip, bind);
                    if (curve == null)
                        continue;
                    var keys = curve.keys;
                    for (int index = 0; index < keys.Length; index++)
                    {
                        Keyframe _keyframe = keys[index];
                        _keyframe.time = float.Parse(_keyframe.time.ToString(floatFormat));
                        _keyframe.value = float.Parse(_keyframe.value.ToString(floatFormat));
                        _keyframe.inTangent = float.Parse(_keyframe.inTangent.ToString(floatFormat));
                        _keyframe.outTangent = float.Parse(_keyframe.outTangent.ToString(floatFormat));
                        _keyframe.inWeight = float.Parse(_keyframe.inWeight.ToString(floatFormat));
                        _keyframe.outWeight = float.Parse(_keyframe.outWeight.ToString(floatFormat));
                        keys[index] = _keyframe;
                    }

                    // struct 需要重新指定
                    curve.keys = keys;
                    // 重新指定
                    AnimationUtility.SetEditorCurve(clip, bind, curve);
                }
            }

            //删除editor配置信息
            SerializedObject so = new SerializedObject(clip);
            so.FindProperty("m_EditorCurves").arraySize = 0;
            so.FindProperty("m_EulerEditorCurves").arraySize = 0;
            so.ApplyModifiedProperties();
            EditorUtility.SetDirty(clip);
            AssetDatabase.SaveAssets();
        }
    }
}
