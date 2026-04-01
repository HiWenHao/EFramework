/*
 * ================================================
 * Describe:      This script is used to .
 * Author:        Alvin5100(Wang)
 * CreationTime:  2026-03-19 15:23:14
 * ModifyAuthor:  Alvin5100
 * ModifyTime:    2026-04-01 15:01:35
 * ScriptVersion: 0.1
 * ===============================================
 */

using System.IO;
using EasyFramework.Edit.SpriteTools;
using EasyFramework.Edit.TaskList;
using EasyFramework.Edit.Windows.SettingPanel;
using UnityEditor;
using UnityEngine;

namespace EasyFramework.Edit.Create
{
    internal static class CreateSettings
    {
        [MenuItem("Assets/Create/EF/ProjectSetting", priority = 200)]
        private static void CreatedProjectSetting()
        {
            Instance<ProjectSetting>(folderPath: "Assets/Resources/Settings/");
        }
        
        [MenuItem("Assets/Create/EF/AutoBindSetting", priority = 210)]
        private static void CreatedAutoBindSetting()
        {
            Instance<AutoBindSetting>();
        }
        
        [MenuItem("Assets/Create/EF/PathConfigSetting", priority = 211)]
        private static void CreatedPathConfigSetting()
        {
            Instance<PathConfigSetting>();
        }
        
        [MenuItem("Assets/Create/EF/TaskListConfig", priority = 300)]
        private static void CreatedTaskListConfig()
        {
            Instance<TaskListConfig>(false);
        }
        
        [MenuItem("Assets/Create/EF/SpriteCollection", priority = 301)]
        private static void CreatedSpriteCollection()
        {
            Instance<SpriteCollection>(false);
        }
        
        /// <summary>
        /// 创建对应设置
        /// </summary>
        /// <param name="single">是否全局唯一， 默认为True</param>
        /// <param name="folderPath">所属文件夹，基于Assets下</param>
        /// <typeparam name="T">设置类型</typeparam>
        public static void Instance<T>(bool single = true, string folderPath = "")  where T : ScriptableObject
        {
            string typeName = typeof(T).Name;
            string path = !string.IsNullOrEmpty(folderPath) ? folderPath : Utility.Path.GetCurrentFolderPath();
            string configPath = string.IsNullOrEmpty(path) ? $"Assets/{typeName}.asset" : Path.Combine(path, $"{typeName}.asset");
            
            if (single && EditorUtils.CheckAssets<T>(out var assetPath))
            {
                T existingAsset = AssetDatabase.LoadAssetAtPath<T>(assetPath);
                Selection.activeObject = existingAsset;
                EditorGUIUtility.PingObject(existingAsset);
                return;
            }

            T asset = ScriptableObject.CreateInstance<T>();
        
            string folder = Path.GetDirectoryName(configPath);
            if (!Directory.Exists(folder))
                Directory.CreateDirectory(folder);
            AssetDatabase.CreateAsset(asset, configPath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            
            Selection.activeObject = asset;
        }
    }
}
