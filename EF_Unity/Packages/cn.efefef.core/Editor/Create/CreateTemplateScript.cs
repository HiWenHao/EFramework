/*
 * ================================================
 * Describe:        This script is used to create template script.
 * Author:          Xiaohei.Wang(Wenhao)
 * CreationTime:    2022-06-07 18:18:36
 * ModifyAuthor:    Alvin8412
 * ModifyTime:      2026-06-01 15:33:16
 * ScriptVersion:   0.1
 * ================================================
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

        /// <summary>
        /// 创建模板脚本 — 使用实例字段传递上下文，避免静态字段重入竞态
        /// <para>Create template script — uses instance fields for context, avoiding static-field re-entrancy race</para>
        /// </summary>
        private static void Create(string scriptName, string scriptIconName = "")
        {
            var instance = ScriptableObject.CreateInstance<CreateScriptAsset>();
            instance.ScriptName = scriptName;
            instance.ScriptIconName = scriptIconName;
            ProjectWindowUtil.StartNameEditingIfProjectWindowExists(0,
                instance,
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

        /// <summary>
        /// 脚本创建回调（实例级字段，避免多次创建时的静态状态污染）
        /// <para>Script creation callback — instance-level fields to avoid static state pollution between concurrent creations</para>
        /// </summary>
        private class CreateScriptAsset : EndNameEditAction
        {
            public string ScriptName = "";
            public string ScriptIconName = "";

            public override void Action(int instanceId, string newScriptPath, string templatePath)
            {
                Object obj = CreateTemplateScriptAsset(newScriptPath, templatePath);
                ProjectWindowUtil.ShowCreatedAsset(obj);
            }

            private Object CreateTemplateScriptAsset(string newScriptPath, string templatePath)
            {
                try
                {
                    string fullPath = Path.GetFullPath(newScriptPath);

                    // 读取模板内容
                    string text;
                    using (var reader = new StreamReader(templatePath))
                    {
                        text = reader.ReadToEnd();
                    }

                    string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(newScriptPath);

                    text = Regex.Replace(text, Regex.Escape(ScriptName), fileNameWithoutExtension);

                    var encoding = new UTF8Encoding(true, false);
                    using (var writer = new StreamWriter(fullPath, false, encoding))
                    {
                        writer.WriteLine(EditorInfoToolkit.GetFileHead("This script is used to ."));
                        writer.WriteLine();
                        text = text.Replace("PleaseChangeTheNamespace",
                            ConfigManager.Project?.ScriptNamespace ?? "PleaseChangeTheNamespace");
                        writer.Write(text);
                    }

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
                catch (System.Exception ex)
                {
                    D.Error($"[ CreateTemplateScript ] Failed to create script from template '{templatePath}': {ex.Message}");
                    return null;
                }
            }
        }
    }
}
