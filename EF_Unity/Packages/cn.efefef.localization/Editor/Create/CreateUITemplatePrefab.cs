/*
 * ================================================
 * Describe:        This script is used to .
 * Author:          Alvin8412
 * CreationTime:    2026-10-16 20:15:50
 * ModifyAuthor:    Alvin8412
 * ModifyTime:      2026-10-16 20:15:50
 * ScriptVersion:   0.1
 * ================================================
 */

using System;
using System.Reflection;
using EasyFramework.Edit;
using UnityEditor;
using UnityEngine;

namespace EasyFramework.Localization.Edit
{
    public class CreateUITemplatePrefab
    {
        [MenuItem("GameObject/UI/EF/TextMeshPro - LocalizedText", false, 41)]
        static void CreateUIButtonProTmp(MenuCommand menuCommand)
        {
            Transform tran = CreateUIObject(menuCommand, "LocalizedText(TMP)", new[] { typeof(LocalizedText) }).transform;
            SetUIObjectSize(tran, new Vector2(160f, 30f));
        }

        /// <summary>
        /// 设置UI物体尺寸
        /// </summary>
        /// <param name="trans">目标</param>
        /// <param name="size">尺寸</param>
        /// <param name="full">是否填充为父物体大小</param>
        private static void SetUIObjectSize(Transform trans, Vector2 size, bool full = false)
        {
            RectTransform rect = (RectTransform)trans;
            rect.sizeDelta = size;
            rect.localPosition = Vector3.zero;
            if (!full) return;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
        }

        private static GameObject CreateUIObject(MenuCommand menuCommand, string name, Type[] types,
            bool recordUndo = true)
        {
            GameObject go = new GameObject(name, types);

            if (recordUndo)
                Undo.RegisterCreatedObjectUndo(go, $"Creat{name}{Time.frameCount}");
            // 以下代码通过反射获取 UGUI 中新增 UI 组件的体验：会自动构建 UI 运行环境
            try
            {
                Type type = Type.GetType("UnityEditor.UI.MenuOptions,UnityEditor.UI.dll", true);
                var method = type.GetMethod("PlaceUIElementRoot", BindingFlags.Static | BindingFlags.NonPublic);
                method.Invoke(null, new object[] { go, menuCommand });
            }
            catch (Exception e)
            {
                D.Error(LC.Tr("{0}: 挂载组件失败，多半是 API 变更！   {1}",
                    "{0}: failed to attach the component, most likely an API change!   {1}",
                    types[0].Name, e.Message));
                throw;
            }

            return go;
        }
    }
}