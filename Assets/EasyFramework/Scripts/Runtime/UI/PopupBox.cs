/* 
 * ================================================
 * Describe:      This script is used to Show the popup info. 
 * Author:        Xiaohei.Wang(Wenhao)
 * CreationTime:  2022-10-18 18:02:45
 * ModifyAuthor:  Xiaohei.Wang(Wenhao)
 * ModifyTime:    2022-10-18 18:02:45
 * ScriptVersion: 0.1
 * ===============================================
*/
using UnityEngine;
using UnityEngine.UI;

namespace EasyFramework.UI
{
    /// <summary>
    /// Show the popup info.展示弹出提示.
    /// </summary>
    public class PopupBox
    {
        public string TextInfo
        {
            set
            {
                m_bgHorizontal.spacing -= .1f;
                m_Text.text = value;
                BoxObject.localPosition = Vector3.zero;
                m_timer = .7f;
                BoxObject.gameObject.SetActive(true);
                EF.Timer.AddCountdownEvent(0.04f, delegate
                {
                    m_bgHorizontal.spacing += .1f;
                });
            }
        }
        public Color TextColor
        {
            set
            {
                m_Text.color = value;
            }
        }
        public Color BackgroundColor
        {
            set
            {
                m_BG.color = value;
            }
        }

        public float Alpha
        {
            set
            {
                Color _co = m_BG.color;
                _co.a = value;
                m_BG.color = _co;
            }
        }

        public Transform BoxObject;
        Text m_Text;
        Image m_BG;
        HorizontalLayoutGroup m_bgHorizontal;

        float m_timer = .7f;
        public PopupBox(Transform obj)
        {
            BoxObject = obj;
            m_BG = obj.Find("BG").GetComponent<Image>();
            m_Text = m_BG.transform.Find("Text").GetComponent<Text>();
            m_bgHorizontal = m_BG.GetComponent<HorizontalLayoutGroup>();
        }

        public void Update()
        {
            if (!BoxObject.gameObject.activeSelf)
            {
                return;
            }
            if ((m_timer -= Time.deltaTime) <= 0.0f)
            {
                BoxObject.gameObject.SetActive(false);
            }
            BoxObject.position += Vector3.up * 500.0f * Time.deltaTime;
        }

        public void OnDestory()
        {
            m_BG = null;
            m_Text = null;
            m_bgHorizontal = null;
            Object.Destroy(BoxObject.gameObject);
        }
    }
}
