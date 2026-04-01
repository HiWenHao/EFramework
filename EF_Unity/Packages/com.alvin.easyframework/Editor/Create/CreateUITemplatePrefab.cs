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
using EasyFramework.Edit.Windows.SettingPanel;
using EasyFramework.UI;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace EasyFramework.Edit.Create
{
    public class CreateUITemplatePrefab
    {
        [MenuItem("GameObject/UI/EF/New UI Page", false, 20)]
        static void CreateUINewUIPage(MenuCommand menuCommand)
        {
            RectTransform tran = CreateUIObject(menuCommand, "UiPage", new[] { typeof(Image) }).transform as RectTransform;
            tran.gameObject.AddComponent<Canvas>();
            tran.gameObject.AddComponent<GraphicRaycaster>();
            tran.gameObject.AddComponent<CanvasGroup>();
            tran.gameObject.AddComponent<UiBind>();
            UnityEngine.Object.DestroyImmediate(tran.GetComponent<Image>());
            
            tran.anchorMax = Vector2.one;
            tran.anchorMin = Vector2.zero;
            tran.sizeDelta = Vector2.zero;
            tran.anchoredPosition = Vector2.zero;
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

        [MenuItem("GameObject/UI/EF/Scroll View Pro", false, 43)]
        static void CreateUIScrollViewPro(MenuCommand menuCommand)
        {
            Transform tran = CreateUIObject(menuCommand, "Scroll View Pro", new[] { typeof(Image), typeof(ScrollRectPro)}).transform;
            RectTransform scrollbar = CreateUIObject(menuCommand, "Scrollbar Pro", new[] { typeof(Image), typeof(ScrollbarPro)}, false).transform as RectTransform;

            #region Content
            RectTransform content = new GameObject("Content").AddComponent<RectTransform>();
            Image element = new GameObject("Element").AddComponent<Image>();
            Text text = new GameObject("Text").AddComponent<Text>();
            content.transform.SetParent(tran);
            element.transform.SetParent(content);
            text.transform.SetParent(element.transform);

            tran.GetComponent<ScrollRectPro>().Elemental = element.gameObject;
            SetUIObjectSize(tran, Vector2.one * 220.0f);
            SetUIObjectSize(content.transform, Vector2.right * -20.0f, true);
            SetUIObjectSize(element.transform, new Vector2(200.0f, 50.0f));
            SetUIObjectSize(text.transform, Vector2.zero, true);
            SetTextDefault(text, "Scroll View Pro Item");
            content.anchoredPosition = Vector2.right * -10.0f;
            element.rectTransform.pivot = Vector2.up;
            element.rectTransform.anchorMin = Vector2.up;
            element.rectTransform.anchorMax = Vector2.up;
            element.color = new Color(1.0f, 0.75f, 0.75f, 1.0f);
            #endregion

            #region Scrollbar
            RectTransform sliding = new GameObject("Sliding Area").AddComponent<RectTransform>();
            RectTransform handle = new GameObject("Handle").AddComponent<Image>().rectTransform;
            scrollbar.SetParent(tran);
            sliding.SetParent(scrollbar);
            handle.SetParent(sliding);
            
            scrollbar.sizeDelta = Vector2.right * 20.0f;
            scrollbar.anchorMin = Vector2.right;
            scrollbar.anchorMax = Vector2.one;
            scrollbar.pivot = new Vector2(1.0f, 0.5f);
            scrollbar.anchoredPosition = Vector2.zero;

            ScrollbarPro scroPro = scrollbar.gameObject.GetComponent<ScrollbarPro>();
            scroPro.handleRect = handle;
            scroPro.direction = ScrollbarPro.Direction.TopToBottom;
            scrollbar.gameObject.GetComponent<Image>().color = Color.gray;
            
            sliding.anchorMin = Vector2.zero;
            sliding.anchorMax = Vector2.one;
            sliding.anchoredPosition = Vector2.zero;
            sliding.sizeDelta = Vector2.one * -20.0f;

            handle.anchorMin = Vector2.zero;
            handle.anchorMax = Vector2.zero;
            handle.sizeDelta = Vector2.one * 20.0f;
            #endregion
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
            EFSettingsPanel.Open(2);
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
                D.Error($"{types[0].Name}: 挂载组件失败，绝逼是 API 变更!   {e.Message}");
                throw;
            }

            return go;
        }
    }
}
