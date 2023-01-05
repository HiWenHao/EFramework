/* 
 * ================================================
 * Describe:         This script is used to get global input event.
 * Author:           Xiaohei.Wang(Wenhao)
 * CreationTime:     2022-06-20 10:26:57
 * ModifyAuthor:     Xiaohei.Wang(Wenhao)
 * ModifyTime:       2022-06-20 10:26:57
 * Version:          0.1 
 * ===============================================
 */
using EasyFramework.Framework.Core;
using UnityEngine;

namespace EasyFramework.Managers
{
    /// <summary>
    /// Get global keyboard input event. 获取全局键盘输入事件
    /// </summary>
    public class InputManager : MonoSingleton<InputManager>, ISingleton
	{
		public bool Key_LeftMouse;
		public bool Key_RightMouse;
		public bool Key_Jump;
		public bool Key_Power;
		public bool Key_Crouch;
		public float Speed_Right;
		public float Speed_Forward;
		public float Speed_Magnitude;
		public float Value_MouseX;
		public float Value_MouseY;
		public Vector3 Magnitude;

        void ISingleton.Init()
        {

        }

        void ISingleton.Quit()
        {
            #region Key
            Key_Jump = false;
            Key_Power = false;
            Key_Crouch = false;

            #endregion

            #region Speed
            Speed_Right = 0.0f;
            Speed_Forward = 0.0f;

            #endregion
        }

        private Vector3 squaretocircle(float x, float y)
		{
			Vector3 _outInput = Vector3.zero;
			_outInput.x = x * Mathf.Sqrt(1 - (y * y) / 2.0f);
			_outInput.z = y * Mathf.Sqrt(1 - (x * x) / 2.0f);
			return _outInput;
		}

		void Update()
		{
			#region Key
			Key_Jump = Input.GetKeyDown(KeyCode.Space);
			Key_Power = Input.GetKey(KeyCode.LeftShift);
			Key_Crouch = Input.GetKey(KeyCode.LeftControl);
			Key_LeftMouse = Input.GetMouseButton(0);
			Key_RightMouse = Input.GetMouseButton(1);
			#endregion

			#region Speed
			Speed_Right = Input.GetAxis("Horizontal");
			Speed_Forward = Input.GetAxis("Vertical");

			Speed_Magnitude = Mathf.Clamp01(Speed_Right + Mathf.Abs(Speed_Forward));
			Magnitude = squaretocircle(Speed_Right, Speed_Forward);
			#endregion

			#region Value
			Value_MouseX = Input.GetAxis("Mouse X");
			Value_MouseY = Input.GetAxis("Mouse Y");
			#endregion
		}
    }
}
