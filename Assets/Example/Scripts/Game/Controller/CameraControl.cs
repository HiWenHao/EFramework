/* 
 * ================================================
 * Describe:      This script is used to controller the camera. 
 * Author:        Xiaohei.Wang(Wenhao)
 * CreationTime:  2023-02-21 15:41:25
 * ModifyAuthor:  Xiaohei.Wang(Wenhao)
 * ModifyTime:    2023-02-21 15:41:25
 * ScriptVersion: 0.1
 * ===============================================
*/
using System.Collections;
using System.Collections.Generic;
using EasyFramework;
using UnityEngine;
using XHTools;

namespace ExampleGame
{
    namespace Controller
    {
        /// <summary>
        /// The camera controller.
        /// 相机控制器
        /// </summary>
        public class CameraControl : MonoSingleton<CameraControl>, ISingleton
        {
            /// <summary>
            /// The camera lookat the target
            /// 追踪目标
            /// </summary>
            public Transform Target => m_Target;

            Vector3 m_Offset;
            Transform m_Target;

            void ISingleton.Init()
            {
                m_Offset = new Vector3(0f, 1.0f, -2.0f);
            }

            void ISingleton.Quit()
            {
                m_Target = null;
            }

            void FixedUpdate()
            {
                if (!m_Target)
                    return;

                transform.position = m_Target.position + m_Offset;

                transform.LookAt(m_Target);
            }

            #region Public function
            public void SetTarget(Transform target)
            {
                if (target == null)
                    m_Target = null;
                else
                    m_Target = target.Find("Eye");
            }
            #endregion
        }
    }
}
