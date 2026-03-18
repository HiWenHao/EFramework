/* 
 * ================================================
 * Describe:      This script is used to create template script.
 * Author:        Xiaohei.Wang(Wenhao)
 * CreationTime:  2022-06-07 18:18:36
 * ModifyAuthor:  Xiaohei.Wang(Wenhao)
 * ModifyTime:    2024-10-16 15:17:28
 * Version:       0.1 
 * ===============================================
 */

using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.ProjectWindowCallback;
using UnityEngine;

namespace EasyFramework.Edit
{
    class CreateTemplateScript
    {
        //菜单项
        [MenuItem("Assets/Create/EF/C# Scripts/TemplateScript", false, 1)]
        static void CreateScript()
        {
            CreateScriptAsset._scriptName = "TemplateScript";
            ProjectWindowUtil.StartNameEditingIfProjectWindowExists(0, ScriptableObject.CreateInstance<CreateScriptAsset>(),
            GetSelectedPathOrFallback() + "/NewTemplateScript.cs",
            null, Path.Combine(Utility.Path.GetEFAssetsPath(), "ScriptTemplate/TemplateScript.cs.txt"));
        }
        [MenuItem("Assets/Create/EF/C# Scripts/SingleScript", false, 2)]
        static void CreateSingleScript()
        {
            CreateScriptAsset._scriptName = "SingleScript";
            ProjectWindowUtil.StartNameEditingIfProjectWindowExists(0, ScriptableObject.CreateInstance<CreateScriptAsset>(),
            GetSelectedPathOrFallback() + "/NewSingleScript.cs",
            null, Path.Combine(Utility.Path.GetEFAssetsPath(), "ScriptTemplate/SingleScript.cs.txt"));
        }
        [MenuItem("Assets/Create/EF/C# Scripts/MonoSingleScript", false, 3)]
        static void CreateMonoSingleScript()
        {
            CreateScriptAsset._scriptName = "MonoSingleScript";
            ProjectWindowUtil.StartNameEditingIfProjectWindowExists(0, ScriptableObject.CreateInstance<CreateScriptAsset>(),
            GetSelectedPathOrFallback() + "/NewMonoSingleScript.cs",
            null, Path.Combine(Utility.Path.GetEFAssetsPath(), "ScriptTemplate/MonoSingleScript.cs.txt"));
        }

        [MenuItem("Assets/Create/EF/C# Scripts/GameLauncherScript", false, 20)]
        static void CreateGameLauncherScript()
        {
            CreateScriptAsset._scriptName = "GameLauncher";
            ProjectWindowUtil.StartNameEditingIfProjectWindowExists(0, ScriptableObject.CreateInstance<CreateScriptAsset>(),
            GetSelectedPathOrFallback() + "/GameLauncher.cs",
            null, Path.Combine(Utility.Path.GetEFAssetsPath(), "ScriptTemplate/GameLauncher.cs.txt"));
        }

        public static string GetSelectedPathOrFallback()
        {
            string path = "Assets";
            foreach (Object obj in Selection.GetFiltered(typeof(Object), SelectionMode.Assets))
            {
                path = AssetDatabase.GetAssetPath(obj);
                if (!string.IsNullOrEmpty(path) && File.Exists(path))
                {
                    path = Path.GetDirectoryName(path);
                    break;
                }
            }
            return path;
        }
    }
    class CreateScriptAsset : EndNameEditAction
    {
        public static string _scriptName = "";
        public override void Action(int instanceId, string newScriptPath, string templatePath)
        {
            Object obj = CreateTemplateScriptAsset(newScriptPath, templatePath);
            ProjectWindowUtil.ShowCreatedAsset(obj);
        }

        public static Object CreateTemplateScriptAsset(string newScriptPath, string templatePath)
        {
            string fullPath = Path.GetFullPath(newScriptPath);
            StreamReader streamReader = new StreamReader(templatePath);
            string text = streamReader.ReadToEnd();
            streamReader.Close();
            string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(newScriptPath);

            //替换模板的文件名
            text = Regex.Replace(text, _scriptName, fileNameWithoutExtension);
            //把内容重新写入脚本
            bool encoderShouldEmitUTF8Identifier = false;
            bool throwOnInvalidBytes = false;
            UTF8Encoding encoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier, throwOnInvalidBytes);
            bool append = false;
            StreamWriter sw = new StreamWriter(fullPath, append, encoding);

            sw.WriteLine("/*");
            sw.WriteLine(" * ================================================");
            sw.WriteLine(" * Describe:      This script is used to .");
            sw.WriteLine(" * Author:        " + ProjectUtility.Project.ScriptAuthor);
            sw.WriteLine(" * CreationTime:  " + System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
            sw.WriteLine(" * ModifyAuthor:  " + ProjectUtility.Project.ScriptAuthor);
            sw.WriteLine(" * ModifyTime:    " + System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
            sw.WriteLine(" * ScriptVersion: " + ProjectUtility.Project.ScriptVersion);
            sw.WriteLine(" * ===============================================");
            sw.WriteLine("*/");

            text = text.Replace("PleaseChangeTheNamespace", EditorUtils.LoadSettingAtPath<AutoBind.AutoBindSetting>().Namespace);
            sw.Write(text);
            sw.Close();
            AssetDatabase.ImportAsset(newScriptPath);
            return AssetDatabase.LoadAssetAtPath(newScriptPath, typeof(Object));
        }
    }
}