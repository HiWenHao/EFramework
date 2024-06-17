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
        private int m_TabIndex = 0;

        private OverviewPanelBase m_CurrentPanel;
        private ConfigSettingPanel m_ConfigSettingPanel;
        private ModelOverviewPanel m_ModelOverviewPanel;
        private TextureOverviewPanel m_TextureOverviewPanel;
        private ParticleEffectOverviewPanel m_ParticleEffectOverviewPanel;

        [MenuItem("EFTools/Assets/Asset Checker &C", false, priority = 40)]
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
            m_CurrentPanel = CreatePanel(m_TabIndex);
        }

        void OnDestroy()
        {
            if (m_ModelOverviewPanel != null)
            {
                m_ModelOverviewPanel.OnDestroy();
                m_ModelOverviewPanel = null;
            }
            if (m_TextureOverviewPanel != null)
            {
                m_TextureOverviewPanel.OnDestroy();
                m_TextureOverviewPanel = null;
            }
            if (m_ParticleEffectOverviewPanel != null)
            {
                m_ParticleEffectOverviewPanel.OnDestroy();
                m_ParticleEffectOverviewPanel = null;
            }
        }

        void OnGUI()
        {
            int _newTab = m_TabIndex;
            GUILayout.BeginHorizontal();
            if (GUILayout.Toggle(_newTab == 0, LC.Combine("Texture"), "ButtonLeft")) _newTab = 0;
            if (GUILayout.Toggle(_newTab == 1, LC.Combine("Model"), "ButtonMid")) _newTab = 1;
            if (GUILayout.Toggle(_newTab == 2, LC.Combine("Effects"), "ButtonMid")) _newTab = 2;
            if (GUILayout.Toggle(_newTab == 3, LC.Combine("Settings"), "ButtonRight")) _newTab = 3;
            GUILayout.EndHorizontal();
            GUILayout.Space(10);

            if (m_TabIndex != _newTab)
            {
                m_TabIndex = _newTab;
                m_CurrentPanel = CreatePanel(_newTab);
            }

            m_CurrentPanel?.OnGUI();
            GUILayout.Space(10);
        }

        private OverviewPanelBase CreatePanel(int index)
        {
            switch (index)
            {
                case 0:
                    if (null == m_TextureOverviewPanel)
                    {
                        m_TextureOverviewPanel = new TextureOverviewPanel();
                        m_TextureOverviewPanel.Initialize();
                    }
                    return m_TextureOverviewPanel;
                case 1:
                    if (null == m_ModelOverviewPanel)
                    {
                        m_ModelOverviewPanel = new ModelOverviewPanel();
                        m_ModelOverviewPanel.Initialize();
                    }
                    return m_ModelOverviewPanel;
                case 2:
                    if (null == m_ParticleEffectOverviewPanel)
                    {
                        m_ParticleEffectOverviewPanel = new ParticleEffectOverviewPanel();
                        m_ParticleEffectOverviewPanel.Initialize();
                    }
                    return m_ParticleEffectOverviewPanel;
                case 3:
                    if (null == m_ConfigSettingPanel)
                    {
                        m_ConfigSettingPanel = new ConfigSettingPanel();
                        m_ConfigSettingPanel.Initialize();
                    }
                    return m_ConfigSettingPanel;
            }
            return null;
        }
    }
}
