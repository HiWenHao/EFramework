/*
 * ================================================
 * Describe:      管理项目相关包资产.
 * Author:        Alvin8412
 * CreationTime:  2026-04-13 14:51:47
 * ModifyAuthor:  Alvin8412
 * ModifyTime:    2026-04-13 14:51:47
 * ScriptVersion: 0.1
 * ===============================================
 */

using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.PackageManager;
using UnityEngine;

namespace EasyFramework.Edit.Windows.ConfigPanel
{
    /// <summary>
    /// 管理项目相关包资产
    /// </summary>
    [EFConfig]
    public class EFPackageConfigPanel : EFConfigPanelBase
    {
        public override int Priority => -1;
        public override string Name => LC.Combine(new Lc[] { Lc.Project, Lc.Package, Lc.Assets });

        /// <summary> 远端服务地址 </summary>
        private const string ServerPath = "https://gitee.com/wang_xiaoheiiii/EFramework.git?path=EF_Unity/Packages/";

        private static GUIStyle _backgroundStyle;

        private bool _inEFFoldoutHeader = true;
        private bool _noInEFFoldoutHeader = true;
        private Vector2 _scrollPosition;
        
        private PackageConfig _config;
        private SerializedObject _packageConfig;
        private SerializedProperty _autoUpdate;
        private SerializedProperty _packagesInfoList;

        public override void OnEnable(string assetsPath)
        {
            LoadWindowData();
        }
        
        public override void LoadWindowData()
        {
            if (null != _config)
                return;

            _config = AssetDatabase.LoadAssetAtPath<PackageConfig>(
                "Packages/cn.efefef.packages/Editor Resources/EFPackageConfig.asset");
            _packageConfig = new SerializedObject(_config);
            _autoUpdate = _packageConfig.FindProperty("autoUpdate");
            _packagesInfoList = _packageConfig.FindProperty("packagesInfo");
            
            _backgroundStyle = new GUIStyle(GUI.skin.box)
            {
                padding = new RectOffset(10, 10, 10, 10),
                margin = new RectOffset(5, 5, 5, 5)
            };
            
            UpdatePackagesInfo();
        }

        public override void OnGUI()
        {
            if (_packageConfig == null)
                return;

            _packageConfig.Update();
            _packageConfig.ApplyModifiedProperties();
            
            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);

            _inEFFoldoutHeader = EditorGUILayout.BeginFoldoutHeaderGroup(_inEFFoldoutHeader,
                _inEFFoldoutHeader
                    ? LC.Combine(new Lc[] { Lc.Close, Lc.Assets, Lc.Package, Lc.List })
                    : LC.Combine(new Lc[] { Lc.Open, Lc.Assets, Lc.Package, Lc.List }));
            if (_inEFFoldoutHeader)
            {
                int count = _config.packagesInfo.Count;
                for (int i = 0; i < count; i++)
                {
                    DrawOnePackage(_config.packagesInfo[i]);
                }
            }
            EditorGUILayout.Space(12.0f);
            EditorGUILayout.EndFoldoutHeaderGroup();
            
            
            _noInEFFoldoutHeader = EditorGUILayout.BeginFoldoutHeaderGroup(_noInEFFoldoutHeader,
                _noInEFFoldoutHeader
                    ? LC.Combine(new Lc[] { Lc.Close, Lc.Other, Lc.Package, Lc.List })
                    : LC.Combine(new Lc[] { Lc.Open, Lc.Other, Lc.Package, Lc.List }));
            if (_noInEFFoldoutHeader)
            {
                
            }
            EditorGUILayout.Space(12.0f);
            EditorGUILayout.EndFoldoutHeaderGroup();
            
            
            EditorGUILayout.EndScrollView();
            EditorGUILayout.Space(12.0f);
        }

        private void DrawOnePackage(EFPackageInfo packageInfo)
        {
            Lc versionType = CompareVersion(packageInfo.CurrentVersion, packageInfo.ServerVersion);
            
            EditorGUILayout.BeginVertical(_backgroundStyle, GUILayout.ExpandWidth(true));
            
            EditorGUILayout.SelectableLabel(packageInfo.Name, GUIUtils.Title);
            EditorGUILayout.LabelField(packageInfo.Description, GUILayout.ExpandWidth(true));
            EditorGUILayout.Space();
            
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(LC.Combine(new []{Lc.Current, Lc.Version}), GUIUtils.Text, GUILayout.Width(80));
            EditorGUILayout.LabelField(packageInfo.CurrentVersion, GUIUtils.Text,GUILayout.ExpandWidth(true));
            DrawButton(versionType, packageInfo);
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.Space();

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(LC.Combine(new []{Lc.Server, Lc.Version}), GUIUtils.Text, GUILayout.Width(80));
            string serverContents = versionType == Lc.Non
                ? LC.Combine(new[] { Lc.Not, Lc.Exist })
                : packageInfo.ServerVersion;
                EditorGUILayout.LabelField(serverContents, GUIUtils.Text, GUILayout.ExpandWidth(true));
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndVertical();
            EditorGUILayout.Space();
        }

        private void DrawButton(Lc versionType, EFPackageInfo packageInfo)
        {
            if (versionType == Lc.Upload && packageInfo.FromGit)
                return;
            
            Lc type = versionType == Lc.Non? Lc.Unload: versionType;
            GUIUtils.Button.normal.textColor = type switch
            {
                Lc.Download or Lc.Update => Color.green,
                Lc.Unload => GUIUtils.LightRed,
                _ => Color.white
            };
            if (GUILayout.Button(LC.Combine(type), GUIUtils.Button,GUILayout.Width(160)))
            {
                
            }
        }

        private void UpdatePackagesInfo()
        {
            for (int i = _config.packagesInfo.Count - 1; i >= 0; i--)
            {
                _config.packagesInfo.RemoveAt(i);
            }
            _config.packagesInfo.Clear();
            
            foreach (var packageInfo in UnityEditor.PackageManager.PackageInfo.GetAllRegisteredPackages())
            {
                var name = packageInfo.name;
                if (!name.Contains("cn.efefef."))
                    continue;

                //packageInfo.git.
                _config.packagesInfo.Add(new EFPackageInfo()
                {
                    Name = string.IsNullOrEmpty(packageInfo.displayName) ?  name : packageInfo.displayName,
                    FromGit = packageInfo.source == PackageSource.Git,
                    Description = packageInfo.description,
                    CurrentVersion = packageInfo.version,
                    ServerVersion = packageInfo.source == PackageSource.Git ? packageInfo.git.revision : "0.1.0"//.git.revision,
                });
            }
        }
        
        private Lc CompareVersion(string v1, string v2)
        {
            if (string.IsNullOrEmpty(v1))
                return Lc.Download;
            if (string.IsNullOrEmpty(v2))
                return Lc.Non;
            
            var parts1 = v1.Split('.');
            var parts2 = v2.Split('.');

            for (int i = 0; i < 3; i++)
            {
                if (parts1[i] == parts2[i])
                    continue;

                int n1 = int.Parse(parts1[i]);
                int n2 = int.Parse(parts2[i]);
                
                if (n1 < n2)
                    return Lc.Update;
                if (n1 > n2)
                    return Lc.Upload;
            }

            return Lc.Unload;
        }
        }
}