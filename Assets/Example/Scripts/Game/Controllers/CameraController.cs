/* 
 * ================================================
 * Describe:         This script is used to control camera look at player.
 * Author:           Xiaohei.Wang(Wenhao)
 * CreationTime:     2022-06-20 14:16:32
 * ModifyAuthor:     Xiaohei.Wang(Wenhao)
 * ModifyTime:       2022-06-20 14:16:32
 * Version:          0.1 
 * ===============================================
 */
using Cinemachine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using XHTools;

namespace PleaseChangeTheNamespace
{
	/// <summary>
	/// control camera look at player. 控制相机看向玩家.
	/// </summary>
	public class CameraController : MonoBehaviour
	{
		Transform m_CameraTransform;

		Transform player;
		CinemachineVirtualCamera m_VirtualCamera;
		void Start()
		{
			m_CameraTransform = GameObject.Find("====================Camera").transform;
			player = GameObject.FindWithTag("Player").transform;
			if (null == player) player = GameObject.Find("Player").transform;
			if (null == player) D.Warning("There are not player object in scene.场景中不存在玩家物体.");

			m_VirtualCamera = m_CameraTransform.Find("VirtualCamera").GetComponent<CinemachineVirtualCamera>();

			m_VirtualCamera.LookAt = player.Find("Eye");
			m_VirtualCamera.m_Follow = player.Find("Eye");
		}
		
		void OnDestroy()
		{
			m_VirtualCamera = null;
			player = null;
			m_CameraTransform = null;
		}
	}
}
