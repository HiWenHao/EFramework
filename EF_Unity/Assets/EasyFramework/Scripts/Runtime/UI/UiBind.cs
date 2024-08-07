/* 
 * ================================================
 * Describe:      This script is used to building component current game object scripts. 
 * Author:        Xiaohei.Wang(Wenhao)
 * CreationTime:  2023-02-13 16:32:16
 * ModifyAuthor:  Xiaohei.Wang(Wenhao)
 * ModifyTime:    2023-02-13 16:32:16
 * ScriptVersion: 0.1
 * ===============================================
*/
using System;
using System.Collections.Generic;
using UnityEngine;

namespace EasyFramework.UI
{
    [DisallowMultipleComponent]
    [AddComponentMenu("UI/UiBind", 100)]
    public class UiBind : MonoBehaviour
    {
        private UiBind() { }

        [Serializable]
        public class BindData
        {
            public string RealName;
            public string ScriptName;
            public Component BindCom;
        }

        public List<BindData> BindDatas = new List<BindData>();
        public string Namespace => m_Namespace;
        public string ComCodePath => m_ComCodePath;
        public List<Component> m_BindComs = new List<Component>();

        [SerializeField]
        private string m_Namespace;

        [SerializeField]
        private string m_ComCodePath;

        [SerializeField]
        private bool m_DeleteScript;

        [SerializeField]
        private bool m_CreatePrefab;

        [SerializeField]
        private string m_PrefabPath;

        [SerializeField]
        private bool m_PackUpBindList;

        [SerializeField]
        private bool m_SortByType;

        [SerializeField]
        private bool m_SortByNameLength;
    }
}
