/*
 * ================================================
 * Describe:      This script is used to .
 * Author:        Alvin5100
 * CreationTime:  2026-05-12 14:51:32
 * ModifyAuthor:  Alvin5100
 * ModifyTime:    2026-05-12 14:51:32
 * ScriptVersion: 0.1
 * ===============================================
 */

using System.Collections;
using EasyFramework;
using UnityEngine;
using UnityEngine.Networking;

namespace EFExample
{
	public class FeishuNotifier : MonoBehaviour
	{
		// 粘贴你在上一步中获取的 Webhook 地址
		[SerializeField] private string _feishuWebhookUrl = "https://open.feishu.cn/open-apis/bot/v2/hook/你的";
		[SerializeField] private string _sendMessage = "";
		[ContextMenu("Test_发送飞书消息")]
		public void SendMessageToFeishu()
		{
			string messageContent = "[ 骚鹏Robot ]: " + _sendMessage;
			StartCoroutine(PostToFeishu(messageContent));
		}

		[ContextMenu("Test_发送带@的通知")]
		public void SendAtNotificationToFeishu()
		{
			// 示例消息，包含 @所有人
			string messageContent = "[ 骚鹏Robot ]: <at user_id=\"all\"></at> " + _sendMessage;
			StartCoroutine(PostToFeishu(messageContent));
		}

		private IEnumerator PostToFeishu(string textContent)
		{
			// 构建请求体
			var message = new FeishuMessage
			{
				msg_type = "text",
				content = new TextContent { text = textContent }
			};
			string jsonData = JsonUtility.ToJson(message);

			using (UnityWebRequest request = new UnityWebRequest(_feishuWebhookUrl, "POST"))
			{
				byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(jsonData);
				request.uploadHandler = new UploadHandlerRaw(bodyRaw);
				request.downloadHandler = new DownloadHandlerBuffer();
				request.SetRequestHeader("Content-Type", "application/json");

				yield return request.SendWebRequest();

				if (request.result == UnityWebRequest.Result.Success)
					D.Log($"消息发送成功: {request.downloadHandler.text}");
				else
					D.Error($"消息发送失败: {request.error}");
			}
		}

		// 用于将消息格式化为 JSON 的辅助类
		[System.Serializable]
		private class FeishuMessage
		{
			public string msg_type;
			public TextContent content;
		}

		[System.Serializable]
		private class TextContent
		{
			public string text;
		}
	}
}
