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
                Object[] _selection = Selection.GetFiltered<Object>(SelectionMode.DeepAssets);

                int _count = _selection.Length;
                for (int i = 0; i < _count; i++)
                {
                    Object _obj = _selection[i];
                    if (_obj.name.IsChinese())
                    {
                        string _path = AssetDatabase.GetAssetPath(_obj);

                        string _pinyin = Pinyin.GetPinyin(_obj.name);
                        _pinyin = _pinyin.Replace(" ", "");

                        AssetDatabase.RenameAsset(_path, _pinyin);
                    }
                }
                AssetDatabase.Refresh();
            }
        }
    }
}