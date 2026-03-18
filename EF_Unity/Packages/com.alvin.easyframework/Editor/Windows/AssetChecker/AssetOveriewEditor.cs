/*
 * ================================================
 * Describe:      This script is used to show the resource detection overview. Let's thank LiangZG!!!!!
 * Author:        Xiaohei.Wang(Wenhao)
 * CreationTime:  2024-06-06 15:29:03
 * ModifyAuthor:  Xiaohei.Wang(Wenhao)
 * ModifyTime:    2024-06-06 15:29:03
 * ScriptVersion: 0.1
 * ===============================================
*/

using EasyFramework.Edit;
using UnityEditor;
using UnityEngine;

namespace EasyFramework.Windows.AssetChecker
{
    /// <summary>
    /// 资源总览编辑器
    /// </summary>
    internal class AssetOveriewEditor : EditorWindow
    {
        private int _tabIndex = 0;

        private OverviewPanelBase _currentPanel;
        private ConfigSettingPanel _configSettingPanel;
        private ModelOverviewPanel _modelOverviewPanel;
        private TextureOverviewPanel _textureOverviewPanel;
        private ParticleEffectOverviewPanel _particleEffectOverviewPanel;

        [MenuItem("EFTools/Tools/Asset Checker", false, priority = 100)]
        public static void ShowEditor()
        {
            AssetOveriewEditor assetOverview = GetWindow<AssetOveriewEditor>("AssetChecker");
            assetOverview.minSize = new Vector2(800f, 130f);
            assetOverview.Initialize();
            assetOverview.Show();
        }

        private void Initialize()
        {
            AssetsCheckerConfig.Initialize();
            _currentPanel = CreatePanel(_tabIndex);
        }

        void OnDestroy()
        {
            if (_modelOverviewPanel != null)
            {
                _modelOverviewPanel.OnDestroy();
                _modelOverviewPanel = null;
            }
            if (_textureOverviewPanel != null)
            {
                _textureOverviewPanel.OnDestroy();
                _textureOverviewPanel = null;
            }
            if (_particleEffectOverviewPanel != null)
            {
                _particleEffectOverviewPanel.OnDestroy();
                _particleEffectOverviewPanel = null;
            }
        }

        void OnGUI()
        {
            int newTab = _tabIndex;
            GUILayout.BeginHorizontal();
            if (GUILayout.Toggle(newTab == 0, LC.Combine(Lc.Texture), "ButtonLeft")) newTab = 0;
            if (GUILayout.Toggle(newTab == 1, LC.Combine(Lc.Model), "ButtonMid")) newTab = 1;
            if (GUILayout.Toggle(newTab == 2, LC.Combine(Lc.Effects), "ButtonMid")) newTab = 2;
            if (GUILayout.Toggle(newTab == 3, LC.Combine(Lc.Settings), "ButtonRight")) newTab = 3;
            GUILayout.EndHorizontal();
            GUILayout.Space(10);

            if (_tabIndex != newTab)
            {
                _tabIndex = newTab;
                _currentPanel = CreatePanel(newTab);
            }

            _currentPanel?.OnGUI();
            GUILayout.Space(10);
        }

        private OverviewPanelBase CreatePanel(int index)
        {
            switch (index)
            {
                case 0:
                    if (null == _textureOverviewPanel)
                    {
                        _textureOverviewPanel = new TextureOverviewPanel();
                        _textureOverviewPanel.Initialize();
                    }
                    return _textureOverviewPanel;
                case 1:
                    if (null == _modelOverviewPanel)
                    {
                        _modelOverviewPanel = new ModelOverviewPanel();
                        _modelOverviewPanel.Initialize();
                    }
                    return _modelOverviewPanel;
                case 2:
                    if (null == _particleEffectOverviewPanel)
                    {
                        _particleEffectOverviewPanel = new ParticleEffectOverviewPanel();
                        _particleEffectOverviewPanel.Initialize();
                    }
                    return _particleEffectOverviewPanel;
                case 3:
                    if (null == _configSettingPanel)
                    {
                        _configSettingPanel = new ConfigSettingPanel();
                        _configSettingPanel.Initialize();
                    }
                    return _configSettingPanel;
            }
            return null;
        }
    }
}
