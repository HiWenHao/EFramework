/*
 * ================================================
 * Describe:      This script is used to .
 * Author:        Alvin5100
 * CreationTime:  2026-05-13 16:16:01
 * ModifyAuthor:  Alvin5100
 * ModifyTime:    2026-05-13 16:16:01
 * ScriptVersion: 0.1
 * ===============================================
 */

using UnityEngine;
using UnityEngine.UI;
using EasyFramework.Systems.RedDot;

namespace EasyFramework.Test.RedDot
{
    public class RedDotTestUI : MonoBehaviour
    {
        public Button mailButton;
        public Button taskButton;
        public Button friendButton;
        public Button resetButton;

        public Button mail0Button;
        public Button mail1Button;
        public Button mail2Button;
        public Button mail3Button;
        public Button mail4Button;

        private void Start()
        {
            mailButton.onClick.AddListener(OnMailClick);
            taskButton.onClick.AddListener(OnTaskClick);
            friendButton.onClick.AddListener(OnFriendClick);

            #region Mails
            resetButton.onClick.AddListener(OnResetClick);
            mail0Button.onClick.AddListener(OnMail0Click);
            mail1Button.onClick.AddListener(OnMail1Click);
            mail2Button.onClick.AddListener(OnMail2Click);
            mail3Button.onClick.AddListener(OnMail3Click);
            mail4Button.onClick.AddListener(OnMail4Click);
            #endregion
        }

        void OnMailClick()
        {
            var node = RedDotSystem.Instance.GetNode("MailRoot");
            node?.SetNumber(node.Number + 1);
        }

        void OnTaskClick() => OnButtonClick("TaskRoot");
        void OnFriendClick() => OnButtonClick("FriendRoot");

        #region Mails
        void OnMail0Click() => OnButtonClick("Mail - 0");
        void OnMail1Click() => OnButtonClick("Mail - 1");
        void OnMail2Click() => OnButtonClick("Mail - 2");
        void OnMail3Click() => OnButtonClick("Mail - 3");
        void OnMail4Click() => OnButtonClick("Mail - 4");

        #endregion

        void OnButtonClick(string key)
        {
            var node = RedDotSystem.Instance.GetNode(key);
            node?.SetNumber(node.Number == 0 ? 1 : 0);
        }
        void OnResetClick()
        {
            // 批量重置所有数值，使用批处理避免多次刷新
            RedDotSystem.Instance.BeginBatch();
            RedDotSystem.Instance.GetNode("MailRoot")?.SetNumber(0);
            RedDotSystem.Instance.GetNode("TaskRoot")?.SetNumber(0);
            RedDotSystem.Instance.GetNode("FriendRoot")?.SetNumber(0);
            
            RedDotSystem.Instance.GetNode("Mail - 0")?.SetNumber(0);
            RedDotSystem.Instance.GetNode("Mail - 1")?.SetNumber(0);
            RedDotSystem.Instance.GetNode("Mail - 2")?.SetNumber(0);
            RedDotSystem.Instance.GetNode("Mail - 3")?.SetNumber(0);
            RedDotSystem.Instance.GetNode("Mail - 4")?.SetNumber(0);
            RedDotSystem.Instance.EndBatch();
            D.Log("所有红点已重置");
        }
    }
}