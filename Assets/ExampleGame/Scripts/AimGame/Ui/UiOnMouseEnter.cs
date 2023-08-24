/*
 * ================================================
 * Describe:      This script is used to .
 * Author:        Xiaohei.Wang(Wenhao)
 * CreationTime:  2023-08-24 17:08:45
 * ModifyAuthor:  Xiaohei.Wang(Wenhao)
 * ModifyTime:    2023-08-24 17:08:45
 * ScriptVersion: 0.1
 * ===============================================
*/
using UnityEngine;
using UnityEngine.EventSystems;

namespace AimGame
{
    /// <summary>
    /// Please modify the description。
    /// </summary>
    public class UiOnMouseEnter : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
	{
        void IPointerEnterHandler.OnPointerEnter(PointerEventData eventData)
        {
            EF.Event.CallEvent("MouseEnter", transform.GetSiblingIndex(), true);
        }

        void IPointerExitHandler.OnPointerExit(PointerEventData eventData)
        {
            EF.Event.CallEvent("MouseEnter", transform.GetSiblingIndex(), false);
        }
    }
}
