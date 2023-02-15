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
    public class UiBind : MonoBehaviour
    {
        [Serializable]
        public class BindData
        {
            public BindData()
            {
            }

            public BindData(string name, Component bindCom)
            {
                Name = name;
                BindCom = bindCom;
            }

            public string Name;
            public Component BindCom;
        }

        public List<BindData> BindDatas = new List<BindData>();
        public string Namespace => m_Namespace;
        public string ComCodePath => m_ComCodePath;

        [SerializeField]
        public List<Component> m_BindComs = new List<Component>();
        [SerializeField]
        private string m_Namespace;

        [SerializeField]
        private string m_ComCodePath;
	}
}
