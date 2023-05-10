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

namespace EasyFramework.Edit.TaskList
{
    [CreateAssetMenu(fileName = "TodoList", menuName = "EF/Todo List", order = 300)]
    public class TaskListConfig : ScriptableObject
    {
#pragma warning disable 0414
        [SerializeField]
        int TaskCount = 0;
#pragma warning disable 0414
        [SerializeField]
        bool[] Enabled = new bool[] { };
        [SerializeField]
        int[] Progress = new int[] { };
        [SerializeField]
        string[] Title = new string[] { };
        [SerializeField]
        string[] Description = new string[] { };
    }
}
