/*
 * ================================================
 * Describe:        The class to used clip animation.
 * Author:          Faquan.Xue
 * CreationTime:    2023-04-19-17:34:01
 * ModifyAuthor:    Xiaohei.Wang(Wenhao)
 * ModifyTime:      2023-04-21-10:36:28
 * Version:         1.0
 * ===============================================
 */

using System.IO;
using UnityEditor;
using UnityEngine;

namespace EasyFramework.Edit
{
    namespace AssetToolLibrary
    {
        public class AnimationEditor : Editor
        {
            [MenuItem("Assets/EF/Animation Tools/Compress One Clip", false, 50)]
            static void CompressAnimation()
            {
                Object[] _selection = Selection.GetFiltered(typeof(Object), SelectionMode.DeepAssets);
                foreach (Object obj in _selection)
                {
                    if (obj is DefaultAsset)
                        continue;
                    AnimationClip _clip = obj as AnimationClip;
                    if (_clip == null)
                    {
                        D.Error($"The object: <<{obj.name}>> is not a animation clip");
                        continue;
                    }
                    CullCurves(_clip);
                }
            }

            [MenuItem("Assets/EF/Animation Tools/Compress All Clips", false, 51)]
            public static void CompressAnimations()
            {
                foreach (var obj in Selection.GetFiltered<Object>(SelectionMode.Assets))
                {
                    var _path = AssetDatabase.GetAssetPath(obj);
                    if (string.IsNullOrEmpty(_path))
                        continue;

                    if (!Directory.Exists(_path))
                    {
                        D.Error($"The <<{obj.name}>> is not a folder, please reselection.");
                        continue;
                    }

                    DirectoryInfo _directoryInfo = new DirectoryInfo(_path);
                    FileInfo[] _files = _directoryInfo.GetFiles("*.anim");
                    foreach (FileInfo file in _files)
                    {
                        string _fbxPath = file.FullName.Substring(file.FullName.IndexOf("Assets"));
                        AnimationClip _clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(_fbxPath);
                        CullCurves(_clip);
                    }

                }
            }

            [MenuItem("Assets/EF/Animation Tools/Extract Clips Compress", false, 52)]
            public static void GetAniamtionClipAndCompress()
            {
                Object[] _objects = Selection.GetFiltered<Object>(SelectionMode.Assets);
                foreach (var obj in _objects)
                {
                    var path = AssetDatabase.GetAssetPath(obj);
                    if (string.IsNullOrEmpty(path))
                        continue;
                    if (Directory.Exists(path))
                    {
                        DirectoryInfo directoryInfo = new DirectoryInfo(path);
                        string _pt = directoryInfo.FullName.Remove(directoryInfo.FullName.IndexOf("Assets"));
                        _pt += ProjectUtility.Path.ExtractPath + directoryInfo.Name;

                        if (Directory.Exists(_pt))
                            Directory.Delete(_pt, true);
                        Directory.CreateDirectory(_pt);

                        FileInfo[] files = directoryInfo.GetFiles("*.FBX");
                        foreach (FileInfo file in files)
                        {
                            if (file.Extension == ".fbx" || file.Extension == ".FBX")
                            {
                                AnimationClip _clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(file.FullName[file.FullName.IndexOf("Assets")..]);
                                ExtractClips(_clip, directoryInfo.Name);
                            }
                        }
                    }
                    else
                    {
                        AnimationClip _clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(path[path.IndexOf("Assets")..]);
                        if (!_clip)
                        {
                            D.Error($"The <<{obj.name}>> is not a FBX/fbx file or folder, please reselection.");
                            continue;
                        }
                        if (!Directory.Exists(ProjectUtility.Path.ExtractPath + "Extract"))
                            Directory.CreateDirectory(ProjectUtility.Path.ExtractPath + "Extract");
                        ExtractClips(_clip, "Extract");
                    }
                }
            }

            static void ExtractClips(AnimationClip clip, string directoryName)
            {
                AnimationClip _tempAC = new AnimationClip();
                EditorUtility.CopySerialized(clip, _tempAC);
                CullCurves(_tempAC);
                string _path = ProjectUtility.Path.ExtractPath + directoryName + "/" + EditorUtils.RemovePunctuation(_tempAC.name);
                int _ClipIndex = 0;
                while (File.Exists($"{_path}_{_ClipIndex}.anim"))
                {
                    _ClipIndex++;
                }
                AssetDatabase.CreateAsset(_tempAC, $"{_path}_{_ClipIndex}.anim");
                EditorUtility.SetDirty(_tempAC);
            }
            static void CullCurves(AnimationClip clip)
            {
                if (clip == null) return;
                // 获取Animation的所有Curve
                EditorCurveBinding[] _binds = AnimationUtility.GetCurveBindings(clip);
                string _floatFormat = "f3";

                foreach (var bind in _binds)
                {
                    // 通常名称都是m_LocalScale.(x/y/z),如果是就置空
                    if (bind.propertyName.Contains("Scale"))
                        AnimationUtility.SetEditorCurve(clip, bind, null);
                    else
                    {
                        AnimationCurve _curve = AnimationUtility.GetEditorCurve(clip, bind);
                        if (_curve == null)
                            continue;
                        var _keys = _curve.keys;
                        for (int index = 0; index < _keys.Length; index++)
                        {
                            Keyframe _keyframe = _keys[index];
                            _keyframe.time = float.Parse(_keyframe.time.ToString(_floatFormat));
                            _keyframe.value = float.Parse(_keyframe.value.ToString(_floatFormat));
                            _keyframe.inTangent = float.Parse(_keyframe.inTangent.ToString(_floatFormat));
                            _keyframe.outTangent = float.Parse(_keyframe.outTangent.ToString(_floatFormat));
                            _keyframe.inWeight = float.Parse(_keyframe.inWeight.ToString(_floatFormat));
                            _keyframe.outWeight = float.Parse(_keyframe.outWeight.ToString(_floatFormat));
                            _keys[index] = _keyframe;
                        }
                        // struct 需要重新指定
                        _curve.keys = _keys;
                        // 重新指定
                        AnimationUtility.SetEditorCurve(clip, bind, _curve);
                    }
                }

                //删除editor配置信息
                SerializedObject _so = new SerializedObject(clip);
                _so.FindProperty("m_EditorCurves").arraySize = 0;
                _so.FindProperty("m_EulerEditorCurves").arraySize = 0;
                _so.ApplyModifiedProperties();
                EditorUtility.SetDirty(clip);
                AssetDatabase.SaveAssets();
            }
        }
    }
}