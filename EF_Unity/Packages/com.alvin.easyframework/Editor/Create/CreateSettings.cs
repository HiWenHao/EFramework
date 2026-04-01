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
using EasyFramework.Edit.TodoList;
using EasyFramework.Edit.Windows.ConfigPanel;
using UnityEditor;
using UnityEngine;

namespace EasyFramework.Edit.Create
{
    internal static class CreateSettings
    {
        [MenuItem("Assets/Create/EF/ProjectConfig", priority = 200)]
        private static void CreatedProjectConfig()
        {
            Instance<ProjectConfig>(folderPath: "Assets/Resources/Configs/");
        }
        
        [MenuItem("Assets/Create/EF/AutoBindingConfig", priority = 210)]
        private static void CreatedAutoBindSetting()
        {
            Instance<AutoBindingConfig>();
        }
        
        [MenuItem("Assets/Create/EF/PathConfig", priority = 211)]
        private static void CreatedPathConfigSetting()
        {
            Instance<PathConfig>();
        }
        
        [MenuItem("Assets/Create/EF/TodoListConfig", priority = 300)]
        private static void CreatedTaskListConfig()
        {
            Instance<TodoListConfig>(false);
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
