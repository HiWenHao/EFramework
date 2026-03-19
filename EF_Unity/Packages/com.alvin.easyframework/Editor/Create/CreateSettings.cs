/*
 * ================================================
 * Describe:      This script is used to .
 * Author:        Alvin5100(Wang)
 * CreationTime:  2026-03-19 15:23:14
 * ModifyAuthor:  Alvin5100(Wang)
 * ModifyTime:    2026-03-19 15:23:14
 * ScriptVersion: 0.1
 * ===============================================
 */

using System.IO;
using EasyFramework.Edit.Setting;
using EasyFramework.Edit.AutoBind;
using EasyFramework.Edit.SpriteTools;
using EasyFramework.Edit.TaskList;
using EasyFramework.Windows.SettingPanel;
using UnityEditor;
using UnityEngine;

namespace EasyFramework.Edit
{
    internal static class CreateSettings
    {
        [MenuItem("Assets/Create/EF/ProjectSetting", priority = 200)]
        private static void CreatedProjectSetting()
        {
            CreatedSetting<ProjectSetting>();
        }
        
        [MenuItem("Assets/Create/EF/AutoBindSetting", priority = 210)]
        private static void CreatedAutoBindSetting()
        {
            CreatedSetting<AutoBindSetting>();
        }
        
        [MenuItem("Assets/Create/EF/PathConfigSetting", priority = 211)]
        private static void CreatedPathConfigSetting()
        {
            CreatedSetting<PathConfigSetting>();
        }
        
        [MenuItem("Assets/Create/EF/TaskListConfig", priority = 300)]
        private static void CreatedTaskListConfig()
        {
            CreatedSetting<TaskListConfig>(false);
        }
        
        [MenuItem("Assets/Create/EF/SpriteCollection", priority = 301)]
        private static void CreatedSpriteCollection()
        {
            CreatedSetting<SpriteCollection>(false);
        }
        
        private static void CreatedSetting<T>(bool single = true)  where T : ScriptableObject
        {
            string typeName = typeof(T).Name;
            string configPath = single
                ? Path.Combine(Utility.Path.GetEfPath(), $"/Editor Resources/Settings/{typeName}.asset")
                : Path.Combine(Utility.Path.GetCurrentFolderPath(), $"{typeName}.asset");
            
            if (single)
            {
                T existingAsset = AssetDatabase.LoadAssetAtPath<T>(configPath);
                if (existingAsset == null)
                {
                    string[] guids = AssetDatabase.FindAssets($"t:{typeName}");
                    if (guids.Length > 0)
                        existingAsset = AssetDatabase.LoadAssetAtPath<T>(AssetDatabase.GUIDToAssetPath(guids[0]));
                }

                if (existingAsset != null)
                {
                    Selection.activeObject = existingAsset;
                    EditorGUIUtility.PingObject(existingAsset);
                    return;
                }
            }

            T asset = ScriptableObject.CreateInstance<T>();
        
            string folderPath = Path.GetDirectoryName(configPath);
            if (!Directory.Exists(folderPath))
                Directory.CreateDirectory(folderPath);
            AssetDatabase.CreateAsset(asset, configPath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            
            Selection.activeObject = asset;
        }
    }
}