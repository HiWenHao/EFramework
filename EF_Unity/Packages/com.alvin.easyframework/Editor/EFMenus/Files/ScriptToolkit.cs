/*
 * ================================================
 * Describe:      This script is used to change the script encoding format. Adapted from, thanks to the original author
 * Author:        Xiaohei.Wang(Wenhao)
 * CreationTime:  2024-05-22 14:47:02
 * ModifyAuthor:  Alvin5100
 * ModifyTime:    2026-03-31 14:20:52
 * ScriptVersion: 0.1
 * ===============================================
*/

using UnityEngine;
using UnityEditor;
using System.IO;
using System.Text;

namespace EasyFramework.Edit
{
    namespace AssetToolLibrary
    {
        /// <summary>
        /// 脚本工具
        /// </summary>
        public static class ScriptToolkit
        {
            /// <summary>
            /// Change the script encoding format
            /// </summary>
            [MenuItem("Assets/EF/Script/Format to UTF-8", false, 100)]
            private static void ChangeScriptEncoding()
            {
                string assetPath = EditorUtils.GetSelectFilePath();

                if (!Path.GetExtension(assetPath).ToLower().Equals(".cs"))
                {
                    D.Warning("Please select a file of type c#, extension is '.cs'\t请选择一个类型为c#的文件，扩展名为 '.cs'");
                    return;
                }

                string absPath = Path.Combine(Path.GetDirectoryName(Application.dataPath), assetPath);

                string contents = File.ReadAllText(absPath, Encoding.GetEncoding("GB2312"));
                File.WriteAllText(absPath, contents, Encoding.UTF8);

                AssetDatabase.Refresh();
            }
            
            /// <summary>
            /// Update the script modify information
            /// </summary>
            [MenuItem("Assets/EF/Script/Update Modify Information", false, 101)]
            private static void UpdateScriptModify()
            {
                foreach (string assetPath in EditorUtils.GetSelectFilesPath())
                {
                    if (!Path.GetExtension(assetPath).ToLower().Equals(".cs"))
                    {
                        D.Warning("Please select a file of type c#, extension is '.cs'\t请选择一个类型为c#的文件，扩展名为 '.cs'");
                        continue;
                    }

                    string absPath = Path.Combine(Path.GetDirectoryName(Application.dataPath), assetPath);
                    string[] contents = File.ReadAllLines(absPath);

                    if (contents.Length < 10)
                        continue;

                    string authorName =
                        EditorPrefs.GetString($"{ProjectUtility.Project.AppConst.AppPrefix}EditorUser");
                    authorName = string.IsNullOrEmpty(ProjectUtility.Project.ScriptAuthor)
                        ? authorName
                        : ProjectUtility.Project.ScriptAuthor;
                    if (!contents[5].Contains("ModifyAuthor:"))
                        continue;
                    contents[5] = $" * ModifyAuthor:  {authorName}";
                    contents[6] = $" * ModifyTime:    {System.DateTime.Now:yyyy-MM-dd HH:mm:ss}";
                    contents[7] = $" * ScriptVersion: {ProjectUtility.Project.ScriptVersion}";
                
                    File.WriteAllLines(absPath, contents, Encoding.UTF8);
                }
                
                AssetDatabase.Refresh();
            }
        }
    }
}
