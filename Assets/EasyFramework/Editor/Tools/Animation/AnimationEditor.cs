/*
 * ================================================
 * Describe:        The class to used clip animation.
 * Author:          Faquan.Xue
 * CreationTime:    2023-04-19-17:34:01
 * ModifyAuthor:    Xiaohei.Wang(Wenhao)
 * ModifyTime:      2023-04-21-
 * Version:         1.0
 * ===============================================
 */

using System.IO;
using UnityEditor;
using UnityEngine;

public class AnimationEditor : Editor
{
    [MenuItem("Assets/EF/Animation Tools/提取并且压缩动画文件", false, 30)]
    public static void GetAniamtionClipAndCompress()
    {
        string savePath = "Assets/Res/Animation";
        string dir = "";
        foreach(var v in Selection.GetFiltered<UnityEngine.Object>(SelectionMode.Assets))
        {
            var path = AssetDatabase.GetAssetPath(v);
            if (string.IsNullOrEmpty(path))
                continue;
            if (System.IO.Directory.Exists(path))
                dir = path;
        }

        if(!string.IsNullOrEmpty(dir))
        {
            DirectoryInfo directoryInfo = new DirectoryInfo(dir);
            savePath = savePath + "/" + directoryInfo.Name;
   
            string p = directoryInfo.FullName.Remove(directoryInfo.FullName.IndexOf("Assets"));
            p = p  + savePath;

            if(Directory.Exists(p))
            {
                Directory.Delete(p, true);
            }
            Directory.CreateDirectory(p);
            FileInfo[] files = directoryInfo.GetFiles("*.FBX");
            foreach (FileInfo file in files)
            {
                if(file.Extension ==".fbx" || file.Extension == ".FBX")
                {

                   string fbxpath = file.FullName.Substring(file.FullName.IndexOf("Assets"));
                    AnimationClip clip = AssetDatabase.LoadAssetAtPath(fbxpath, typeof(AnimationClip))as AnimationClip;
                    AnimationClip temp = new AnimationClip();
                    EditorUtility.CopySerialized(clip, temp);
                    CullCurves(temp);
                    AssetDatabase.CreateAsset(temp, savePath + "/"+temp.name+ ".anim");
                    EditorUtility.SetDirty(temp);
                    
                }
            }
        }
    }


    [MenuItem("Assets/EF/Animation Tools/压缩该目录下的动画文件", false, 31)]
    public static void CompressAnimation()
    {
       
        string dir = "";
        foreach (var v in Selection.GetFiltered<UnityEngine.Object>(SelectionMode.Assets))
        {
            var path = AssetDatabase.GetAssetPath(v);
            if (string.IsNullOrEmpty(path))
                continue;
            if (System.IO.Directory.Exists(path))
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

    [MenuItem("Assets/EF/Animation Tools/压缩单个动画文件", false, 32)]
    static void SigleAnimation()
    {
        UnityEngine.Object[] selection = Selection.GetFiltered(typeof(UnityEngine.Object), SelectionMode.DeepAssets);

        foreach (UnityEngine.Object obj in selection)
        {
            if (obj is DefaultAsset)
                continue;
            AnimationClip clip = obj as AnimationClip;
            if (clip == null)
                continue;
            CullCurves(clip);
        }

    }

    static void CullCurves(AnimationClip clip)
    {
        if (clip == null) return;
        // 获取Animation的所有Curve
        var binds = AnimationUtility.GetCurveBindings(clip);
        var floatFormat = "f3";

        foreach (var bind in binds)
        {
            // 通常名称都是m_LocalScale.(x/y/z),如果是就置空
            if (bind.propertyName.Contains("Scale"))
                AnimationUtility.SetEditorCurve(clip, bind, null);
            else
            {
                var curve = AnimationUtility.GetEditorCurve(clip, bind);
                if (curve == null)
                    continue;
                var keys = curve.keys;
                for (int index = 0; index < keys.Length; index++)
                {
                    var keyframe = keys[index];

                    keyframe.value = float.Parse(keyframe.value.ToString(floatFormat));
                    keyframe.inTangent = float.Parse(keyframe.inTangent.ToString(floatFormat));
                    keyframe.outTangent = float.Parse(keyframe.outTangent.ToString(floatFormat));
                    keyframe.inWeight = float.Parse(keyframe.inWeight.ToString(floatFormat));
                    keyframe.outWeight = float.Parse(keyframe.outWeight.ToString(floatFormat));
                    keys[index] = keyframe;
                }
                // struct 需要重新指定
                curve.keys = keys;
                // 重新指定
                AnimationUtility.SetEditorCurve(clip, bind, curve);
            }
        }

        //删除editor配置信息
        var so = new SerializedObject(clip);
        so.FindProperty("m_EditorCurves").arraySize = 0;
        so.FindProperty("m_EulerEditorCurves").arraySize = 0;
        so.ApplyModifiedProperties();
        EditorUtility.SetDirty(clip);
        // 重新保存
        AssetDatabase.SaveAssets();
        Debug.Log("complete ");

        //foreach (var bind in binds)
        //{
        //    var curve = AnimationUtility.GetEditorCurve(clip, bind);
        //    if (curve == null)
        //        Debug.Log("11");
        //    var keys = curve.keys;

        //    if (bind.propertyName.Contains("Scale"))
        //        AnimationUtility.SetEditorCurve(clip, bind, null);
        //    for (int index = 0; index < keys.Length; index++)
        //    {
        //        var keyframe = keys[index];

        //        keyframe.value = float.Parse(keyframe.value.ToString(floatFormat));
        //        keyframe.inWeight = float.Parse(keyframe.inWeight.ToString(floatFormat));
        //        keyframe.outWeight = float.Parse(keyframe.outWeight.ToString(floatFormat));
        //        keyframe.inTangent = float.Parse(keyframe.inTangent.ToString(floatFormat));
        //        keyframe.outTangent = float.Parse(keyframe.outTangent.ToString(floatFormat));

        //        keys[index] = keyframe;
        //    }
        //    // struct 需要重新指定
        //    curve.keys = keys;
        //    // 重新指定
        //    AnimationUtility.SetEditorCurve(clip, bind, curve);
        //}




    }


}
