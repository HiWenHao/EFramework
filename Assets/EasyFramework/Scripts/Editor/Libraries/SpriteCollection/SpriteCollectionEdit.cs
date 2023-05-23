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
        bool m_Atlas;
        bool m_AllOverwrite;
        string m_FrameworkAtlasFolder;
        List<bool> HasPreview;

        SpriteCollection m_Target;
        SerializedProperty AtlasFolder;
        SerializedProperty TargetObjects;
        private void OnEnable()
        {
            m_Target = (SpriteCollection)target;
            AtlasFolder = serializedObject.FindProperty("m_AtlasFolder");
            TargetObjects = serializedObject.FindProperty("m_Objects");
            m_FrameworkAtlasFolder = ProjectUtility.Path.AtlasFolder;

            m_AllOverwrite = true;
            HasPreview = new List<bool>();
            for (int i = m_Target.Atlas.Count - 1; i >= 0; i--)
            {
                HasPreview.Add(false);
            }
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.PropertyField(TargetObjects, true);

            ShowObjectsAndAtlas();

            ShowSprites();

            if (GUILayout.Button("Pack Preview", GUILayout.Width(100f)))
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
            EditorGUILayout.LabelField("图集路径");
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(new GUIContent("Atlas Path"), GUILayout.Width(130f));
            EditorGUILayout.LabelField(new GUIContent(AtlasFolder.stringValue));
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            {
                if (GUILayout.Button(new GUIContent("Selection Path", "Selection the atlas path.")))
                {
                    string _path = EditorUtility.OpenFolderPanel("选择UI图集保存路径", Application.dataPath, "");
                    if (string.IsNullOrEmpty(_path))
                    {
                        if (string.IsNullOrEmpty(AtlasFolder.stringValue))
                            AtlasFolder.stringValue = m_FrameworkAtlasFolder;
                    }
                    else
                        AtlasFolder.stringValue = Utility.AssetPath.GetPathInAssetsFolder(_path) + "/";
                }
                if (GUILayout.Button(new GUIContent("Default Path", "Set the path with EF project atlas folder.")))
                {
                    AtlasFolder.stringValue = m_FrameworkAtlasFolder;
                }
            }
            EditorGUILayout.EndHorizontal();
        }
        #endregion

        #region Show Atlas
        void ShowObjectsAndAtlas()
        {
            m_Atlas = EditorGUILayout.Foldout(m_Atlas, $"Atlas\t({m_Target.Atlas.Count})");
            if (m_Atlas)
            {
                EditorGUILayout.Space(EditorGUIUtility.singleLineHeight);
                for (int i = 0; i < m_Target.Atlas.Count; i++)
                {
                    EditorGUILayout.BeginHorizontal();
                    if (m_Target.Atlas[i])
                    {
                        EditorGUILayout.LabelField(m_Target.Atlas[i].name, GUILayout.MinWidth(30f), GUILayout.MaxWidth(150f));
                        EditorGUILayout.ObjectField(m_Target.Atlas[i], typeof(Sprite), false);

                        if (GUILayout.Button(new GUIContent("X", "Remove the atlas in current collection.")))
                        {
                            ClearSpriteInfoWithIndex(i);
                            ClearAtlasWithIndex(i);
                        }
                        if (GUILayout.Button(new GUIContent("Del", "Delete the atlas in asset.")))
                        {
                            if (EditorUtility.DisplayDialog("删除图集", $"确定删除 {m_Target.Atlas[i].name} 图集", "确定"))
                            {
                                AssetDatabase.DeleteAsset(AssetDatabase.GetAssetPath(m_Target.Atlas[i]));
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
            if (GUILayout.Button(new GUIContent("Clear All", "Remove the all atlas in current collection")))
            {
                ClearAllAtlas();
                ClearSpriteInfos();
                AssetDatabase.Refresh();
            }

            if (GUILayout.Button(new GUIContent("Delete All", "Delete the all atlas in asset")))
            {
                for (int i = m_Target.Atlas.Count - 1; i >= 0; i--)
                    AssetDatabase.DeleteAsset(AssetDatabase.GetAssetPath(m_Target.Atlas[i]));
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
            for (int i = 0; i < m_Target.SpriteInfos.Count; i++)
            {
                if (i == HasPreview.Count)
                    HasPreview.Add(false);

                HasPreview[i] = EditorGUILayout.Foldout(HasPreview[i], new GUIContent($"({m_Target.SpriteInfos[i].SpriteList.Count})\tAtlas - {m_Target.SpriteInfos[i].FolderName}"));
                if (HasPreview[i] && i < m_Target.SpriteInfos.Count)
                {
                    EditorGUILayout.BeginVertical(EditorStyles.helpBox);

                    for (int j = 0; j < m_Target.SpriteInfos[i].PathList.Count; j++)
                    {
                        EditorGUILayout.BeginHorizontal();
                        EditorGUILayout.TextField(new GUIContent($"{m_Target.SpriteInfos[i].SpriteList[j].name}", m_Target.SpriteInfos[i].PathList[j]), m_Target.SpriteInfos[i].PathList[j]);
                        EditorGUILayout.ObjectField(m_Target.SpriteInfos[i].SpriteList[j], typeof(Sprite), false);
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
            m_AllOverwrite = EditorGUILayout.ToggleLeft(new GUIContent("Overwrite", "Overwrite the all atlas."), m_AllOverwrite, GUILayout.MaxWidth(100f));
            if (GUILayout.Button("Crate Atlas"))
            {
                CreateAtlas();
            }
            EditorGUILayout.EndHorizontal();
        }
        void CreateAtlas()
        {
            ClearAllAtlas();
            if (string.IsNullOrEmpty(AtlasFolder.stringValue))
            {
                EditorUtility.DisplayDialog("提示", $"请先选择图集生成文件夹！", "确定");
                return;
            }

            if (m_Target.TargetObjects.Find(_ => _ is SpriteAtlas) != null)
            {
                EditorUtility.DisplayDialog("提示", $"SpriteCollection 中存在Atlas 请检查!", "确定");
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
            for (int i = 0; i < m_Target.TargetObjects.Count; i++)
            {
                string _atlas = Utility.Path.GetRegularPath(Path.Combine(AtlasFolder.stringValue, m_Target.TargetObjects[i].name + ".spriteatlas"));
                bool _result = false;
                if (!m_AllOverwrite && File.Exists(_atlas))
                {
                    _result = EditorUtility.DisplayDialog("提示", $"已存在 {m_Target.TargetObjects[i].name} 图集,是否覆盖？", "确定", "取消");
                    if (!_result)
                    {
                        bool _hasSA = false;

                        for (int j = 0; j < m_Target.Atlas.Count; j++)
                        {
                            if (m_Target.Atlas[j].name == m_Target.TargetObjects[i].name)
                            {
                                _hasSA = true;
                                continue;
                            }
                        }
                        if (!_hasSA)
                        {
                            SpriteAtlas _sat = AssetDatabase.LoadAssetAtPath<SpriteAtlas>(_atlas);
                            if (i < m_Target.Atlas.Count)
                            {
                                SpriteAtlas _tempSA = m_Target.Atlas[i];
                                m_Target.Atlas.Add(_tempSA);
                                m_Target.Atlas[i] = _sat;
                                _tempSA = null;
                            }
                            else
                                m_Target.Atlas.Add(_sat);
                            _sat = null;
                        }
                        continue;
                    }
                }

                SpriteAtlas _sa = new SpriteAtlas();
                _sa.SetPackingSettings(_packSet);
                _sa.SetTextureSettings(_textureSet);

                AssetDatabase.CreateAsset(_sa, _atlas);

                _sa.Add(new Object[] { m_Target.TargetObjects[i] });
                if (_result && i < m_Target.Atlas.Count && m_Target.Atlas[i].name != m_Target.TargetObjects[i].name)
                {
                    m_Target.Atlas[i] = _sa;
                }
                else
                {
                    m_Target.Atlas.Add(_sa);
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
            for (int i = 0; i < m_Target.Atlas.Count; i++)
            {
                for (int j = m_Target.TargetObjects.Count - 1; j >= 0; j--)
                {
                    if (m_Target.Atlas[i].name == m_Target.TargetObjects[j].name)
                    {
                        Object obj = m_Target.TargetObjects[j];
                        m_Target.SpriteInfos.Add(new SpriteCollection.SpriteInfo()
                        {
                            FolderName = obj.name,
                        });
                        HandlePackable(i, obj);
                        HasPreview.Add(false);
                        continue;
                    }
                }
            }

            SpriteAtlasUtility.PackAtlases(m_Target.Atlas.ToArray(), BuildTarget.NoTarget);
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
                    m_Target.SpriteInfos[index].PathList.Add(_path);
                }
                else
                {
                    string _regularPath = Utility.Path.GetRegularPath(Path.Combine(_path, sp.name));
                    m_Target.SpriteInfos[index].PathList.Add(_regularPath);
                }
                m_Target.SpriteInfos[index].SpriteList.Add(sp);
            }
            else if (obj is Texture2D)
            {
                Object[] objects = AssetDatabase.LoadAllAssetsAtPath(_path);
                if (objects.Length == 2)
                {
                    m_Target.SpriteInfos[index].PathList.Add(_path);
                    m_Target.SpriteInfos[index].SpriteList.Add(GetSprites(objects)[0]);
                }
                else
                {
                    Sprite[] sprites = GetSprites(objects);
                    for (int j = 0; j < sprites.Length; j++)
                    {
                        string regularPath = Utility.Path.GetRegularPath(Path.Combine(_path, sprites[j].name));
                        m_Target.SpriteInfos[index].PathList.Add(regularPath);
                        m_Target.SpriteInfos[index].SpriteList.Add(sprites[j]);
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
                        m_Target.SpriteInfos[index].PathList.Add(file);
                        m_Target.SpriteInfos[index].SpriteList.Add(GetSprites(objects)[0]);
                    }
                    else
                    {
                        Sprite[] sprites = GetSprites(objects);
                        for (int j = 0; j < sprites.Length; j++)
                        {
                            string regularPath = Utility.Path.GetRegularPath(Path.Combine(file, sprites[j].name));
                            m_Target.SpriteInfos[index].PathList.Add(regularPath);
                            m_Target.SpriteInfos[index].SpriteList.Add(sprites[j]);
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
            m_Target.Atlas.RemoveAt(index);
        }

        void ClearAllAtlas()
        {
            for (int i = m_Target.Atlas.Count - 1; i >= 0; i--)
            {
                ClearAtlasWithIndex(i);
            }
        }
        #endregion

        #region Sprite Info
        void ClearSpriteInfoWithIndex(int index)
        {
            if (index >= m_Target.SpriteInfos.Count)
                return;

            for (int i = m_Target.SpriteInfos[index].PathList.Count - 1; i >= 0; i--)
            {
                m_Target.SpriteInfos[index].PathList.RemoveAt(i);
                m_Target.SpriteInfos[index].SpriteList.RemoveAt(i);
            }

            m_Target.SpriteInfos[index].PathList.Clear();
            m_Target.SpriteInfos[index].SpriteList.Clear();
            m_Target.SpriteInfos.RemoveAt(index);
        }

        void ClearSpriteInfos()
        {
            if (m_Target.SpriteInfos.Count == 0)
                return;

            for (int i = m_Target.SpriteInfos.Count - 1; i >= 0; i--)
            {
                ClearSpriteInfoWithIndex(i);
            }
            m_Target.SpriteInfos.Clear();
        }
        #endregion
        #endregion
    }
}
