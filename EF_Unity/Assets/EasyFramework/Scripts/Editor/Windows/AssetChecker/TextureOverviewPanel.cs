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
        int m_SortName = 1,
            m_SortWidth = 1,
            m_SortHeight = 1,
            m_SortMaxSize = 1,
            m_SortMemorySize = 1;

        SettingView<TextureSetting> m_RuleView;
        List<TextureInformation> m_ShowTextureInfo;
        Dictionary<string, TextureSetting> m_SettingMap;

        internal override void Initialize()
        {
            m_ShowTextureInfo = new List<TextureInformation>();
            m_RuleView = new SettingView<TextureSetting>();
            m_SettingMap = new Dictionary<string, TextureSetting>();
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
            m_ShowTextureInfo.Clear();
            m_ShowTextureInfo = null;
            m_SettingMap.Clear();
            m_SettingMap = null;
            m_RuleView = null;
        }

        protected override void OnClickInfoList(int index)
        {
            if (index == 0)
            {
                m_SortName *= -1;
                m_ShowTextureInfo.Sort((x, y) => x.Name.CompareTo(y.Name) * m_SortName);
            }
            else if (index == 2)
            {
                m_SortWidth *= -1;
                m_ShowTextureInfo.Sort((x, y) => x.Width.CompareTo(y.Width) * m_SortWidth);
            }
            else if (index == 3)
            {
                m_SortHeight *= -1;
                m_ShowTextureInfo.Sort((x, y) => x.Height.CompareTo(y.Height) * m_SortHeight);
            }
            else if (index == 5)
            {
                m_SortMaxSize *= -1;
                m_ShowTextureInfo.Sort((x, y) => x.MaxSize.CompareTo(y.MaxSize) * m_SortMaxSize);
            }
            else if (index == 7)
            {
                m_SortMemorySize *= -1;
                m_ShowTextureInfo.Sort((x, y) => x.MemorySize.CompareTo(y.MemorySize) * m_SortMemorySize);
            }
        }

        protected override void FiltrateChanged(int index, bool fflush = false)
        {
            #region FindAllTextures
            List<string> _filtrates = new List<string>
            {
                "ALL"
            };
            List<string> _filePaths = new List<string>();
            Dictionary<TextureSetting, string[]> _fileMaps = new Dictionary<TextureSetting, string[]>();

            int _count = 0;
            if (m_RuleView.Settings != null)
            {
                m_SettingMap.Clear();
                for (int i = 0; i < m_RuleView.Settings.Count; i++)
                {
                    m_SettingMap[m_RuleView.Settings[i].AssetDesc] = m_RuleView.Settings[i];
                    _filePaths.Clear();
                    for (int j = 0; j < m_RuleView.Settings[i].Folder.Count; j++)
                    {
                        string rootFolder = m_RuleView.Settings[i].Folder[j];
                        if (string.IsNullOrEmpty(rootFolder)) continue;
                        string[] fileArr = Directory.GetFiles(rootFolder, "*.jpg", SearchOption.AllDirectories);
                        _filePaths.AddRange(fileArr);
                        _count += fileArr.Length;

                        fileArr = Directory.GetFiles(rootFolder, "*.png", SearchOption.AllDirectories);
                        _filePaths.AddRange(fileArr);
                        _count += fileArr.Length;
                    }
                    _fileMaps[m_RuleView.Settings[i]] = _filePaths.ToArray();
                    _filtrates.Add(m_RuleView.Settings[i].AssetDesc);
                }
            }
            Filtrates = _filtrates.ToArray();

            int _curFileIndex = 0;
            m_ShowTextureInfo.Clear();
            foreach (TextureSetting msb in _fileMaps.Keys)
            {
                string[] childFiles = _fileMaps[msb];
                for (int i = 0; i < childFiles.Length; i++)
                {
                    _curFileIndex++;
                    EditorUtility.DisplayProgressBar(LC.Combine(Lc.Holdon), LC.Combine(Lc.Holdon), _curFileIndex / _count);
                    TextureInformation asset = GetTextureInformation(childFiles[i]);
                    asset.AssetDesc = msb.AssetDesc;
                    m_ShowTextureInfo.Add(asset);
                }
            }
            ListCount = _curFileIndex;
            EditorUtility.ClearProgressBar();

            #endregion

            if (index == 0)
                return;

            for (int i = m_ShowTextureInfo.Count - 1; i >= 0; i--)
            {
                if (!Filtrates[index].Equals(m_ShowTextureInfo[i].AssetDesc))
                    m_ShowTextureInfo.RemoveAt(i);
            }
            ListCount = m_ShowTextureInfo.Count;

        }

        protected override void Refresh()
        {
            FiltrateChanged(FiltrateIndex, true);
        }

        protected override void RuleViewOnGUI()
        {
            m_RuleView.OnGUI();
        }

        protected override void DrawOne(int index)
        {
            TextureInformation _texture = m_ShowTextureInfo[index];
            if (GUILayout.Button(_texture.Name, AssetsCheckerConfig.ButtonStyle, GUILayout.Width(140f)))
            {
                Selection.activeObject = AssetDatabase.LoadAssetAtPath<Object>(_texture.FilePath);
            }

            float _width = (Screen.width - 150f) / (ObjectInfoList.Length - 1);
            GUILayout.Label(_texture.AssetDesc, AssetsCheckerConfig.LabelStyle, GUILayout.Width(_width));

            GUILayout.Label(_texture.Width.ToString(), AssetsCheckerConfig.LabelStyle, GUILayout.Width(_width));
            GUILayout.Label(_texture.Height.ToString(), AssetsCheckerConfig.LabelStyle, GUILayout.Width(_width));

            GUI.color = (_texture.MipMaps != m_SettingMap[_texture.AssetDesc].MipMaps) ? Color.red : Color.white;
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
            for (int i = 0; i < m_ShowTextureInfo.Count; i++)
            {
                if (m_ShowTextureInfo[i].MipMaps.Equals(m_SettingMap[m_ShowTextureInfo[i].AssetDesc].MipMaps)) continue;
                _textures.Add(m_ShowTextureInfo[i]);
            }

            for (int i = 0; i < _textures.Count; i++)
            {
                EditorUtility.DisplayProgressBar(LC.Combine(Lc.Holdon), LC.Combine(Lc.BeingProcessed), i / (float)_textures.Count);
                TextureImporter texImp = AssetImporter.GetAtPath(_textures[i].FilePath) as TextureImporter;
                texImp.mipmapEnabled = m_SettingMap[_textures[i].AssetDesc].MipMaps;
                _textures[i].MipMaps = m_SettingMap[_textures[i].AssetDesc].MipMaps;
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
            float _colorByte = 4;
            switch (format)
            {
                case TextureFormat.ARGB4444:
                case TextureFormat.RGB565:
                    _colorByte = 2;
                    break;
                case TextureFormat.ETC_RGB4:
                    _colorByte = 0.5f;
                    break;
            }
            return _colorByte * width * height / 1024;
        }
    }
}
