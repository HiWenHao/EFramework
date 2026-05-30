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
using Object = UnityEngine.Object;

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

        [MenuItem("Assets/Create/EF/C# Scripts/EF", false, 21)]
        // ReSharper disable once InconsistentNaming
        public static void CreateEFLauncherScript() => Create("EF", "EF_Icon");

        private static void Create(string scriptName, string scriptIconName = "")
        {
            CreateScriptAsset.ScriptName = scriptName;
            CreateScriptAsset.ScriptIconName = scriptIconName;
            ProjectWindowUtil.StartNameEditingIfProjectWindowExists(0,
                ScriptableObject.CreateInstance<CreateScriptAsset>(),
                GetSelectedPathOrFallback() + "/" + scriptName + ".cs",
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

        private class CreateScriptAsset : EndNameEditAction
        {
            public static string ScriptName = "";
            public static string ScriptIconName = "";

            public override void Action(int instanceId, string newScriptPath, string templatePath)
            {
                Object obj = CreateTemplateScriptAsset(newScriptPath, templatePath);
                ProjectWindowUtil.ShowCreatedAsset(obj);
            }

            private static Object CreateTemplateScriptAsset(string newScriptPath, string templatePath)
            {
                string fullPath = Path.GetFullPath(newScriptPath);
                StreamReader streamReader = new StreamReader(templatePath);
                string text = streamReader.ReadToEnd();
                streamReader.Close();
                string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(newScriptPath);

                //替换模板的文件名
                text = Regex.Replace(text, ScriptName, fileNameWithoutExtension);
                UTF8Encoding encoding = new UTF8Encoding(true, false);
                StreamWriter sw = new StreamWriter(fullPath, false, encoding);

                sw.WriteLine(EditorToolkit.GetFileHead("This script is used to ."));
                sw.WriteLine();

                text = text.Replace("PleaseChangeTheNamespace", ConfigManager.Project.ScriptNamespace);
                sw.Write(text);
                sw.Close();
                AssetDatabase.ImportAsset(newScriptPath);
                Object obj = AssetDatabase.LoadAssetAtPath(newScriptPath, typeof(Object));

                if (string.IsNullOrEmpty(ScriptIconName)) return obj;

                string iconAssetPath = Utility.Path.GetEfAssetsPath() + $"/Gizmos/{ScriptIconName}.png";
                string iconGuid = AssetDatabase.AssetPathToGUID(iconAssetPath);
                if (string.IsNullOrEmpty(iconGuid)) return obj;

                string metaFullPath = fullPath + ".meta";
                string metaContent = File.ReadAllText(metaFullPath);
                string iconLine = $"  icon: {{fileID: 2800000, guid: {iconGuid}, type: 3}}";

                metaContent = metaContent.Contains("icon:")
                    ? Regex.Replace(metaContent, @"  icon:.*", iconLine)
                    : Regex.Replace(metaContent, @"(MonoImporter:\s*\n)", "$1" + iconLine + "\n");

                File.WriteAllText(metaFullPath, metaContent);
                AssetDatabase.ImportAsset(newScriptPath);
                obj = AssetDatabase.LoadAssetAtPath(newScriptPath, typeof(Object));

                return obj;
            }
        }
    }
}