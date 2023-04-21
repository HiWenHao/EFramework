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

using EasyFramework.Edit;
using System.IO;
using UnityEditor;
using UnityEngine;
using XHTools;

public class AnimationEditor : Editor
{
    [MenuItem("Assets/EF/Animation Tools/压缩该目录下的动画文件", false, 31)]
    public static void CompressAnimation()
    {
       
        string dir = "";
        foreach (var v in Selection.GetFiltered<Object>(SelectionMode.Assets))
        {
            var path = AssetDatabase.GetAssetPath(v);
            if (string.IsNullOrEmpty(path))
                continue;
            if (Directory.Exists(path))
                dir = path;
        }

        if (!string.IsNullOrEmpty(dir))
        {
            DirectoryInfo directoryInfo = new DirectoryInfo(dir);
            FileInfo[] files = directoryInfo.GetFiles("*.anim");
            foreach (FileInfo file in files)
            {
                string fbxpath = file.FullName.Substring(file.FullName.IndexOf("Assets"));
                AnimationClip clip = AssetDatabase.LoadAssetAtPath(fbxpath, typeof(AnimationClip)) as AnimationClip;
                CullCurves(clip);
            }
        }
    }

    [MenuItem("Assets/EF/Animation Tools/压缩单个动画文件", false, 30)]
    static void SigleAnimation()
    {
        Object[] selection = Selection.GetFiltered(typeof(Object), SelectionMode.DeepAssets);

        foreach (Object obj in selection)
        {
            if (obj is DefaultAsset)
                continue;
            AnimationClip clip = obj as AnimationClip;
            if (clip == null)
                continue;
            CullCurves(clip);
        }

    }

    [MenuItem("Assets/EF/Animation Tools/提取并且压缩动画文件", false, 32)]
    public static void GetAniamtionClipAndCompress()
    {
        string _savePath = ProjectSettingsUtils.Optimal.ExtractPath;
        int _ClipIndex = 0;
        string _dir = "";
        foreach (var v in Selection.GetFiltered<Object>(SelectionMode.Assets))
        {
            var path = AssetDatabase.GetAssetPath(v);
            D.Correct(path);
            if (string.IsNullOrEmpty(path))
                continue;
            if (Directory.Exists(path))
                _dir = path;
        }

        if (!string.IsNullOrEmpty(_dir))
        {
            DirectoryInfo directoryInfo = new DirectoryInfo(_dir);
            _savePath += $"/{directoryInfo.Name}";

            string _pt = directoryInfo.FullName.Remove(directoryInfo.FullName.IndexOf("Assets"));
            _pt += _savePath;

            if (Directory.Exists(_pt))
                Directory.Delete(_pt, true);

            Directory.CreateDirectory(_pt);
            FileInfo[] files = directoryInfo.GetFiles("*.FBX");
            foreach (FileInfo file in files)
            {
                if (file.Extension == ".fbx" || file.Extension == ".FBX")
                {
                    AnimationClip _clip = AssetDatabase.LoadAssetAtPath(file.FullName.Substring(file.FullName.IndexOf("Assets")), typeof(AnimationClip)) as AnimationClip;
                    AnimationClip _tempAC = new AnimationClip();
                    EditorUtility.CopySerialized(_clip, _tempAC);
                    CullCurves(_tempAC);
                    string _path = _savePath + "/" + EditorUtils.RemovePunctuation(_tempAC.name);
                    while (File.Exists($"{_path}_{_ClipIndex}.anim"))
                    {
                        _ClipIndex++;
                    }
                    AssetDatabase.CreateAsset(_tempAC, $"{_path}_{_ClipIndex}.anim");
                    EditorUtility.SetDirty(_tempAC);
                }
            }
        }
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
