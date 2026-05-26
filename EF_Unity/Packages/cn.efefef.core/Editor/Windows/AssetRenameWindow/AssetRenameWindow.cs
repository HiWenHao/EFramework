/*
 * ================================================
 * Describe:        用来做资源重命名工具
 * Author:          Shaofei.Cui
 * CreationTime:    2026-05-26-11:30:59
 * ModifyAuthor:    Alvin5100
 * ModifyTime:      2026-05-26-13:24:07
 * ScriptVersion:   1.0
 * ===============================================
 */

using EasyFramework.Edit;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace EasyFramework.Windows.AssetRename
{
    /// <summary>
    /// 资产重命名工具面板 - Asset rename tool panel
    /// </summary>
    internal class AssetRenameWindow : EditorWindow
    {
        /// <summary> 资产信息 </summary>
        private class AssetInfo
        {
            public string Path; //路径
            public string Name; //名字
            public string Extension; //扩展名
            public System.Type AssetType; //资产类型
            public string PreviewName; //预览名称
            public Texture2D Icon; //图标
        }

        private enum RenameMode
        {
            Prefix,
            Suffix,
            Replace,
            Sequence
        }

        private Vector2 _scrollPos;
        private readonly List<AssetInfo> _assetList = new();

        private RenameMode _mode = RenameMode.Prefix;
        private string _addText = "";
        private string _findText = "";
        private string _replaceText = "";
        private int _seqStart = 0;
        private int _seqStep = 1;
        private int _seqPadding = 2;

        // Colors
        private static readonly Color TagTextureColor = new Color(0.3f, 0.75f, 0.4f);
        private static readonly Color TagMaterialColor = new Color(0.3f, 0.55f, 0.9f);
        private static readonly Color TagPrefabColor = new Color(0.9f, 0.6f, 0.2f);
        private static readonly Color TagAudioColor = new Color(0.65f, 0.4f, 0.85f);
        private static readonly Color TagScriptableObjColor = new Color(0.2f, 0.75f, 0.8f);
        private static readonly Color TagShaderColor = new Color(0.85f, 0.4f, 0.7f);
        private static readonly Color TagAnimColor = new Color(0.9f, 0.85f, 0.2f);
        private static readonly Color TagDefaultColor = new Color(0.55f, 0.55f, 0.55f);

        // Cached styles
        private GUIStyle _boldLabelStyle;
        private GUIStyle _oldNameStyle;
        private GUIStyle _previewNameStyle;
        private GUIStyle _tagStyle;
        private GUIStyle _removeBtnStyle;

        [MenuItem("EFTools/Tools/Assets Rename", priority = 201)]
        private static void OpenWindow()
        {
            var window = GetWindow<AssetRenameWindow>(false, LC.Combine(Lc.Assets, Lc.Rename, Lc.Tool));
            window.minSize = new Vector2(560, 350);
            window.Show();
        }

        private void OnSelectionChange()
        {
            AddSelectedAssets();
            Repaint();
        }

        private void OnGUI()
        {
            #region Style Initialize

            _oldNameStyle = new GUIStyle(EditorStyles.label)
            {
                normal = { textColor = new Color(0.6f, 0.6f, 0.6f) },
                fontSize = 10
            };
            _previewNameStyle = new GUIStyle(EditorStyles.label)
            {
                normal = { textColor = new Color(0.2f, 0.8f, 0.4f) },
                fontStyle = FontStyle.Bold
            };
            _tagStyle = new GUIStyle(EditorStyles.miniLabel)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 9,
                normal = { textColor = Color.white }
            };
            _removeBtnStyle = new GUIStyle(EditorStyles.miniButton)
            {
                fontSize = 12,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = new Color(0.85f, 0.3f, 0.3f) },
                hover = { textColor = Color.red }
            };

            #endregion

            DrawToolbar();
            EditorGUILayout.Space(4);
            DrawRenameOptions();
            EditorGUILayout.Space(4);
            DrawAssetList();
            EditorGUILayout.Space(4);
            DrawBottomButtons();
        }

        #region Toolbar

        private void DrawToolbar()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            if (GUILayout.Button(LC.Combine(Lc.Refresh, Lc.Select), EditorStyles.toolbarButton, GUILayout.Width(180)))
            {
                RefreshAssetList();
            }

            if (GUILayout.Button(LC.Combine(Lc.Clear), EditorStyles.toolbarButton, GUILayout.Width(80)))
            {
                _assetList.Clear();
            }

            GUILayout.FlexibleSpace();
            GUILayout.Label($"{LC.Combine(Lc.Select)} {LC.Combine(Lc.Count)}: {_assetList.Count}",
                EditorStyles.miniLabel, GUILayout.Width(100));
            EditorGUILayout.EndHorizontal();
        }

        #endregion

        #region Rename Options

        private void DrawRenameOptions()
        {
            EditorGUILayout.BeginVertical("HelpBox");

            EditorGUILayout.LabelField(LC.Combine(new Lc[] { Lc.Rename, Lc.Option }), EditorStyles.boldLabel);

            var newMode = (RenameMode)EditorGUILayout.Popup(LC.Combine(Lc.Mode), (int)_mode,
                new string[]
                {
                    LC.Combine(Lc.Prefix),
                    LC.Combine(Lc.Suffix),
                    LC.Combine(Lc.Replace),
                    LC.Combine(Lc.Sequence)
                });
            if (newMode != _mode)
            {
                _mode = newMode;
                UpdatePreviewNames();
            }

            switch (_mode)
            {
                case RenameMode.Prefix:
                case RenameMode.Suffix:
                    EditorGUI.BeginChangeCheck();
                    _addText = EditorGUILayout.TextField(LC.Combine(Lc.Text), _addText);
                    if (EditorGUI.EndChangeCheck())
                        UpdatePreviewNames();
                    break;

                case RenameMode.Replace:
                    EditorGUI.BeginChangeCheck();
                    _findText = EditorGUILayout.TextField(LC.Combine(Lc.Replace), _findText);
                    _replaceText = EditorGUILayout.TextField(LC.Combine(Lc.Artw_ReplaceTo), _replaceText);
                    if (EditorGUI.EndChangeCheck())
                        UpdatePreviewNames();
                    break;

                case RenameMode.Sequence:
                    EditorGUI.BeginChangeCheck();
                    _addText = EditorGUILayout.TextField(LC.Combine(Lc.Prefix), _addText);
                    _seqStart = EditorGUILayout.IntField(LC.Combine(Lc.Start), _seqStart);
                    _seqStep = EditorGUILayout.IntField(LC.Combine(Lc.Step), _seqStep);
                    _seqPadding = EditorGUILayout.IntField(LC.Combine(Lc.Padding), _seqPadding);
                    if (EditorGUI.EndChangeCheck())
                    {
                        _seqPadding = Mathf.Max(1, _seqPadding);
                        _seqStep = Mathf.Max(1, _seqStep);
                        UpdatePreviewNames();
                    }

                    break;
            }

            EditorGUILayout.EndVertical();
        }

        #endregion

        #region Asset List

        private void DrawAssetList()
        {
            EditorGUILayout.BeginVertical("HelpBox");
            EditorGUILayout.LabelField(LC.Combine(new Lc[] { Lc.Asset, Lc.List }), EditorStyles.boldLabel);

            float itemHeight = EditorGUIUtility.singleLineHeight + 6f;
            float viewHeight = Mathf.Min(_assetList.Count, 20) * itemHeight + 4f;
            if (_assetList.Count == 0)
                viewHeight = 40f;

            if (_assetList.Count == 0)
            {
                EditorGUILayout.HelpBox(LC.Combine(Lc.Artw_SelectAssetsHint), MessageType.Info);
                EditorGUILayout.EndVertical();
                return;
            }

            _scrollPos = EditorGUILayout.BeginScrollView(_scrollPos, GUILayout.Height(viewHeight));

            for (int i = 0; i < _assetList.Count; i++)
            {
                var item = _assetList[i];
                EditorGUILayout.BeginHorizontal(GUILayout.Height(itemHeight));

                // Icon
                if (item.Icon != null)
                    GUILayout.Label(item.Icon, GUILayout.Width(20), GUILayout.Height(20));
                else
                    GUILayout.Label("", GUILayout.Width(20));

                // Original name
                GUILayout.Label(item.Name, item.Name != item.PreviewName ? _oldNameStyle : EditorStyles.label,
                    GUILayout.Width(140));

                // Arrow & preview name
                if (item.Name != item.PreviewName)
                {
                    GUILayout.Label("→", EditorStyles.miniLabel, GUILayout.Width(16));
                    GUILayout.Label(item.PreviewName, _previewNameStyle, GUILayout.Width(240));
                }

                GUILayout.FlexibleSpace();

                // Type tag
                DrawTypeTag(item.AssetType);

                // Remove button
                if (GUILayout.Button("X", _removeBtnStyle, GUILayout.Width(20), GUILayout.Height(18)))
                {
                    _assetList.RemoveAt(i);
                    UpdatePreviewNames();
                    i--;
                }

                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.EndScrollView();
            EditorGUILayout.EndVertical();
        }

        private void DrawTypeTag(System.Type type)
        {
            var tagName = GetTypeName(type);
            var tagColor = GetTypeColor(type);

            var prevBg = GUI.backgroundColor;
            GUI.backgroundColor = tagColor;

            GUILayout.Label(tagName, _tagStyle, GUILayout.Width(70), GUILayout.Height(18));

            GUI.backgroundColor = prevBg;
        }

        private string GetTypeName(System.Type type)
        {
            if (type == typeof(Texture2D) || type == typeof(Texture) || type.IsSubclassOf(typeof(Texture)))
                return "Texture";
            if (type == typeof(Material))
                return "Material";
            if (type == typeof(GameObject) || type == typeof(Component))
                return "Prefab";
            if (type == typeof(AudioClip))
                return "Audio";
            if (type.IsSubclassOf(typeof(ScriptableObject)) || type == typeof(ScriptableObject))
                return "ScriptObj";
            if (type == typeof(Shader))
                return LC.Combine(Lc.Shader);
            if (type == typeof(AnimationClip) || type.Name == "AnimatorController")
                return "Anim";
            if (type.Name.EndsWith("Controller") || type.Name.EndsWith("Animator"))
                return "Anim";
            return type.Name.Length > 10 ? type.Name[..7] + ".." : type.Name;
        }

        private Color GetTypeColor(System.Type type)
        {
            if (type == typeof(Texture2D) || type == typeof(Texture) || type.IsSubclassOf(typeof(Texture)))
                return TagTextureColor;
            if (type == typeof(Material))
                return TagMaterialColor;
            if (type == typeof(GameObject) || type == typeof(Component))
                return TagPrefabColor;
            if (type == typeof(AudioClip))
                return TagAudioColor;
            if (type.IsSubclassOf(typeof(ScriptableObject)) || type == typeof(ScriptableObject))
                return TagScriptableObjColor;
            if (type == typeof(Shader))
                return TagShaderColor;
            if (type == typeof(AnimationClip) || type.Name == "AnimatorController" || type.Name.EndsWith("Controller"))
                return TagAnimColor;
            return TagDefaultColor;
        }

        #endregion

        #region Bottom Buttons

        private void DrawBottomButtons()
        {
            EditorGUILayout.BeginHorizontal();

            if (GUILayout.Button(LC.Combine(new Lc[] { Lc.Preview, Lc.Rename }), GUILayout.Height(28)))
            {
                UpdatePreviewNames();
            }

            EditorGUI.BeginDisabledGroup(_assetList.Count == 0);
            if (GUILayout.Button(LC.Combine(new Lc[] { Lc.Apply, Lc.Rename }), GUILayout.Height(28)))
            {
                ApplyRename();
            }

            EditorGUI.EndDisabledGroup();

            EditorGUILayout.EndHorizontal();
        }

        #endregion

        #region Apply Rename

        private void ApplyRename()
        {
            int success = 0;
            int failed = 0;

            for (int i = 0; i < _assetList.Count; i++)
            {
                var item = _assetList[i];
                if (item.Name == item.PreviewName) continue;

                string dir = System.IO.Path.GetDirectoryName(item.Path);
                string newPath = System.IO.Path.Combine(dir, item.PreviewName + item.Extension);
                newPath = newPath.Replace("\\", "/");

                // Check for duplicate names
                bool duplicate = _assetList.Any(other =>
                    other != item &&
                    other.PreviewName == item.PreviewName &&
                    System.IO.Path.GetDirectoryName(other.Path) == dir);

                if (duplicate)
                {
                    Debug.LogWarning($"{LC.Combine(Lc.Artw_RenameConflictSkip)}: {item.Name} → {item.PreviewName}");
                    failed++;
                    continue;
                }

                string error = AssetDatabase.RenameAsset(item.Path, item.PreviewName);
                if (string.IsNullOrEmpty(error))
                {
                    item.Name = item.PreviewName;
                    item.Path = newPath;
                    success++;
                }
                else
                {
                    Debug.LogError($"{LC.Combine(Lc.Artw_RenameFailed)}: {item.Name} → {item.PreviewName}, {error}");
                    failed++;
                }
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            if (success > 0 || failed > 0)
            {
                Debug.Log(
                    $"{LC.Combine(Lc.Rename)} {LC.Combine(Lc.Complete)}: {LC.Combine(Lc.Success)} {success}, {LC.Combine(Lc.Failed)} {failed}");
            }
            else
            {
                Debug.Log(LC.Combine(Lc.Artw_NoNeedRename));
            }

            RefreshAssetList();
        }

        #endregion

        #region Preview & Refresh

        private void RefreshAssetList()
        {
            _assetList.Clear();
            AddSelectedAssets();
        }

        private void AddSelectedAssets()
        {
            var selected = Selection.GetFiltered<UnityEngine.Object>(SelectionMode.Assets);
            var existingPaths = new HashSet<string>(_assetList.Select(a => a.Path));

            foreach (var obj in selected)
            {
                var assetPath = AssetDatabase.GetAssetPath(obj);
                if (string.IsNullOrEmpty(assetPath)) continue;
                if (AssetDatabase.IsValidFolder(assetPath)) continue;
                if (existingPaths.Contains(assetPath)) continue;

                var info = new AssetInfo
                {
                    Path = assetPath,
                    Name = System.IO.Path.GetFileNameWithoutExtension(assetPath),
                    Extension = System.IO.Path.GetExtension(assetPath),
                    AssetType = obj.GetType(),
                    Icon = AssetPreview.GetMiniThumbnail(obj)
                };
                if (info.Icon == null)
                    info.Icon = EditorGUIUtility.ObjectContent(obj, obj.GetType()).image as Texture2D;

                info.PreviewName = info.Name;
                _assetList.Add(info);
                existingPaths.Add(assetPath);
            }

            UpdatePreviewNames();
        }

        private void UpdatePreviewNames()
        {
            for (int i = 0; i < _assetList.Count; i++)
            {
                var item = _assetList[i];
                switch (_mode)
                {
                    case RenameMode.Prefix:
                        item.PreviewName = _addText + item.Name;
                        break;
                    case RenameMode.Suffix:
                        item.PreviewName = item.Name + _addText;
                        break;
                    case RenameMode.Replace:
                        if (!string.IsNullOrEmpty(_findText))
                            item.PreviewName = item.Name.Replace(_findText, _replaceText);
                        else
                            item.PreviewName = item.Name;
                        break;
                    case RenameMode.Sequence:
                        int seqNum = _seqStart + i * _seqStep;
                        item.PreviewName = _addText + seqNum.ToString("D" + _seqPadding);
                        break;
                }
            }
        }

        #endregion
    }
}