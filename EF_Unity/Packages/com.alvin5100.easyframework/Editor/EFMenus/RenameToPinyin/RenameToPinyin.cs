/*
 * ================================================
 * Describe:      This script is used to .
 * Author:        Xiaohei.Wang(Wenhao)
 * CreationTime:  2024-10-18 14:25:00
 * ModifyAuthor:  Xiaohei.Wang(Wenhao)
 * ModifyTime:    2024-10-18 14:25:00
 * ScriptVersion: 0.1
 * ===============================================
*/

using NPinyin;
using UnityEditor;
using UnityEngine;

namespace EasyFramework.Edit
{
    namespace AssetToolLibrary
    {
        /// <summary>
        /// Quickly change Chinese to Pinyin
        /// </summary>
        public class RenameToPinyin
        {
            [MenuItem("Assets/EF/Rename To Pinyin", false, 101)]
            static void RenameAll()
            {
                Object[] selection = Selection.GetFiltered<Object>(SelectionMode.DeepAssets);

                int count = selection.Length;
                for (int i = 0; i < count; i++)
                {
                    Object obj = selection[i];
                    if (obj.name.IsChinese())
                    {
                        string path = AssetDatabase.GetAssetPath(obj);

                        string pinyin = Pinyin.GetPinyin(obj.name);
                        pinyin = pinyin.Replace(" ", "");

                        AssetDatabase.RenameAsset(path, pinyin);
                    }
                }
                AssetDatabase.Refresh();
            }
        }
    }
}