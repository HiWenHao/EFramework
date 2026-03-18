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
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
        List<bool> _hasPreview;

        SpriteCollection _target;
        SerializedProperty _atlasFolder;
        SerializedProperty _targetObjects;
        private void OnEnable()
        {
            _target = (SpriteCollection)target;
            _atlasFolder = serializedObject.FindProperty("_atlasFolder");
            _targetObjects = serializedObject.FindProperty("_objects");
            _frameworkAtlasFolder = ProjectUtility.Path.AtlasFolder;

            _allOverwrite = true;
            _hasPreview = new List<bool>();
            for (int i = _target.Atlas.Count - 1; i >= 0; i--)
            {
                _hasPreview.Add(false);
            }
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.PropertyField(_targetObjects, true);

            ShowObjectsAndAtlas();

            ShowSprites();

            if (GUILayout.Button(LC.Combine(Lc.Overview), GUILayout.Width(100f)))
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
            EditorGUILayout.LabelField(LC.Combine(new Lc[] { Lc.Atlas, Lc.Path }), GUILayout.Width(130f));
            EditorGUILayout.LabelField(new GUIContent(_atlasFolder.stringValue));
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            {
                if (GUILayout.Button(LC.Combine(new Lc[] { Lc.Select, Lc.Path })))
                {
                    string _path = EditorUtility.OpenFolderPanel(LC.Combine(new Lc[] { Lc.Select, Lc.Path }), Application.dataPath, "");
                    if (string.IsNullOrEmpty(_path))
                    {
                        if (string.IsNullOrEmpty(_atlasFolder.stringValue))
                            _atlasFolder.stringValue = _frameworkAtlasFolder;
                    }
                    else
                        _atlasFolder.stringValue = Utility.AssetPath.GetPathInAssetsFolder(_path) + "/";
                }
                if (GUILayout.Button(LC.Combine(new Lc[] { Lc.Default, Lc.Path })))
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
            _atlas = EditorGUILayout.Foldout(_atlas, LC.Combine(Lc.Atlas) + $"\t({_target.Atlas.Count})");
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
                            if (EditorUtility.DisplayDialog(LC.Combine(Lc.Delete), LC.Combine(new Lc[] { Lc.Confirm, Lc.Delete }) + _target.Atlas[i].name + LC.Combine(Lc.Atlas), LC.Combine(Lc.Ok)))
                            {
                                AssetDatabase.DeleteAsset(AssetDatabase.GetAssetPath(_target.Atlas[i]));
                                ClearSpriteInfoWithIndex(i);
                                ClearAtlasWithIndex(i);
                                AssetDatabase.Refresh();
                            }
                        }
                    }
                    EditorGUILayout.EndHorizontal();
                }
            }

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button(LC.Combine(new Lc[] { Lc.Clear, Lc.All })))
            {
                ClearAllAtlas();
                ClearSpriteInfos();
                AssetDatabase.Refresh();
            }

            if (GUILayout.Button(LC.Combine(new Lc[] { Lc.Delete, Lc.All })))
            {
                for (int i = _target.Atlas.Count - 1; i >= 0; i--)
                    AssetDatabase.DeleteAsset(AssetDatabase.GetAssetPath(_target.Atlas[i]));
                ClearAllAtlas();
                ClearSpriteInfos();
                AssetDatabase.Refresh();
            }
            EditorGUILayout.EndHorizontal();
        }
        #endregion

        #region Show Sprites
        void ShowSprites()
        {
            EditorGUILayout.Space(EditorGUIUtility.singleLineHeight);
            for (int i = 0; i < _target.SpriteInfos.Count; i++)
            {
                if (i == _hasPreview.Count)
                    _hasPreview.Add(false);

                _hasPreview[i] = EditorGUILayout.Foldout(_hasPreview[i], new GUIContent(_target.SpriteInfos[i].SpriteList.Count + "\t" + LC.Combine(Lc.Atlas) + " - " + _target.SpriteInfos[i].FolderName));
                if (_hasPreview[i] && i < _target.SpriteInfos.Count)
                {
                    EditorGUILayout.BeginVertical(EditorStyles.helpBox);

                    for (int j = 0; j < _target.SpriteInfos[i].PathList.Count; j++)
                    {
                        EditorGUILayout.BeginHorizontal();
                        EditorGUILayout.TextField(new GUIContent($"{_target.SpriteInfos[i].SpriteList[j].name}", _target.SpriteInfos[i].PathList[j]), _target.SpriteInfos[i].PathList[j]);
                        EditorGUILayout.ObjectField(_target.SpriteInfos[i].SpriteList[j], typeof(Sprite), false);
                        EditorGUILayout.EndHorizontal();
                    }

                    EditorGUILayout.EndVertical();
                }
            }
            EditorGUILayout.Space(EditorGUIUtility.singleLineHeight);
        }
        #endregion

        #region Crate Atlas
        void CrateTheAtlas()
        {
            EditorGUILayout.BeginHorizontal();
            _allOverwrite = EditorGUILayout.ToggleLeft(LC.Combine(Lc.Overwrite), _allOverwrite, GUILayout.MaxWidth(100f));
            if (GUILayout.Button(LC.Combine(new Lc[] { Lc.Create, Lc.Atlas })))
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
                EditorUtility.DisplayDialog(LC.Combine(Lc.Hints), LC.Combine(new Lc[] { Lc.Select, Lc.Atlas, Lc.Folder }), LC.Combine(Lc.Ok));
                return;
            }

            if (_target.TargetObjects.Find(_ => _ is SpriteAtlas) != null)
            {
                EditorUtility.DisplayDialog(LC.Combine(Lc.Hints), LC.Combine(Lc.SC_AtlasExistInCollection), LC.Combine(Lc.Ok));
                return;
            }

            //创建图集
            SpriteAtlasTextureSettings _textureSet = new SpriteAtlasTextureSettings()
            {
                readable = false,
                generateMipMaps = false,
                sRGB = true,
                filterMode = FilterMode.Bilinear,
            };
            SpriteAtlasPackingSettings _packSet = new SpriteAtlasPackingSettings()
            {
                blockOffset = 1,
                enableRotation = false,
                enableTightPacking = false,
                padding = 8,
            };
            for (int i = 0; i < _target.TargetObjects.Count; i++)
            {
                string _atlas = Utility.Path.GetRegularPath(Path.Combine(_atlasFolder.stringValue, _target.TargetObjects[i].name + ".spriteatlas"));
                bool _result = false;
                if (!_allOverwrite && File.Exists(_atlas))
                {
                    _result = EditorUtility.DisplayDialog(LC.Combine(Lc.Hints), _target.TargetObjects[i].name + LC.Combine(Lc.SC_AtlasExistAlsoOverwrite), LC.Combine(Lc.Ok), LC.Combine(Lc.Cancel));
                    if (!_result)
                    {
                        bool _hasSA = false;

                        for (int j = 0; j < _target.Atlas.Count; j++)
                        {
                            if (_target.Atlas[j].name == _target.TargetObjects[i].name)
                            {
                                _hasSA = true;
                                continue;
                            }
                        }
                        if (!_hasSA)
                        {
                            SpriteAtlas _sat = AssetDatabase.LoadAssetAtPath<SpriteAtlas>(_atlas);
                            if (i < _target.Atlas.Count)
                            {
                                SpriteAtlas _tempSA = _target.Atlas[i];
                                _target.Atlas.Add(_tempSA);
                                _target.Atlas[i] = _sat;
                                _tempSA = null;
                            }
                            else
                                _target.Atlas.Add(_sat);
                            _sat = null;
                        }
                        continue;
                    }
                }

                SpriteAtlas _sa = new SpriteAtlas();
                _sa.SetPackingSettings(_packSet);
                _sa.SetTextureSettings(_textureSet);

                AssetDatabase.CreateAsset(_sa, _atlas);

                _sa.Add(new Object[] { _target.TargetObjects[i] });
                if (_result && i < _target.Atlas.Count && _target.Atlas[i].name != _target.TargetObjects[i].name)
                {
                    _target.Atlas[i] = _sa;
                }
                else
                {
                    _target.Atlas.Add(_sa);
                }
                _sa = null;
                AssetDatabase.SaveAssets();
            }
            AssetDatabase.Refresh();
        }
        #endregion

        #region Pack Preview
        public void Pack()
        {
            ClearSpriteInfos();
            for (int i = 0; i < _target.Atlas.Count; i++)
            {
                for (int j = _target.TargetObjects.Count - 1; j >= 0; j--)
                {
                    if (_target.Atlas[i].name == _target.TargetObjects[j].name)
                    {
                        Object obj = _target.TargetObjects[j];
                        _target.SpriteInfos.Add(new SpriteCollection.SpriteInfo()
                        {
                            FolderName = obj.name,
                        });
                        HandlePackable(i, obj);
                        _hasPreview.Add(false);
                        continue;
                    }
                }
            }

            SpriteAtlasUtility.PackAtlases(_target.Atlas.ToArray(), BuildTarget.NoTarget);
            EditorUtility.SetDirty(this);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        void HandlePackable(int index, Object obj)
        {
            string _path = AssetDatabase.GetAssetPath(obj);
            if (obj is Sprite sp)
            {
                Object[] _objects = AssetDatabase.LoadAllAssetsAtPath(_path);
                if (_objects.Length == 2)
                {
                    _target.SpriteInfos[index].PathList.Add(_path);
                }
                else
                {
                    string _regularPath = Utility.Path.GetRegularPath(Path.Combine(_path, sp.name));
                    _target.SpriteInfos[index].PathList.Add(_regularPath);
                }
                _target.SpriteInfos[index].SpriteList.Add(sp);
            }
            else if (obj is Texture2D)
            {
                Object[] objects = AssetDatabase.LoadAllAssetsAtPath(_path);
                if (objects.Length == 2)
                {
                    _target.SpriteInfos[index].PathList.Add(_path);
                    _target.SpriteInfos[index].SpriteList.Add(GetSprites(objects)[0]);
                }
                else
                {
                    Sprite[] sprites = GetSprites(objects);
                    for (int j = 0; j < sprites.Length; j++)
                    {
                        string regularPath = Utility.Path.GetRegularPath(Path.Combine(_path, sprites[j].name));
                        _target.SpriteInfos[index].PathList.Add(regularPath);
                        _target.SpriteInfos[index].SpriteList.Add(sprites[j]);
                    }
                }
            }
            else if (obj is DefaultAsset && ProjectWindowUtil.IsFolder(obj.GetInstanceID()))
            {
                string[] files = Directory.GetFiles(_path, "*.*", SearchOption.AllDirectories)
                    .Where(_ => !_.EndsWith(".meta")).Select(Utility.Path.GetRegularPath).ToArray();
                foreach (string file in files)
                {
                    Object[] objects = AssetDatabase.LoadAllAssetsAtPath(file);
                    if (objects.Length == 2)
                    {
                        _target.SpriteInfos[index].PathList.Add(file);
                        _target.SpriteInfos[index].SpriteList.Add(GetSprites(objects)[0]);
                    }
                    else
                    {
                        Sprite[] sprites = GetSprites(objects);
                        for (int j = 0; j < sprites.Length; j++)
                        {
                            string regularPath = Utility.Path.GetRegularPath(Path.Combine(file, sprites[j].name));
                            _target.SpriteInfos[index].PathList.Add(regularPath);
                            _target.SpriteInfos[index].SpriteList.Add(sprites[j]);
                        }
                    }
                }
            }
            else if (obj is SpriteAtlas spriteAtlas)
            {
                Object[] objs = spriteAtlas.GetPackables();
                for (int i = 0; i < objs.Length; i++)
                {
                    HandlePackable(i, objs[i]);
                }
            }
        }

        private Sprite[] GetSprites(Object[] objects)
        {
            return objects.OfType<Sprite>().ToArray();
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

        #region Sprite Info
        void ClearSpriteInfoWithIndex(int index)
        {
            if (index >= _target.SpriteInfos.Count)
                return;

            for (int i = _target.SpriteInfos[index].PathList.Count - 1; i >= 0; i--)
            {
                _target.SpriteInfos[index].PathList.RemoveAt(i);
                _target.SpriteInfos[index].SpriteList.RemoveAt(i);
            }

            _target.SpriteInfos[index].PathList.Clear();
            _target.SpriteInfos[index].SpriteList.Clear();
            _target.SpriteInfos.RemoveAt(index);
        }

        void ClearSpriteInfos()
        {
            if (_target.SpriteInfos.Count == 0)
                return;

            for (int i = _target.SpriteInfos.Count - 1; i >= 0; i--)
            {
                ClearSpriteInfoWithIndex(i);
            }
            _target.SpriteInfos.Clear();
        }
        #endregion
        #endregion
    }
}
