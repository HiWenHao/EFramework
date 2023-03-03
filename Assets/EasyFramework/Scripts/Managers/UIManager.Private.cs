/* 
 * ================================================
 * Describe:      The class is ui page manager
 * Author:        Xiaohei.Wang(Wenhao)
 * CreationTime:  2023-03-02 18:00:58
 * ModifyAuthor:  Xiaohei.Wang(Wenhao)
 * ModifyTime:    2023-03-02 18:00:58
 * ScriptVersion: 0.1
 * ===============================================
*/

using EasyFramework.UI;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;

namespace EasyFramework.Managers
{
    public partial class UIManager : Singleton<UIManager>, IManager, IUpdate
    {
        int m_int_Serial;
        GameObject m_CurrentObj;
        UIPageBase m_CurrentPage;

        Stack<UIPageBase> m_stc_UseUI;
        Stack<GameObject> m_stc_UseGO;

        Dictionary<int, GameObject> m_dic_ReadyGO;

        void Init()
        {
            m_stc_UseUI = new Stack<UIPageBase>();
            m_stc_UseGO = new Stack<GameObject>();
            m_dic_ReadyGO = new Dictionary<int, GameObject>();
        }
        void Exit()
        {
            while (m_stc_UseUI.Count != 0)
            {
                m_stc_UseUI.Pop().Quit();
                Destroy(m_stc_UseGO.Pop());
            }

            var _keys = m_dic_ReadyGO.Keys;
            foreach (int key in _keys) 
            {
                m_dic_ReadyGO.Remove(key);
            }
            m_dic_ReadyGO.Clear();
            m_dic_ReadyGO = null;

            m_stc_UseUI.Clear();
            m_stc_UseGO.Clear();
            m_stc_UseUI = null;
            m_stc_UseGO = null;
        }

        private GameObject Created(UIPageBase page)
        {
            GameObject _uiObj = Object.Instantiate(EF.Load.Load<GameObject>($"{AppConst.UI}{page.GetType().Name}"));
            _uiObj.transform.SetParent(pageBaseObject);
            RectTransform _rect = _uiObj.GetComponent<RectTransform>();
            _rect.sizeDelta = Vector2.zero;
            _rect.localPosition = Vector2.zero;
            _rect.localScale = Vector2.one;
            _uiObj.SetActive(true);
            page.SerialId = m_int_Serial++;
            return _uiObj;
        }
        private void Open(UIPageBase page, bool hideCurrent, params object[] args)
        {
            if (null != m_CurrentPage)
            {
                m_CurrentPage.OnFocus(false);
                m_CurrentObj.SetActive(!hideCurrent);
            }

            bool _hasReady = false;
            int _readyKey = -1;
            foreach (var item in m_dic_ReadyGO)
            {
                if (item.Value.name == page.GetType().Name)
                {
                    _hasReady = true;
                    _readyKey = item.Key;
                    page.SerialId = item.Key;
                    m_CurrentObj = item.Value;
                    break;
                }
            }
            if (!_hasReady)
            {
                m_CurrentObj = Created(page);
                m_CurrentPage.Awake(m_CurrentObj, args);
            }
            else
                m_dic_ReadyGO.Remove(_readyKey);

            m_CurrentPage = page;

            m_stc_UseGO.Push(m_CurrentObj);
            m_stc_UseUI.Push(m_CurrentPage);

            m_CurrentPage.Open(args);
        }
        private void Update()
        {
            if (null != m_CurrentPage)
                m_CurrentPage?.Update();
        }
        private void Close(bool destroy = false, params object[] args)
        {
            UIPageBase _ui = m_stc_UseUI.Pop();
            GameObject _go = m_stc_UseGO.Pop();

            _ui.Close();
            if (destroy)
            {
                _ui.Quit();
                Destroy(_go);
            }
            else
            {
                _go.SetActive(false);
                m_dic_ReadyGO.Add(_ui.SerialId, _go);
            }

            m_CurrentPage = null;

            m_CurrentObj = m_stc_UseGO.Peek();
            m_CurrentPage = m_stc_UseUI.Peek();

            m_CurrentObj.SetActive(true);
            m_CurrentPage.OnFocus(true, args);
        }
    }
}
