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
using EasyFramework.Managers.Pool;
using EasyFramework.Managers.Ui;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace EasyFramework.Edit.Create
{
    public class CreateUITemplatePrefab
    {
        private const string CircularScrollRectField = "_scrollRect";
        private const string CircularContentField = "_content";
        private const string CircularItemPrefabField = "_itemPrefab";
        private const string ProScrollRectField = "scrollRect";
        private const string ProItemPrefabField = "itemPrefab";
        private const string ChildViewsNodeName = "ChildViews";

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

            // 自动生成子面板容器：AddChild 会把子面板挂到该节点下
            GameObject childViews = new GameObject(ChildViewsNodeName, typeof(RectTransform));
            RectTransform childViewsRect = childViews.GetComponent<RectTransform>();
            childViewsRect.SetParent(rect, false);
            childViewsRect.anchorMin = Vector2.zero;
            childViewsRect.anchorMax = Vector2.one;
            childViewsRect.sizeDelta = Vector2.zero;
            childViewsRect.anchoredPosition = Vector2.zero;
        }

        [MenuItem("GameObject/UI/EF/Image Pro", false, 40)]
        static void CreateUIImagePro(MenuCommand menuCommand)
        {
            Transform tran = CreateUIObject(menuCommand, "ImagePro", new[] { typeof(ImagePro) }).transform;

            SetUIObjectSize(tran, new Vector2(100f, 100f));
        }

        [MenuItem("GameObject/UI/EF/Button Pro", false, 41)]
        static void CreateUIButtonPro(MenuCommand menuCommand)
        {
            Transform tran = CreateUIObject(menuCommand, "ButtonPro", new[] { typeof(Image), typeof(ButtonPro)}).transform;
            Text tmp = new GameObject("Text").AddComponent<Text>();
            tmp.transform.SetParent(tran);

            SetTextDefault(tmp, "ButtonPro");
            SetUIObjectSize(tran, new Vector2(160f, 30f));
            SetUIObjectSize(tmp.transform, Vector2.zero, true);
        }

        [MenuItem("GameObject/UI/EF/Button Pro - TextMeshPro", false, 42)]
        static void CreateUIButtonProTmp(MenuCommand menuCommand)
        {
            Transform tran = CreateUIObject(menuCommand, "ButtonPro(TMP)", new[] { typeof(Image), typeof(ButtonPro)}).transform;
            TextMeshProUGUI tmp = new GameObject("Text (TMP)").AddComponent<TextMeshProUGUI>();
            tmp.transform.SetParent(tran);

            SetTextDefault(tmp, "ButtonPro (TMP)");
            SetUIObjectSize(tran, new Vector2(160f, 30f));
            SetUIObjectSize(tmp.transform, Vector2.zero, true);
        }

        [MenuItem("GameObject/UI/EF/Radar Map", false, 44)]
        static void CreateUIRadarMap(MenuCommand menuCommand)
        {
            // RadarMap 自身用 UV 映射铺满 sprite/渐变，无需 Mask 与子 Image
            Transform tran = CreateUIObject(menuCommand, "Radar Map", new[] { typeof(RadarMap) }).transform;
            SetUIObjectSize(tran, Vector2.one * 100.0f);
        }

        [MenuItem("GameObject/UI/EF/Slideshow", false, 45)]
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

        /// <summary>
        /// 创建循环滚动列表模板
        /// <para>Create a circular scroll list template.</para>
        /// </summary>
        [MenuItem("GameObject/UI/EF/Circular Scroll List", false, 60)]
        static void CreateUICircularScrollList(MenuCommand menuCommand)
        {
            GameObject root = CreateScrollListRoot(menuCommand, "Circular Scroll List", typeof(CircularScrollList));
            CircularScrollList list = root.GetComponent<CircularScrollList>();
            if (list == null)
            {
                D.Error(LC.Tr(
                    "[ UiManager ] [ CreateUITemplatePrefab ] 循环滚动列表组件挂载失败",
                    "[ UiManager ] [ CreateUITemplatePrefab ] Failed to attach CircularScrollList component"));
                return;
            }
            RectTransform content = CreateScrollViewStructure(root.transform);

            GameObject item = CreateScrollItemTemplate(content);
            item.AddComponent<CircularScrollItem>();
            // PoolManager 回收依赖 PooledObject，缺失会走回退销毁路径，模板预先挂好
            item.AddComponent<PooledObject>();

            SerializedObject so = new SerializedObject(list);
            so.FindProperty(CircularScrollRectField).objectReferenceValue = root.GetComponent<ScrollRect>();
            so.FindProperty(CircularContentField).objectReferenceValue = content;
            so.FindProperty(CircularItemPrefabField).objectReferenceValue = item;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        /// <summary>
        /// 创建无限不规则滚动列表模板
        /// <para>Create an infinite irregular scroll list template.</para>
        /// </summary>
        [MenuItem("GameObject/UI/EF/Scroll List Pro", false, 61)]
        static void CreateUIScrollListPro(MenuCommand menuCommand)
        {
            GameObject root = CreateScrollListRoot(menuCommand, "Scroll List Pro", typeof(ScrollListPro));
            ScrollListPro list = root.GetComponent<ScrollListPro>();
            if (list == null)
            {
                D.Error(LC.Tr(
                    "[ UiManager ] [ CreateUITemplatePrefab ] 无限滚动列表组件挂载失败",
                    "[ UiManager ] [ CreateUITemplatePrefab ] Failed to attach ScrollListPro component"));
                return;
            }
            RectTransform content = CreateScrollViewStructure(root.transform);

            GameObject item = CreateScrollItemTemplate(content);
            item.AddComponent<ScrollProItemBase>();
            // ScrollProItemBase 依赖 ContentSizeFitter 做自适应测量，占位模板预先挂好
            item.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            SerializedObject so = new SerializedObject(list);
            so.FindProperty(ProScrollRectField).objectReferenceValue = root.GetComponent<ScrollRect>();
            so.FindProperty(ProItemPrefabField).objectReferenceValue = item;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        /// <summary>
        /// 创建滚动列表根节点（ScrollRect + 列表组件）
        /// <para>Create the scroll list root with ScrollRect and list component.</para>
        /// </summary>
        private static GameObject CreateScrollListRoot(MenuCommand menuCommand, string name, Type listComponentType)
        {
            GameObject root = CreateUIObject(menuCommand, name,
                new[] { typeof(Image), typeof(ScrollRect), listComponentType });
            SetUIObjectSize(root.transform, new Vector2(200f, 300f));
            return root;
        }

        /// <summary>
        /// 构建 Viewport + Content 层级并完成 ScrollRect 引用，返回 Content
        /// <para>Build Viewport + Content hierarchy, wire up ScrollRect, return Content.</para>
        /// </summary>
        private static RectTransform CreateScrollViewStructure(Transform root)
        {
            GameObject viewport = new GameObject("Viewport", typeof(RectTransform), typeof(RectMask2D));
            viewport.transform.SetParent(root, false);
            RectTransform viewportRect = viewport.GetComponent<RectTransform>();
            viewportRect.anchorMin = Vector2.zero;
            viewportRect.anchorMax = Vector2.one;
            viewportRect.pivot = Vector2.one * 0.5f;
            viewportRect.sizeDelta = Vector2.zero;
            viewportRect.anchoredPosition = Vector2.zero;

            GameObject content = new GameObject("Content", typeof(RectTransform));
            content.transform.SetParent(viewport.transform, false);
            RectTransform contentRect = content.GetComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(0f, 1f);
            contentRect.anchorMax = Vector2.one;
            contentRect.pivot = new Vector2(0.5f, 1f);
            contentRect.sizeDelta = Vector2.zero;
            contentRect.anchoredPosition = Vector2.zero;

            ScrollRect scrollRect = root.GetComponent<ScrollRect>();
            scrollRect.viewport = viewportRect;
            scrollRect.content = contentRect;
            scrollRect.horizontal = false;
            scrollRect.vertical = true;
            return contentRect;
        }

        /// <summary>
        /// 创建占位 Item（默认隐藏，供列表克隆），返回其 GameObject
        /// <para>Create a placeholder item (inactive, for list cloning) and return it.</para>
        /// </summary>
        private static GameObject CreateScrollItemTemplate(RectTransform parent)
        {
            GameObject item = new GameObject("Item", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            item.transform.SetParent(parent, false);
            RectTransform rect = item.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0f, 1f);
            rect.sizeDelta = new Vector2(200f, 300f);
            rect.anchoredPosition = Vector2.zero;
            item.SetActive(false);
            return item;
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
                    "[ UiManager ] [ CreateUITemplatePrefab ] {0}: 挂载组件失败，绝逼是 API 变更!   {1}",
                    "[ UiManager ] [ CreateUITemplatePrefab ] {0}: failed to attach component, the Unity API must have changed!   {1}",
                    types[0].Name, e.Message));
                throw;
            }

            return go;
        }
    }
}
