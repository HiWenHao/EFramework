/*
 * ================================================
 * Describe:      This script is used to .
 * Author:        Alvin5100
 * CreationTime:  2026-05-13 16:44:44
 * ModifyAuthor:  Alvin5100
 * ModifyTime:    2026-05-13 16:44:44
 * ScriptVersion: 0.1
 * ===============================================
 */

using UnityEngine;
using EasyFramework.Managers.RedDot;

namespace EasyFramework.Test.RedDot
{
    public class RedDotTreeSetup : MonoBehaviour
    {
        [Header("UI 引用 (自动查找, 也可手动拖拽)")]
        public GameObject mailNumberText;
        public GameObject taskRedDotImage;
        public GameObject friendIconImage;
        public GameObject rootRedDotImage;

        private void Start()
        {
            // 1. 注册树形结构
            RegisterTree();

            // 2. 动态设置渲染器和key， 当然如果你已经在场景中设置过了，则可以直接跳过本步骤
            //  SetupTaskUI();

            D.Log("红点树初始化完成，开始测试...");
        }

        private void RegisterTree()
        {
            // 根节点（Dot类型，聚合子节点）
            RedDotManager.Instance.RegisterNode("Root");
            // 任务节点（纯红点）
            RedDotManager.Instance.RegisterNode("TaskRoot", "Root");
            // 邮件节点（数字）
            RedDotManager.Instance.RegisterNode("MailRoot", "Root", RedDotDisplayType.Number);
            // 好友节点（图片）
            RedDotManager.Instance.RegisterNode("FriendRoot", "Root", RedDotDisplayType.Image, "FriendRequestIcon");
            
            RedDotManager.Instance.RegisterNode("Mail - 0", "MailRoot");
            RedDotManager.Instance.RegisterNode("Mail - 1", "MailRoot");
            RedDotManager.Instance.RegisterNode("Mail - 2", "MailRoot");
            RedDotManager.Instance.RegisterNode("Mail - 3", "MailRoot");
            RedDotManager.Instance.RegisterNode("Mail - 4", "MailRoot");
        }
        
        private void SetupTaskUI()
        {
            if (taskRedDotImage == null)
                taskRedDotImage = GameObject.Find("TaskPanel/RedDotImageTask");
            if (taskRedDotImage == null) return;

            var dotRenderer = taskRedDotImage.GetComponent<DotRenderer>();
            if (dotRenderer == null) dotRenderer = taskRedDotImage.AddComponent<DotRenderer>();

            var view = taskRedDotImage.GetComponent<RedDotView>();
            if (view == null) view = taskRedDotImage.AddComponent<RedDotView>();

            view.SetKey("TaskRoot");
            view.SetDotRenderer(dotRenderer);
        }
    }
}