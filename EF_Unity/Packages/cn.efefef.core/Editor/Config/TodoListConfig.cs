/* 
 * ================================================
 * Describe:      This script is used to .
 * Author:        Xiaohei.Wang(Wenhao)
 * CreationTime:  2023-05-10 15:32:46
 * ModifyAuthor:  Xiaohei.Wang(Wenhao)
 * ModifyTime:    2023-05-10 15:32:46
 * ScriptVersion: 0.1
 * ===============================================
*/
using UnityEngine;

namespace EasyFramework.Edit.TodoList
{
    public class TodoListConfig : ScriptableObject
    {
#pragma warning disable 0414
        [SerializeField]
        int TaskCount = 0;
        [SerializeField]
        bool[] Mark = new bool[] { };
        [SerializeField]
        bool[] Enabled = new bool[] { };
        [SerializeField]
        int[] Progress = new int[] { };
        [SerializeField]
        string[] Title = new string[] { };
        [SerializeField]
        string[] Description = new string[] { };
#pragma warning disable 0414
    }
}
