/* 
 * ================================================
 * Describe:      This script is used to create template script.
 * Author:        Xiaohei.Wang(Wenhao)
 * CreationTime:  2022-06-07 18:18:36
 * ModifyAuthor:  Xiaohei.Wang(Wenhao)
 * ModifyTime:    2022-06-07 18:18:36
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
        //脚本模板路径
        private readonly static string SingleScriptPath = ProjectUtility.Path.FrameworkPath + "Editor/Create/ScriptTemplate/Template/SingleScript.cs.txt";
        private readonly static string TemplateScriptPath = ProjectUtility.Path.FrameworkPath + "Editor/Create/ScriptTemplate/Template/TemplateScript.cs.txt";
        private readonly static string MonoSingleScriptPath = ProjectUtility.Path.FrameworkPath + "Editor/Create/ScriptTemplate/Template/MonoSingleScript.cs.txt";

        //菜单项
        [MenuItem("Assets/Create/EF/C# Scripts/TemplateScript", false, 1)]
        static void CreateScript()
        {
            CreateScriptAsset.ScriptName = "TemplateScript";
            ProjectWindowUtil.StartNameEditingIfProjectWindowExists(0, ScriptableObject.CreateInstance<CreateScriptAsset>(),
            GetSelectedPathOrFallback() + "/NewTemplateScript.cs",
            null, TemplateScriptPath);
        }
        [MenuItem("Assets/Create/EF/C# Scripts/SingleScript", false, 2)]
        static void CreateSingleScript()
        {
            CreateScriptAsset.ScriptName = "SingleScript";
            ProjectWindowUtil.StartNameEditingIfProjectWindowExists(0, ScriptableObject.CreateInstance<CreateScriptAsset>(),
            GetSelectedPathOrFallback() + "/NewSingleScript.cs",
            null, SingleScriptPath);
        }
        [MenuItem("Assets/Create/EF/C# Scripts/MonoSingleScript", false, 3)]
        static void CreateMonoSingleScript()
        {
            CreateScriptAsset.ScriptName = "MonoSingleScript";
            ProjectWindowUtil.StartNameEditingIfProjectWindowExists(0, ScriptableObject.CreateInstance<CreateScriptAsset>(),
            GetSelectedPathOrFallback() + "/NewMonoSingleScript.cs",
            null, MonoSingleScriptPath);
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
        public static string ScriptName = "";
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
            text = Regex.Replace(text, ScriptName, fileNameWithoutExtension);
            //把内容重新写入脚本
            bool encoderShouldEmitUTF8Identifier = false;
            bool throwOnInvalidBytes = false;
            UTF8Encoding encoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier, throwOnInvalidBytes);
            bool append = false;
            StreamWriter _sw = new StreamWriter(fullPath, append, encoding);

            _sw.WriteLine("/* ");
            _sw.WriteLine(" * ================================================");
            _sw.WriteLine(" * Describe:      This script is used to .");
            _sw.WriteLine(" * Author:        " + ProjectUtility.Project.ScriptAuthor);
            _sw.WriteLine(" * CreationTime:  " + System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
            _sw.WriteLine(" * ModifyAuthor:  " + ProjectUtility.Project.ScriptAuthor);
            _sw.WriteLine(" * ModifyTime:    " + System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
            _sw.WriteLine(" * ScriptVersion: " + ProjectUtility.Project.ScriptVersion);
            _sw.WriteLine(" * ===============================================");
            _sw.WriteLine("*/");

            text = text.Replace("PleaseChangeTheNamespace", EditorUtils.LoadSettingAtPath<AutoBind.AutoBindSetting>().Namespace);
            _sw.Write(text);
            _sw.Close();
            AssetDatabase.ImportAsset(newScriptPath);
            return AssetDatabase.LoadAssetAtPath(newScriptPath, typeof(Object));
        }
    }
}