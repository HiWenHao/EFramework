/*
 * ================================================
 * Describe:      This script is used to show the resource detection overview. Let's thank LiangZG!!!!!
 * Author:        Xiaohei.Wang(Wenhao)
 * CreationTime:  2024-06-06 15:29:55
 * ModifyAuthor:  Xiaohei.Wang(Wenhao)
 * ModifyTime:    2024-06-06 15:29:55
 * ScriptVersion: 0.1
 * ===============================================
*/

using EasyFramework.Edit;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace EasyFramework.Windows.AssetChecker
{
    /// <summary>
    /// 贴图材质面板
    /// </summary>
    internal class TextureOverviewPanel : OverviewPanelBase
    {
        int _sortName = 1;
        int _sortWidth = 1;
        int _sortHeight = 1;
        int _sortMaxSize = 1;
        int _sortMemorySize = 1;

        SettingView<TextureSetting> _ruleView;
        List<TextureInformation> _showTextureInfo;
        Dictionary<string, TextureSetting> _settingMap;

        internal override void Initialize()
        {
            _showTextureInfo = new List<TextureInformation>();
            _ruleView = new SettingView<TextureSetting>();
            _settingMap = new Dictionary<string, TextureSetting>();
            ObjectInfoList = new string[]
            {
                LC.Combine(new Lc[]{ Lc.Texture, Lc.Name }),
                LC.Combine(new Lc[]{ Lc.Resource, Lc.Type }),
                LC.Combine(Lc.Width),
                LC.Combine(Lc.Height),
                "MipMaps",
                LC.Combine(new Lc[]{ Lc.Max, Lc.Size }),
                LC.Combine(Lc.Compression),
                LC.Combine(Lc.Memory),
            };

            base.Initialize();
        }

        internal override void OnDestroy()
        {
            base.OnDestroy();
            _showTextureInfo.Clear();
            _showTextureInfo = null;
            _settingMap.Clear();
            _settingMap = null;
            _ruleView = null;
        }

        protected override void OnClickInfoList(int index)
        {
            if (index == 0)
            {
                _sortName *= -1;
                _showTextureInfo.Sort((x, y) => x.Name.CompareTo(y.Name) * _sortName);
            }
            else if (index == 2)
            {
                _sortWidth *= -1;
                _showTextureInfo.Sort((x, y) => x.Width.CompareTo(y.Width) * _sortWidth);
            }
            else if (index == 3)
            {
                _sortHeight *= -1;
                _showTextureInfo.Sort((x, y) => x.Height.CompareTo(y.Height) * _sortHeight);
            }
            else if (index == 5)
            {
                _sortMaxSize *= -1;
                _showTextureInfo.Sort((x, y) => x.MaxSize.CompareTo(y.MaxSize) * _sortMaxSize);
            }
            else if (index == 7)
            {
                _sortMemorySize *= -1;
                _showTextureInfo.Sort((x, y) => x.MemorySize.CompareTo(y.MemorySize) * _sortMemorySize);
            }
        }

        protected override void FiltrateChanged(int index, bool fflush = false)
        {
            #region FindAllTextures
            List<string> filtrates = new List<string>
            {
                "ALL"
            };
            List<string> filePaths = new List<string>();
            Dictionary<TextureSetting, string[]> fileMaps = new Dictionary<TextureSetting, string[]>();

            int count = 0;
            if (_ruleView.Settings != null)
            {
                _settingMap.Clear();
                for (int i = 0; i < _ruleView.Settings.Count; i++)
                {
                    _settingMap[_ruleView.Settings[i].AssetDesc] = _ruleView.Settings[i];
                    filePaths.Clear();
                    for (int j = 0; j < _ruleView.Settings[i].Folder.Count; j++)
                    {
                        string rootFolder = _ruleView.Settings[i].Folder[j];
                        if (string.IsNullOrEmpty(rootFolder)) continue;
                        string[] fileArr = Directory.GetFiles(rootFolder, "*.jpg", SearchOption.AllDirectories);
                        filePaths.AddRange(fileArr);
                        count += fileArr.Length;

                        fileArr = Directory.GetFiles(rootFolder, "*.png", SearchOption.AllDirectories);
                        filePaths.AddRange(fileArr);
                        count += fileArr.Length;
                    }
                    fileMaps[_ruleView.Settings[i]] = filePaths.ToArray();
                    filtrates.Add(_ruleView.Settings[i].AssetDesc);
                }
            }
            Filtrates = filtrates.ToArray();

            int curFileIndex = 0;
            _showTextureInfo.Clear();
            foreach (TextureSetting msb in fileMaps.Keys)
            {
                string[] childFiles = fileMaps[msb];
                for (int i = 0; i < childFiles.Length; i++)
                {
                    curFileIndex++;
                    EditorUtility.DisplayProgressBar(LC.Combine(Lc.Holdon), LC.Combine(Lc.Holdon), curFileIndex / count);
                    TextureInformation asset = GetTextureInformation(childFiles[i]);
                    asset.AssetDesc = msb.AssetDesc;
                    _showTextureInfo.Add(asset);
                }
            }
            ListCount = curFileIndex;
            EditorUtility.ClearProgressBar();

            #endregion

            if (index == 0)
                return;

            for (int i = _showTextureInfo.Count - 1; i >= 0; i--)
            {
                if (!Filtrates[index].Equals(_showTextureInfo[i].AssetDesc))
                    _showTextureInfo.RemoveAt(i);
            }
            ListCount = _showTextureInfo.Count;

        }

        protected override void Refresh()
        {
            FiltrateChanged(FiltrateIndex, true);
        }

        protected override void RuleViewOnGUI()
        {
            _ruleView.OnGUI();
        }

        protected override void DrawOne(int index)
        {
            TextureInformation _texture = _showTextureInfo[index];
            if (GUILayout.Button(_texture.Name, AssetsCheckerConfig.ButtonStyle, GUILayout.Width(140f)))
            {
                Selection.activeObject = AssetDatabase.LoadAssetAtPath<Object>(_texture.FilePath);
            }

            float _width = (Screen.width - 150f) / (ObjectInfoList.Length - 1);
            GUILayout.Label(_texture.AssetDesc, AssetsCheckerConfig.LabelStyle, GUILayout.Width(_width));

            GUILayout.Label(_texture.Width.ToString(), AssetsCheckerConfig.LabelStyle, GUILayout.Width(_width));
            GUILayout.Label(_texture.Height.ToString(), AssetsCheckerConfig.LabelStyle, GUILayout.Width(_width));

            GUI.color = (_texture.MipMaps != _settingMap[_texture.AssetDesc].MipMaps) ? Color.red : Color.white;
            GUILayout.Label(_texture.MipMaps.ToString(), AssetsCheckerConfig.LabelStyle, GUILayout.Width(_width));
            GUI.color = Color.white;

            GUILayout.Label(_texture.MaxSize.ToString(), AssetsCheckerConfig.LabelStyle, GUILayout.Width(_width));
            GUILayout.Label(_texture.Format, AssetsCheckerConfig.LabelStyle, GUILayout.Width(_width));

            GUILayout.Label(_texture.MemoryText, AssetsCheckerConfig.LabelStyle, GUILayout.Width(_width));

        }

        protected override void BeforeTheRefreshButton()
        {
            if (GUILayout.Button(LC.Combine(Lc.AlignAt) + "MipMaps", GUILayout.Width(130)))
            {
                SwitchMipMaps();
            }
        }

        /// <summary>
        /// Align with MipMaps
        /// <para>对齐多级贴图</para>
        /// </summary>
        void SwitchMipMaps()
        {
            List<TextureInformation> _textures = new List<TextureInformation>();
            for (int i = 0; i < _showTextureInfo.Count; i++)
            {
                if (_showTextureInfo[i].MipMaps.Equals(_settingMap[_showTextureInfo[i].AssetDesc].MipMaps)) continue;
                _textures.Add(_showTextureInfo[i]);
            }

            for (int i = 0; i < _textures.Count; i++)
            {
                EditorUtility.DisplayProgressBar(LC.Combine(Lc.Holdon), LC.Combine(Lc.BeingProcessed), i / (float)_textures.Count);
                TextureImporter texImp = AssetImporter.GetAtPath(_textures[i].FilePath) as TextureImporter;
                texImp.mipmapEnabled = _settingMap[_textures[i].AssetDesc].MipMaps;
                _textures[i].MipMaps = _settingMap[_textures[i].AssetDesc].MipMaps;
            }

            _textures.Clear();
            EditorUtility.ClearProgressBar();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        /// <summary>
        /// Get texture information
        /// <para>获取贴图文件信息</para>
        /// </summary>
        /// <param name="filePath"></param>
        private TextureInformation GetTextureInformation(string filePath)
        {
            Texture2D _t2d = AssetDatabase.LoadAssetAtPath<Texture2D>(filePath);
            TextureImporter _texImp = AssetImporter.GetAtPath(filePath) as TextureImporter;

            TextureInformation _textrue = new TextureInformation
            {
                Name = Path.GetFileName(filePath),
                FilePath = filePath,

                Width = _t2d.width,
                Height = _t2d.height,
                Format = _t2d.format.ToString(),

                MipMaps = _texImp.mipmapEnabled,
                MaxSize = _texImp.maxTextureSize,
            };

            _textrue.MemorySize = ComputeMemory(_t2d.format, _textrue.Width, _textrue.Height);
            _textrue.MemoryText = _textrue.MemorySize >= 1024 ?
                string.Format("{0:F}MB", _textrue.MemorySize / 1024) :
                string.Format("{0:F}KB", _textrue.MemorySize);

            return _textrue;
        }

        /// <summary>
        /// Compute memory.
        /// <para>计算内存大小</para>
        /// </summary>
        private float ComputeMemory(TextureFormat format, int width, int height)
        {
            float colorByte = 4;
            switch (format)
            {
                case TextureFormat.ARGB4444:
                case TextureFormat.RGB565:
                    colorByte = 2;
                    break;
                case TextureFormat.ETC_RGB4:
                    colorByte = 0.5f;
                    break;
            }
            return colorByte * width * height / 1024;
        }
    }
}
