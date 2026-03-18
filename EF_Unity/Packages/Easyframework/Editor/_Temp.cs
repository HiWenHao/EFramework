/* 
 * ================================================
 * Describe:      This script is used to .
 * Author:        Xiaohei.Wang(Wenhao)
 * CreationTime:  2023-05-15 19:00:53
 * ModifyAuthor:  Xiaohei.Wang(Wenhao)
 * ModifyTime:    2023-05-15 19:00:53
 * ScriptVersion: 0.1
 * ===============================================
*/
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using EasyFramework;
using UnityEditor;

namespace EFExample
{
	/// <summary>
	/// Please modify the description。
	/// </summary>
	public class _Temp
	{

    }



public class 源生自定义菜单
    {
        //[InitializeOnLoadMethod]
        static void InitializeOnLoadMethod()
        {
            SceneView.duringSceneGui += delegate (SceneView sceneView)
            {
                Event e = Event.current;
                //鼠标右键抬起时
                if (e != null && e.button == 1 && e.type == EventType.MouseUp)
                {
                    Vector2 mousePosition = e.mousePosition;
                    //设置菜单项
                    var options = new GUIContent[]
                    {
                    new GUIContent("Test1"),
                    new GUIContent(""),
                    new GUIContent("Test2"),
                    new GUIContent(""),
                    new GUIContent("Test/Test3"),
                    new GUIContent("Test/Test4")
                    };
                    //设置菜单显示区域
                    var selected = -1;
                    var userData = Selection.activeGameObject;
                    var width = 100;
                    var height = 100;
                    var position = new Rect(mousePosition.x, mousePosition.y - height,
                        width, height);
                    //显示菜单
                    EditorUtility.DisplayCustomMenu(position, options, selected,
                        delegate (object data, string[] opt, int select)
                        {
                            Debug.Log(opt[select]);
                        }, userData
                    );
                    e.Use();
                }
            };
        }
    }
}
