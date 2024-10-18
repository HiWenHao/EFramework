/*
 * ================================================
 * Describe:      This script is used to change the script encoding format. Adapted from, thanks to the original author
 * Author:        Xiaohei.Wang(Wenhao)
 * CreationTime:  2024-05-22 14:47:02
 * ModifyAuthor:  Xiaohei.Wang(Wenhao)
 * ModifyTime:    2024-05-22 14:47:02
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
        /// Change the script encoding format
        /// </summary>
        public class ChangeScriptEncodingFormat : MonoBehaviour
        {
            [MenuItem("Assets/EF/Script format to UTF-8", false, 100)]
            private static void ChangeScriptEncoding()
            {
                Object _selectedObject = Selection.activeObject;

                if (_selectedObject == null)
                    return;

                string _assetPath = AssetDatabase.GetAssetPath(_selectedObject);

                if (!Path.GetExtension(_assetPath).ToLower().Equals(".cs"))
                {
                    D.Warning("Please select a file of type c#, extension is '.cs'\t请选择一个类型为c#的文件，扩展名为 '.cs'");
                    return;
                }

                string _absPath = Path.Combine(Path.GetDirectoryName(Application.dataPath), _assetPath);

                string _contents = File.ReadAllText(_absPath, Encoding.GetEncoding("GB2312"));
                File.WriteAllText(_absPath, _contents, Encoding.UTF8);

                AssetDatabase.Refresh();
            }
        }
    }
}