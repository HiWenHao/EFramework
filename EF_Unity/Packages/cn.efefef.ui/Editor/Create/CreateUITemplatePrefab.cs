/*
 * ================================================
 * Describe:      This script is used to create template ui.
 * Author:        Xiaohei.Wang(Wenhao)
 * CreationTime:  2022-10-27 14:55:09
 * ModifyAuthor:  Alvin5100
 * ModifyTime:    2026-04-01 15:01:35
 * ScriptVersion: 0.1
 * ===============================================
 */

using System;
using System.Reflection;
using EasyFramework.Edit.Windows.ConfigPanel;
using EasyFramework.Managers.Ui;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace EasyFramework.Edit.Create
{
    public class CreateUITemplatePrefab
    {
        [MenuItem("GameObject/UI/EF/New Ui View", false, 20)]
        static void CreateUINewUIPage(MenuCommand menuCommand)
        {
            RectTransform rect = CreateUIObject(menuCommand, "New Ui View", new[] { typeof(Image) }).transform as RectTransform;
            
            if (rect == null) 
                return;
            
            UiBinding uiBinding = rect.gameObject.AddComponent<UiBinding>();
            uiBinding.Namespace = ConfigManager.Project.ScriptNamespace;
            uiBinding.PrefabPath = UiBindingConfig.Instance.UIPrefabPath;
            UnityEngine.Object.DestroyImmediate(rect.GetComponent<Image>());

            rect.anchorMax = Vector2.one;
            rect.anchorMin = Vector2.zero;
            rect.sizeDelta = Vector2.zero;
            rect.anchoredPosition = Vector2.zero;
        }

        [MenuItem("GameObject/UI/EF/Image Pro", false, 39)]
        static void CreateUIImagePro(MenuCommand menuCommand)
        {
            Transform tran = CreateUIObject(menuCommand, "ImagePro", new[] { typeof(ImagePro) }).transform;

            SetUIObjectSize(tran, new Vector2(100f, 100f));
        }

        [MenuItem("GameObject/UI/EF/Button Pro", false, 40)]
        static void CreateUIButtonPro(MenuCommand menuCommand)
        {
            Transform tran = CreateUIObject(menuCommand, "ButtonPro", new[] { typeof(Image), typeof(ButtonPro)}).transform;
            Text tmp = new GameObject("Text").AddComponent<Text>();
            tmp.transform.SetParent(tran);

            SetTextDefault(tmp, "ButtonPro");
            SetUIObjectSize(tran, new Vector2(160f, 30f));
            SetUIObjectSize(tmp.transform, Vector2.zero, true);
        }

        [MenuItem("GameObject/UI/EF/Button Pro - TextMeshPro", false, 41)]
        static void CreateUIButtonProTmp(MenuCommand menuCommand)
        {
            Transform tran = CreateUIObject(menuCommand, "ButtonPro(TMP)", new[] { typeof(Image), typeof(ButtonPro)}).transform;
            TextMeshProUGUI tmp = new GameObject("Text (TMP)").AddComponent<TextMeshProUGUI>();
            tmp.transform.SetParent(tran);

            SetTextDefault(tmp, "ButtonPro (TMP)");
            SetUIObjectSize(tran, new Vector2(160f, 30f));
            SetUIObjectSize(tmp.transform, Vector2.zero, true);
        }

        [MenuItem("GameObject/UI/EF/Radar Map", false, 42)]
        static void CreateUIRadarMap(MenuCommand menuCommand)
        {
            Transform tran = CreateUIObject(menuCommand, "Radar Map", new[] { typeof(Mask), typeof(RadarMap)}).transform;
            Image img = new GameObject("Image").AddComponent<Image>();
            img.transform.SetParent(tran);

            SetUIObjectSize(tran, Vector2.one * 100.0f);
            SetUIObjectSize(img.transform, Vector2.zero, true);
        }

        [MenuItem("GameObject/UI/EF/Slideshow", false, 44)]
        static void CreateUISlideshow(MenuCommand menuCommand)
        {
            Color[] colors = new[] { Color.gray, Color.yellow, Color.cyan }; 
            Transform tran =
                CreateUIObject(menuCommand, "Slideshow", new[] { typeof(Image), typeof(Mask), typeof(Slideshow) })
                    .transform;
            SetUIObjectSize(tran, new Vector2(200f, 200f));
            for (int i = -1; i < 2; i++)
            {
                Image img = new GameObject("Image_Item").AddComponent<Image>();
                img.color = colors[i + 1];
                img.transform.SetParent(tran);
                img.rectTransform.sizeDelta = Vector2.one * 100.0f;
                img.rectTransform.anchoredPosition = Vector2.right * 110.0f * i;
            }

        }

        [MenuItem("GameObject/UI/EF/About Bind", false, 999)]
        static void CreateUIBind(MenuCommand menuCommand)
        {
            EFConfigsPanel.Open<EFUiBindingConfigPanel>();
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
            if (full)
            {
                rect.anchorMin = Vector2.zero;
                rect.anchorMax = Vector2.one;
            }
        }

        /// <summary>
        /// 设置TMP到默认状态
        /// </summary>
        /// <param name="tmp">目标</param>
        /// <param name="showContents">文本显示内容</param>
        private static void SetTextDefault(TextMeshProUGUI tmp, string showContents)
        {
            tmp.fontSize = 14f;
            tmp.text = showContents;
            tmp.color = Color.black;
            tmp.alignment = TextAlignmentOptions.Center;
        }
        /// <summary>
        /// 设置Text到默认状态
        /// </summary>
        /// <param name="txt">目标</param>
        /// <param name="showContents">文本显示内容</param>
        private static void SetTextDefault(Text txt, string showContents)
        {
            txt.fontSize = 14;
            txt.text = showContents;
            txt.color = Color.black;
            txt.alignment = TextAnchor.MiddleCenter;
        }
        
        static GameObject CreateUIObject(MenuCommand menuCommand, string name, Type[] types, bool recordUndo = true)
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
                D.Error(LC.Tr(
                    "[ UiSystem ] [ CreateUITemplatePrefab ] {0}: 挂载组件失败，绝逼是 API 变更!   {1}",
                    "[ UiSystem ] [ CreateUITemplatePrefab ] {0}: failed to attach component, the Unity API must have changed!   {1}",
                    types[0].Name, e.Message));
                throw;
            }

            return go;
        }
    }
}
