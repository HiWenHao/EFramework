/* 
 * ================================================
 * Describe:      This script is used to copy the file info.
 * Author:        Xiaohei.Wang(Wenhao)
 * CreationTime:  2023-04-24 20:55:56
 * ModifyAuthor:  Xiaohei.Wang(Wenhao)
 * ModifyTime:    2023-04-24 20:55:56
 * ScriptVersion: 0.1
 * ===============================================
*/
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace EasyFramework.Edit
{
    namespace AssetToolLibrary
    {
        /// <summary>
        /// Copy the file info
        /// </summary>
        public class CopyFileInfos
        {
            [MenuItem("Assets/EF/Copy Infos/Copy GUID", false, 1)]
            private static void CopySelectedGuid()
            {
                List<string> guids = new List<string>(Selection.objects.Length);

                foreach (var obj in Selection.objects)
                {
                    if (!AssetDatabase.TryGetGUIDAndLocalFileIdentifier(obj, out string guid, out long localId))
                        continue;

                    if (AssetDatabase.IsMainAsset(obj))
                    {
                        guids.Add(guid);
                    }
                    else
                    {
                        guids.Add($"{guid}-{localId}");
                    }
                }

                var te = new TextEditor()
                {
                    text = string.Join("\n", guids)
                };
                te.SelectAll();
                te.Copy();
            }

            [MenuItem("Assets/EF/Copy Infos/Relative Path", false, 2)]
            private static void CopyFileAssetsPath()
            {
                var assetNames = Selection.assetGUIDs.Select(AssetDatabase.GUIDToAssetPath);

                var te = new TextEditor()
                {
                    text = string.Join("\n", assetNames.Distinct())
                };
                te.SelectAll();
                te.Copy();
            }

            [MenuItem("Assets/EF/Copy Infos/Absolute Path", false, 3)]
            private static void CopyFileDiskPath()
            {
                var projectRoot = Path.GetDirectoryName(Application.dataPath);

                var assetNames = Selection.assetGUIDs
                    .Select(AssetDatabase.GUIDToAssetPath)
                    .Select(path => path.Replace('/', Path.DirectorySeparatorChar))
                    .Select(path => projectRoot + Path.DirectorySeparatorChar + path)
                    ;

                var te = new TextEditor()
                {
                    text = string.Join("\n", assetNames.Distinct())
                };
                te.SelectAll();
                te.Copy();
            }
        }
    }
}