/* 
 * ================================================
 * Describe:         This script is used to  control player entity
 * Author:           Xiaohei.Wang(Wenhao)
 * CreationTime:     2022-06-20 10:20:34
 * ModifyAuthor:     Xiaohei.Wang(Wenhao)
 * ModifyTime:       2022-06-20 10:20:34
 * Version:          0.1 
 * ===============================================
 */
using ExampleGame.Configs;
using UnityEngine;
using XHTools;

namespace ExampleGame
{
    namespace Controller
    {
        /// <summary>
        /// control player entity. 控制主角物体。
        /// </summary>
        [RequireComponent(typeof(Rigidbody))]
        [RequireComponent(typeof(CapsuleCollider))]
        public class PlayerController : MonoBehaviour
        {
            //int m_int_GoundLayer = 10;
            float m_flt_SpeedRate = 1;
            float m_flt_RotateSpeed = 2.0f;
            bool m_bol_InGround;
            bool m_bol_InFirstJumping;

            public float ForwarSpeed = 5f;
            public float BackSpeed = 3f;
            public float HorizonSpeed = 4f;

            public float RunValue = 2f;
            public float JumpForce = 50f;

            PlayerConfig m_Config;
            Rigidbody m_rigidbody;
            Animator m_animator;
            //爬坡的速度曲线
            public AnimationCurve SlopCurve;

            //地面法线向量
            private Vector3 curGroundNormal;
            void Start()
            {
                m_animator = GetComponent<Animator>();
                m_rigidbody = GetComponent<Rigidbody>();
                m_rigidbody.mass = 9.8f;
                m_rigidbody.constraints = RigidbodyConstraints.FreezeRotation;
            }

            private Vector3 Squaretocircle(Vector3 vector3)
            {
                Vector3 _outInput = Vector3.zero;
                _outInput.x = vector3.x * Mathf.Sqrt(1 - (vector3.z * vector3.z) / 2.0f);
                _outInput.z = vector3.z * Mathf.Sqrt(1 - (vector3.x * vector3.x) / 2.0f);
                return _outInput;
            }

            //检测地面
            void CheckGround()
            {
                if (Physics.Raycast(transform.position, transform.position + Vector3.down * .001f, float.MaxValue, 1 << 10))
                {
                    m_bol_InGround = true;
                }
                else
                {
                    m_bol_InGround = false;
                }

                Debug.DrawLine(transform.position, transform.position + Vector3.down * .01f, Color.green, .1f);
            }

            void Jumping()
            {
                if (m_bol_InGround)
                {
                    m_bol_InFirstJumping = true;
                    m_rigidbody.drag = 5f;
                    JumpToUp();
                    m_animator.SetTrigger("IsJump");
                }
                else
                {
                    if (m_bol_InFirstJumping)
                    {
                        m_bol_InFirstJumping = false;
                        JumpToUp();
                    }
                }
            }
            void JumpToUp()
            {
                m_rigidbody.drag = 0f;
                //把刚体的上下方向的速度先归零
                m_rigidbody.velocity = new Vector3(m_rigidbody.velocity.x, 0f, m_rigidbody.velocity.z);
                m_rigidbody.AddForce(new Vector3(0, JumpForce, 0), ForceMode.Impulse);
            }

            void FixedUpdate()
            {
                CheckGround();
                if (EF.Input.Key_Jump)
                    Jumping();
                if (!m_bol_InGround)
                    return;
                #region Move
                if (EF.Input.Key_Power)
                    m_flt_SpeedRate = 2.0f;
                else if (Input.GetKey(KeyCode.B))
                    m_flt_SpeedRate = 3.0f;
                else
                    m_flt_SpeedRate = 1.0f;

                var ss = m_Config.MoveSpeed * m_flt_SpeedRate * Squaretocircle(transform.forward * EF.Input.Speed_Forward + transform.right * EF.Input.Speed_Right);
                if (EF.Input.Speed_Forward < 0.0f)
                    ss *= 0.5f;
                if (EF.Input.Speed_Forward == 0.0f && EF.Input.Speed_Right != 0.0f)
                    ss *= 0.5f;
                m_rigidbody.AddForce(ss, ForceMode.Force);

                m_animator.SetFloat("MoveHorizontal", EF.Input.Speed_Right * m_flt_SpeedRate);
                m_animator.SetFloat("MoveVertical", EF.Input.Speed_Forward * m_flt_SpeedRate);
                #endregion

                #region Rotate
                if (EF.Input.Key_LeftMouse)
                    transform.Rotate(transform.up, EF.Input.Value_MouseX * m_flt_RotateSpeed * m_flt_SpeedRate);

                #endregion
            }


            #region Public function
            public void SetConfig(PlayerConfig config)
            {
                m_Config = config;
            }
            #endregion
        }
    }
}
