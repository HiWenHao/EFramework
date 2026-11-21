/*
 * ================================================
 * Describe:      This script is used to .
 * Author:        Alvin8412
 * CreationTime:  2026-05-25 14:30:03
 * ModifyAuthor:  Alvin8412
 * ModifyTime:    2026-05-25 14:30:03
 * ScriptVersion: 0.1
 * ===============================================
 */

using EasyFramework.Edit.SpriteTools;
using EasyFramework.Edit.Windows.ConfigPanel;
using EasyFramework.Ui;
using UnityEditor;
using ConfigManager = EasyFramework.Edit.ConfigManager;

namespace EasyFramework.Edit.Create
{
    /// <summary>
    /// 创建配置
    /// </summary>
    internal static class CreateConfig
    {
        [MenuItem("Assets/Create/EF/UI Binding Config", priority = 412)]
        private static void CreatedAutoBindSetting()
        {
            ConfigManager.Get<UiBindingConfig>(Utility.Path.GetCurrentFolderPath());
        }

        [MenuItem("Assets/Create/EF/UI Animation Config", priority = 413)]
        private static void CreatedAnimationConfig()
        {
            ConfigManager.Get<UiAnimationConfig>(Utility.Path.GetCurrentFolderPath());
        }

        [MenuItem("Assets/Create/EF/UI Atlas Collector", priority = 414)]
        private static void CreatedAtlasCollector()
        {
            ConfigManager.Get<AtlasCollector>(Utility.Path.GetCurrentFolderPath());
        }
    }
}