/*
 * ================================================
 * Describe:      This script is used to show the resource detection overview. Let's thank LiangZG!!!!!
 * Author:        Xiaohei.Wang(Wenhao)
 * CreationTime:  2024-06-06 15:29:21
 * ModifyAuthor:  Xiaohei.Wang(Wenhao)
 * ModifyTime:    2024-06-06 15:29:21
 * ScriptVersion: 0.1
 * ===============================================
*/

using EasyFramework.Edit;
using UnityEditor;
using UnityEngine;

namespace EasyFramework.Windows.AssetChecker
{
    internal class ConfigSettingPanel : OverviewPanelBase
    {
        bool m_OpenModel, 
             m_OpenColor, 
             m_OpenEffect;

        internal override void Initialize()
        {
            m_OpenModel = true;
            m_OpenColor = true;
            m_OpenEffect = true;
            Tabs = new string[0];
            ObjectInfoList = new string[0];
            base.Initialize();
        }

        internal override void OnGUI()
        {
            base.OnGUI();
            m_OpenModel = EditorGUILayout.BeginFoldoutHeaderGroup(m_OpenModel, LC.Combine(LC.Language.Model, LC.Language.Information, LC.Language.Settings));
            if (m_OpenModel)
            {
                AssetsCheckerConfig.ModelMaxBones = DrawIntInfo("      " + LC.Language.ModelMaxBones, AssetsCheckerConfig.ModelMaxBones, 60);
                AssetsCheckerConfig.ModelMaxTriangs = DrawIntInfo("      " + LC.Language.ModelMaxTriangs, AssetsCheckerConfig.ModelMaxTriangs, 50000);
            }
            EditorGUILayout.EndFoldoutHeaderGroup();
            EditorGUILayout.Separator();

            m_OpenEffect = EditorGUILayout.BeginFoldoutHeaderGroup(m_OpenEffect, LC.Combine(LC.Language.Effects, LC.Language.Information,LC.Language.Settings));
            if (m_OpenEffect)
            {
                AssetsCheckerConfig.EffectMaxMatrials = DrawIntInfo("      " + LC.Language.EffectMaxMatrials, AssetsCheckerConfig.EffectMaxMatrials, 15);
                AssetsCheckerConfig.EffectMaxParticles = DrawIntInfo("      " + LC.Language.EffectMaxParticles, AssetsCheckerConfig.EffectMaxParticles, 1000);
            }
            EditorGUILayout.EndFoldoutHeaderGroup();
            EditorGUILayout.Separator();

            m_OpenColor = EditorGUILayout.BeginFoldoutHeaderGroup(m_OpenColor, LC.Combine(LC.Language.Score, LC.Language.Color, LC.Language.Settings));
            if (m_OpenColor)
            {
                AssetsCheckerConfig.ScoreColors[0] = DrawColorInfo("      " + AssetsCheckerConfig.ScoreNames[0], AssetsCheckerConfig.ScoreColors[0], Color.green);
                AssetsCheckerConfig.ScoreColors[1] = DrawColorInfo("      " + AssetsCheckerConfig.ScoreNames[1], AssetsCheckerConfig.ScoreColors[1], new Color(1.0f, .85f, 0.0f));
                AssetsCheckerConfig.ScoreColors[2] = DrawColorInfo("      " + AssetsCheckerConfig.ScoreNames[2], AssetsCheckerConfig.ScoreColors[2], new Color(1.0f, 0.5f, 0.0f));
                AssetsCheckerConfig.ScoreColors[3] = DrawColorInfo("      " + AssetsCheckerConfig.ScoreNames[3], AssetsCheckerConfig.ScoreColors[3], new Color(1.0f, 0.4f, 0.4f));
                AssetsCheckerConfig.ScoreColors[4] = DrawColorInfo("      " + AssetsCheckerConfig.ScoreNames[4], AssetsCheckerConfig.ScoreColors[4], new Color(0.6f, 0.0f, 0.0f));
            }
            EditorGUILayout.EndFoldoutHeaderGroup();
        }

        protected override void BeforeTheRefreshButton()
        {
            if (GUILayout.Button(LC.Combine(LC.Language.Save, LC.Language.Config), GUILayout.Width(80f)))
            {
                AssetsCheckerConfig.SaveConfig();
                AssetDatabase.Refresh();
            }
        }

        protected override void Refresh()
        {

        }

        /// <summary>
        /// 绘制Int类型设置信息
        /// </summary>
        /// <param name="infoText">信息文本</param>
        /// <param name="value">具体值</param>
        /// <param name="defaultValue">默认值</param>
        /// <returns></returns>
        int DrawIntInfo(string infoText, int value, int defaultValue)
        {
            EditorGUILayout.BeginHorizontal();
            int _bones = EditorGUILayout.IntField("   " + infoText, value);
            EditorGUILayout.Space(10f, false);
            if (GUILayout.Button(LC.Language.Reset, GUILayout.Width(80f)))
            {
                _bones = defaultValue;
            }
            EditorGUILayout.Space(8f, false);
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.Space(6f, false);

            if (_bones > 0)
                return _bones;
            else return 1;
        }

        /// <summary>
        /// 绘制颜色类型设置信息
        /// </summary>
        /// <param name="infoText">信息文本</param>
        /// <param name="value">具体值</param>
        /// <param name="defaultValue">默认值</param>
        /// <returns></returns>
        Color DrawColorInfo(string infoText, Color value, Color defaultValue)
        {
            EditorGUILayout.BeginHorizontal();
            Color _color = EditorGUILayout.ColorField("   " + infoText, value);
            EditorGUILayout.Space(10f, false);
            if (GUILayout.Button(LC.Language.Reset, GUILayout.Width(80f)))
            {
                _color = defaultValue;
            }
            EditorGUILayout.Space(8f, false);
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.Space(6f, false);

            return _color;
        }
    }
}
