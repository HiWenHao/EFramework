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
        bool _openModel;
        bool _openColor;
        bool _openEffect;

        internal override void Initialize()
        {
            _openModel = true;
            _openColor = true;
            _openEffect = true;
            Tabs = new string[0];
            ObjectInfoList = new string[0];
            base.Initialize();
        }

        internal override void OnGUI()
        {
            base.OnGUI();
            _openModel = EditorGUILayout.BeginFoldoutHeaderGroup(_openModel, LC.Combine(new Lc[] { Lc.Model, Lc.Information, Lc.Settings} ));
            if (_openModel)
            {
                AssetsCheckerConfig.ModelMaxBones = DrawIntInfo("      " + LC.Combine(new Lc[] { Lc.Model, Lc.Max, Lc.Bone} ), AssetsCheckerConfig.ModelMaxBones, 60);
                AssetsCheckerConfig.ModelMaxTriangs = DrawIntInfo("      " + LC.Combine(new Lc[] { Lc.Model, Lc.Max, Lc.Triangular} ), AssetsCheckerConfig.ModelMaxTriangs, 50000);
            }
            EditorGUILayout.EndFoldoutHeaderGroup();
            EditorGUILayout.Separator();

            _openEffect = EditorGUILayout.BeginFoldoutHeaderGroup(_openEffect, LC.Combine(new Lc[] { Lc.Effects, Lc.Information, Lc.Settings} ));
            if (_openEffect)
            {
                AssetsCheckerConfig.EffectMaxMatrials = DrawIntInfo("      " + LC.Combine(new Lc[] { Lc.Effects, Lc.Max, Lc.Matrials} ), AssetsCheckerConfig.EffectMaxMatrials, 15);
                AssetsCheckerConfig.EffectMaxParticles = DrawIntInfo("      " + LC.Combine(new Lc[] { Lc.Effects, Lc.Max, Lc.Particle} ), AssetsCheckerConfig.EffectMaxParticles, 1000);
            }
            EditorGUILayout.EndFoldoutHeaderGroup();
            EditorGUILayout.Separator();

            _openColor = EditorGUILayout.BeginFoldoutHeaderGroup(_openColor, LC.Combine(new Lc[] { Lc.Score, Lc.Color, Lc.Settings} ));
            if (_openColor)
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
            if (GUILayout.Button(LC.Combine(new Lc[] { Lc.Save, Lc.Config} ), GUILayout.Width(80f)))
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
            int bones = EditorGUILayout.IntField("   " + infoText, value);
            EditorGUILayout.Space(10f, false);
            if (GUILayout.Button(LC.Combine(Lc.Reset), GUILayout.Width(80f)))
            {
                bones = defaultValue;
            }
            EditorGUILayout.Space(8f, false);
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.Space(6f, false);

            if (bones > 0)
                return bones;
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
            Color color = EditorGUILayout.ColorField("   " + infoText, value);
            EditorGUILayout.Space(10f, false);
            if (GUILayout.Button(LC.Combine(Lc.Reset), GUILayout.Width(80f)))
            {
                color = defaultValue;
            }
            EditorGUILayout.Space(8f, false);
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.Space(6f, false);

            return color;
        }
    }
}
