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
using EasyFramework.Managers.Ui;
using UnityEditor;

namespace EasyFramework.Edit.Create
{
    /// <summary>
    /// 创建配置
    /// </summary>
    internal static class CreateConfig
    {
        [MenuItem("Assets/Create/EF/UI Binding Config", priority = 212)]
        private static void CreatedAutoBindSetting()
        {
            CreateSettings.Instance<UiBindingConfig>();
        }

        [MenuItem("Assets/Create/EF/UI Animation Config", priority = 213)]
        private static void CreatedAnimationConfig()
        {
            CreateSettings.Instance<UiAnimationConfig>();
        }

        [MenuItem("Assets/Create/EF/UI Sprite Collection", priority = 320)]
        private static void CreatedSpriteCollection()
        {
            CreateSettings.Instance<SpriteCollection>();
        }
    }
}