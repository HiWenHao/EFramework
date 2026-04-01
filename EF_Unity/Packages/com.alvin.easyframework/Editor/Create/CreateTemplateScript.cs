/*
 * ================================================
 * Describe:      This script is used to create template script.
 * Author:        Xiaohei.Wang(Wenhao)
 * CreationTime:  2022-06-07 18:18:36
 * ModifyAuthor:  Alvin5100
 * ModifyTime:    2026-04-01 15:01:35
 * ScriptVersion: 0.1
 * ===============================================
 */

using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.ProjectWindowCallback;
using UnityEngine;

namespace EasyFramework.Edit.Create
{
    internal static class CreateTemplateScript
    {
        //菜单项
        [MenuItem("Assets/Create/EF/C# Scripts/TemplateScript", false, 1)]
        public static void CreateScript() => Create("TemplateScript");

        [MenuItem("Assets/Create/EF/C# Scripts/SingleScript", false, 2)]
        public static void CreateSingleScript() => Create("SingleScript");

        [MenuItem("Assets/Create/EF/C# Scripts/MonoSingleScript", false, 3)]
        public static void CreateMonoSingleScript() => Create("MonoSingleScript");

        [MenuItem("Assets/Create/EF/C# Scripts/GameLauncherScript", false, 20)]
        public static void CreateGameLauncherScript() => Create("GameLauncher");

        private static void Create(string scriptName)
        {
            CreateScriptAsset.ScriptName = scriptName;
            ProjectWindowUtil.StartNameEditingIfProjectWindowExists(0,
                ScriptableObject.CreateInstance<CreateScriptAsset>(),
                Path.Combine(GetSelectedPathOrFallback(), $"\\{scriptName}.cs"),
                null, Path.Combine(Utility.Path.GetEfAssetsPath(), $"ScriptTemplate/{scriptName}.cs.txt"));
        }

        private static string GetSelectedPathOrFallback()
        {
            string path = "Assets";
            foreach (Object obj in Selection.GetFiltered(typeof(Object), SelectionMode.Assets))
            {
                path = AssetDatabase.GetAssetPath(obj);
                if (string.IsNullOrEmpty(path) || !File.Exists(path))
                    continue;

                path = Path.GetDirectoryName(path);
                break;
            }

            return path;
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
                string authorName =
                    EditorPrefs.GetString($"{ProjectUtility.Project.AppConst.AppPrefix}EditorUser");
                authorName = string.IsNullOrEmpty(ProjectUtility.Project.ScriptAuthor)
                    ? authorName
                    : ProjectUtility.Project.ScriptAuthor;
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
                StreamWriter sw = new StreamWriter(fullPath, append, encoding);

                sw.WriteLine("/*");
                sw.WriteLine(" * ================================================");
                sw.WriteLine(" * Describe:      This script is used to .");
                sw.WriteLine(" * Author:        " + authorName);
                sw.WriteLine(" * CreationTime:  " + System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
                sw.WriteLine(" * ModifyAuthor:  " + authorName);
                sw.WriteLine(" * ModifyTime:    " + System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
                sw.WriteLine(" * ScriptVersion: " + ProjectUtility.Project.ScriptVersion);
                sw.WriteLine(" * ===============================================");
                sw.WriteLine("*/");

                text = text.Replace("PleaseChangeTheNamespace",
                    EditorUtils.LoadSettingAtPath<AutoBind.AutoBindSetting>().Namespace);
                sw.Write(text);
                sw.Close();
                AssetDatabase.ImportAsset(newScriptPath);
                return AssetDatabase.LoadAssetAtPath(newScriptPath, typeof(Object));
            }
        }
    }
}
