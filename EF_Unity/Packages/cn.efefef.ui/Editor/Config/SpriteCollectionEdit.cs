/*
 * ================================================
 * Describe:      This script is used to edit the sprite collect.
 * Author:        Xiaohei.Wang(Wenhao)
 * CreationTime:  2023-04-01 16:05:44
 * ModifyAuthor:  Xiaohei.Wang(Wenhao)
 * ModifyTime:    2023-04-01 16:05:44
 * ScriptVersion: 0.1
 * ===============================================
 */

using System.IO;
using UnityEditor;
using UnityEditor.U2D;
using UnityEngine;
using UnityEngine.U2D;
using Application = UnityEngine.Application;

namespace EasyFramework.Edit.SpriteTools
{
    /// <summary>
    /// The sprite collect editer
    /// </summary>
    [CustomEditor(typeof(SpriteCollection))]
    public class SpriteCollectionEdit : Editor
    {
        bool _atlas;
        bool _allOverwrite;
        string _frameworkAtlasFolder;
        SpriteCollection _target;
        SerializedProperty _atlasFolder;
        SerializedProperty _targetObjects;
        private void OnEnable()
        {
            _target = (SpriteCollection)target;
            _atlasFolder = serializedObject.FindProperty("_atlasFolder");
            _targetObjects = serializedObject.FindProperty("_objects");
            _frameworkAtlasFolder = ConfigManager.Path.AtlasFolder;

            _allOverwrite = true;
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.PropertyField(_targetObjects, true);

            ShowObjectsAndAtlas();

            if (GUILayout.Button(LC.Combine(Lc.Create, Lc.Atlas) + " (Repack)", GUILayout.Width(130f)))
            {
                Pack();
            }

            EditorGUILayout.Space(EditorGUIUtility.singleLineHeight);

            SelectPath();

            CrateTheAtlas();

            if (!GUI.changed)
            {
                return;
            }
            serializedObject.ApplyModifiedProperties();
        }

        #region Select Path
        void SelectPath()
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(LC.Combine(Lc.Atlas, Lc.Path), GUILayout.Width(130f));
            EditorGUILayout.LabelField(new GUIContent(_atlasFolder.stringValue));
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            {
                if (GUILayout.Button(LC.Combine(Lc.Select, Lc.Path)))
                {
                    string path = EditorUtility.OpenFolderPanel(LC.Combine(Lc.Select, Lc.Path), Application.dataPath, "");
                    if (string.IsNullOrEmpty(path))
                    {
                        if (string.IsNullOrEmpty(_atlasFolder.stringValue))
                            _atlasFolder.stringValue = _frameworkAtlasFolder;
                    }
                    else
                        _atlasFolder.stringValue = Utility.Path.GetPathInAssetsFolder(path) + "/";
                }
                if (GUILayout.Button(LC.Combine(Lc.Default, Lc.Path)))
                {
                    _atlasFolder.stringValue = _frameworkAtlasFolder;
                }
            }
            EditorGUILayout.EndHorizontal();
        }
        #endregion

        #region Show Atlas
        void ShowObjectsAndAtlas()
        {
            _atlas = EditorGUILayout.Foldout(_atlas, LC.Combine(Lc.Atlas) + $"  ({_target.Atlas.Count})");
            if (_atlas)
            {
                EditorGUILayout.Space(EditorGUIUtility.singleLineHeight);
                for (int i = 0; i < _target.Atlas.Count; i++)
                {
                    EditorGUILayout.BeginHorizontal();
                    if (_target.Atlas[i])
                    {
                        EditorGUILayout.LabelField(_target.Atlas[i].name, GUILayout.MinWidth(30f), GUILayout.MaxWidth(150f));
                        EditorGUILayout.ObjectField(_target.Atlas[i], typeof(Sprite), false);

                        if (GUILayout.Button("X"))
                        {
                            if (EditorUtility.DisplayDialog(LC.Combine(Lc.Delete), LC.Combine(Lc.Confirm, Lc.Delete) + _target.Atlas[i].name + LC.Combine(Lc.Atlas), LC.Combine(Lc.Ok)))
                            {
                                AssetDatabase.DeleteAsset(AssetDatabase.GetAssetPath(_target.Atlas[i]));
                                ClearAtlasWithIndex(i);
                                EditorUtility.SetDirty(_target);
                                serializedObject.Update();
                                EditorApplication.delayCall += AssetDatabase.Refresh;
                            }
                        }
                    }
                    EditorGUILayout.EndHorizontal();
                }
            }

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button(LC.Combine(Lc.Clear, Lc.All)))
            {
                ClearAllAtlas();
                EditorUtility.SetDirty(_target);
                serializedObject.Update();
                EditorApplication.delayCall += AssetDatabase.Refresh;
            }

            if (GUILayout.Button(LC.Combine(Lc.Delete, Lc.All)))
            {
                for (int i = _target.Atlas.Count - 1; i >= 0; i--)
                    AssetDatabase.DeleteAsset(AssetDatabase.GetAssetPath(_target.Atlas[i]));
                ClearAllAtlas();
                EditorUtility.SetDirty(_target);
                serializedObject.Update();
                EditorApplication.delayCall += AssetDatabase.Refresh;
            }
            EditorGUILayout.EndHorizontal();
        }
        #endregion

        #region Crate Atlas
        void CrateTheAtlas()
        {
            EditorGUILayout.BeginHorizontal();
            _allOverwrite = EditorGUILayout.ToggleLeft(LC.Combine(Lc.Overwrite), _allOverwrite, GUILayout.MaxWidth(100f));
            if (GUILayout.Button(LC.Combine(Lc.Create, Lc.Atlas)))
            {
                CreateAtlas();
            }
            EditorGUILayout.EndHorizontal();
        }
        void CreateAtlas()
        {
            ClearAllAtlas();
            if (string.IsNullOrEmpty(_atlasFolder.stringValue))
            {
                EditorUtility.DisplayDialog(LC.Combine(Lc.Hints), LC.Combine(Lc.Select, Lc.Atlas, Lc.Folder), LC.Combine(Lc.Ok));
                return;
            }

            if (_target.TargetObjects.Find(obj => obj is SpriteAtlas) != null)
            {
                EditorUtility.DisplayDialog(LC.Combine(Lc.Hints), LC.Combine(Lc.SC_AtlasExistInCollection), LC.Combine(Lc.Ok));
                return;
            }

            // 确保图集输出目录存在
            string atlasDir = _atlasFolder.stringValue.TrimEnd('/');
            if (!Directory.Exists(atlasDir))
            {
                Directory.CreateDirectory(atlasDir);
                AssetDatabase.Refresh();
            }

            //创建图集
            SpriteAtlasTextureSettings textureSet = new SpriteAtlasTextureSettings
            {
                readable = false,
                generateMipMaps = false,
                sRGB = true,
                filterMode = FilterMode.Bilinear,
            };
            SpriteAtlasPackingSettings packSet = new SpriteAtlasPackingSettings
            {
                blockOffset = 1,
                enableRotation = false,
                enableTightPacking = false,
                enableAlphaDilation = true,
                padding = 8,
            };
            for (int i = 0; i < _target.TargetObjects.Count; i++)
            {
                string assetPath = Utility.Path.GetRegularPath(Path.Combine(_atlasFolder.stringValue, _target.TargetObjects[i].name + "Atlas.spriteatlas"));
                bool result = false;
                if (!_allOverwrite && File.Exists(assetPath))
                {
                    result = EditorUtility.DisplayDialog(LC.Combine(Lc.Hints), _target.TargetObjects[i].name + LC.Combine(Lc.SC_AtlasExistAlsoOverwrite), LC.Combine(Lc.Ok), LC.Combine(Lc.Cancel));
                    if (!result)
                    {
                        bool hasSa = false;

                        for (int j = 0; j < _target.Atlas.Count; j++)
                        {
                            if (_target.Atlas[j].name == _target.TargetObjects[i].name)
                            {
                                hasSa = true;
                            }
                        }
                        if (!hasSa)
                        {
                            SpriteAtlas sat = AssetDatabase.LoadAssetAtPath<SpriteAtlas>(assetPath);
                            if (i < _target.Atlas.Count)
                                _target.Atlas.Insert(i, sat);
                            else
                                _target.Atlas.Add(sat);
                        }
                        continue;
                    }
                }

                SpriteAtlas sa = new SpriteAtlas();
                sa.SetPackingSettings(packSet);
                sa.SetTextureSettings(textureSet);

                AssetDatabase.CreateAsset(sa, assetPath);

                sa.Add(new[] { _target.TargetObjects[i] });
                if (result && i < _target.Atlas.Count && _target.Atlas[i].name != _target.TargetObjects[i].name)
                {
                    _target.Atlas[i] = sa;
                }
                else
                {
                    _target.Atlas.Add(sa);
                }
                sa = null;
                AssetDatabase.SaveAssets();
            }

            // 创建完成后自动烘焙
            if (_target.Atlas.Count > 0)
            {
                SpriteAtlasUtility.PackAtlases(_target.Atlas.ToArray(), BuildTarget.NoTarget);
            }

            EditorUtility.SetDirty(_target);
            serializedObject.Update();
            AssetDatabase.SaveAssets();
            EditorApplication.delayCall += AssetDatabase.Refresh;
        }
        #endregion

        #region Pack Atlas
        public void Pack()
        {
            if (_target.Atlas.Count == 0)
            {
                EditorUtility.DisplayDialog(LC.Combine(Lc.Hints), "No atlas to pack.", LC.Combine(Lc.Ok));
                return;
            }

            SpriteAtlasUtility.PackAtlases(_target.Atlas.ToArray(), BuildTarget.NoTarget);
            EditorUtility.SetDirty(_target);
            serializedObject.Update();
            AssetDatabase.SaveAssets();
            EditorApplication.delayCall += AssetDatabase.Refresh;
        }
        #endregion

        #region Common Tools
        #region Atlas
        void ClearAtlasWithIndex(int index)
        {
            _target.Atlas.RemoveAt(index);
        }

        void ClearAllAtlas()
        {
            for (int i = _target.Atlas.Count - 1; i >= 0; i--)
            {
                ClearAtlasWithIndex(i);
            }
        }
        #endregion
        #endregion
    }
}
