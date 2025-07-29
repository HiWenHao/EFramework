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
        public string Namespace => _namespace;
        public string ComCodePath => _comCodePath;
        public List<Component> BindComs = new List<Component>();

        [SerializeField]
        private string _namespace;

        [SerializeField]
        private string _comCodePath;

        [SerializeField]
        private bool _deleteScript;

        [SerializeField]
        private bool _createPrefab;

        [SerializeField]
        private string _prefabPath;

        [SerializeField]
        private bool _packUpBindList;

        [SerializeField]
        private bool _sortByType;

        [SerializeField]
        private bool _sortByNameLength;
    }
}
