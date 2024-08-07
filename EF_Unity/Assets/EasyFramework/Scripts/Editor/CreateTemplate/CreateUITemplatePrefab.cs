/* 
 * ================================================
 * Describe:      This script is used to create template ui. 
 * Author:        Xiaohei.Wang(Wenhao)
 * CreationTime:  2022-10-27 14:55:09
 * ModifyAuthor:  Xiaohei.Wang(Wenhao)
 * ModifyTime:    2022-10-27 14:55:09
 * ScriptVersion: 0.1
 * ===============================================
*/
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace EasyFramework.Edit
{
    public class CreateUITemplatePrefab
    {
        [MenuItem("GameObject/UI/EF/New UI Page", false, 20)]
        static void CreateUINewUIPage(MenuCommand menuCommand)
        {
            CreateUIObject(menuCommand, "UiPage");
        }
        [MenuItem("GameObject/UI/EF/Button Pro", false, 40)]
        static void CreateUIButtonPro(MenuCommand menuCommand)
        {
            CreateUIObject(menuCommand, "ButtonPro");
        }
        [MenuItem("GameObject/UI/EF/Button Pro - TextMeshPro", false, 41)]
        static void CreateUIButtonProTmp(MenuCommand menuCommand)
        {
            CreateUIObject(menuCommand, "ButtonPro(TMP)"); 
        }
        [MenuItem("GameObject/UI/EF/Radar Map", false, 42)]
        static void CreateUIRadarMap(MenuCommand menuCommand)
        {
            CreateUIObject(menuCommand, "Radar Map");
        }
        [MenuItem("GameObject/UI/EF/Scroll View Pro", false, 43)]
        static void CreateUIScrollViewPro(MenuCommand menuCommand)
        {
            CreateUIObject(menuCommand, "Scroll View Pro");
        }
        [MenuItem("GameObject/UI/EF/Slideshow", false, 44)]
        static void CreateUISlideshow(MenuCommand menuCommand)
        {
            CreateUIObject(menuCommand, "Slideshow");
        }
        [MenuItem("GameObject/UI/EF/About Bind", false, 999)]
        static void CreateUIBind(MenuCommand menuCommand)
        {
            SettingsService.OpenProjectSettings("EF/Auto Bind Setting");
        }


        static GameObject CreateUIObject(MenuCommand menuCommand, string prefabName)
        {
            string fullPath = ProjectUtility.Path.FrameworkPath + $"EFAssets/UiTemplate/{prefabName}.prefab";
            GameObject prefab = (GameObject)AssetDatabase.LoadMainAssetAtPath(fullPath);
            if (prefab)
            {
                #region Check display conditions
                Canvas _canvas = Object.FindObjectOfType<Canvas>();
                if (!_canvas)
                {
                    _canvas = new GameObject("Canvas",typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster)).GetComponent<Canvas>();
                    _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                }
                if (!Object.FindObjectOfType<EventSystem>())
                {
                    new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
                }
                #endregion

                GameObject _inst = GameObject.Instantiate(prefab);
                _inst.name = prefabName;
                _inst.transform.SetParent(_canvas.transform, false);
                GameObjectUtility.SetParentAndAlign(_inst, menuCommand.context as GameObject);
                Undo.RegisterCreatedObjectUndo(_inst, $"Create {_inst.name}__{_inst.name}");
                Selection.activeObject = _inst;
                AssetDatabase.Refresh();
                AssetDatabase.SaveAssets();
                return _inst;
            }
            return null;
        }
    }
}
