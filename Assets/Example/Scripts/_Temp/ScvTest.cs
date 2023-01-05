/* 
 * ================================================
 * Describe:      This script is used to  . 
 * Author:        Xiaohei.Wang(Wenhao)
 * CreationTime:  2022-10-27 16:42:50
 * ModifyAuthor:  Xiaohei.Wang(Wenhao)
 * ModifyTime:    2022-10-27 16:42:50
 * ScriptVersion: 0.1
 * ===============================================
*/
using EasyFramework.UI;
using UnityEngine;
using UnityEngine.UI;

namespace PleaseChangeTheNamespace
{
    /// <summary>
    /// Please modify the description。
    /// </summary>
    public class ScvTest : MonoBehaviour
	{
        ScrollViewPro infinityGridLayoutGroup;

        void Start()
        {
            //初始化数据列表;
            infinityGridLayoutGroup = FindObjectOfType<ScrollViewPro>();

            infinityGridLayoutGroup.updateChildrenCallback = UpdateChildrenCallback;
            for (int i = 0; i < infinityGridLayoutGroup.transform.childCount; i++)
            {
                Transform child = infinityGridLayoutGroup.transform.GetChild(i);
                child.GetComponent<Button>().onClick.AddListener(() =>
                {
                    OnClickButtonWithIndex(child.GetComponentInChildren<Text>());
                });
            }

            infinityGridLayoutGroup.InitSetAmount(1000);
        }

        void OnClickButtonWithIndex(Text tex)
        {
            Debug.Log($" Unity log:     index is {tex.text} in your click button...");
        }

        /// <summary>
        /// 上下翻滚更新函数
        /// </summary>
        void UpdateChildrenCallback(int indx, Transform trans)
        {
            Text tex = trans.Find("Text").GetComponent<Text>();
            tex.text = indx.ToString();
        }
    }
}
