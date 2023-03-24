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
        [MenuItem("GameObject/UI/Button Pro", false, 20)]
        static void CreateUIButtonPro(MenuCommand menuCommand)
        {
            CreateUIObject(menuCommand, "ButtonPro");
        }
        [MenuItem("GameObject/UI/Button Pro - TextMeshPro", false, 21)]
        static void CreateUIButtonProTmp(MenuCommand menuCommand)
        {
            CreateUIObject(menuCommand, "ButtonPro(TMP)"); 
        }
        [MenuItem("GameObject/UI/Radar Map", false, 22)]
        static void CreateUIRadarMap(MenuCommand menuCommand)
        {
            CreateUIObject(menuCommand, "Radar Map");
        }
        [MenuItem("GameObject/UI/Scroll View Pro", false, 23)]
        static void CreateUIScrollViewPro(MenuCommand menuCommand)
        {
            CreateUIObject(menuCommand, "Scroll View Pro");
        }


        static GameObject CreateUIObject(MenuCommand menuCommand, string prefabName)
        {
            string fullPath = ($"Assets/EasyFramework/Editor/Create/UiTemplate/Template/{prefabName}.prefab");
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

                GameObject _inst = (GameObject)PrefabUtility.InstantiateAttachedAsset(prefab);
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
