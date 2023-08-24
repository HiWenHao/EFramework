/*
 * ================================================
 * Describe:      This script is used to .
 * Author:        Xiaohei.Wang(Wenhao)
 * CreationTime:  2023-08-23 17:27:09
 * ModifyAuthor:  Xiaohei.Wang(Wenhao)
 * ModifyTime:    2023-08-23 17:27:09
 * ScriptVersion: 0.1
 * ===============================================
*/
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using EasyFramework;

namespace AimGame
{
    /// <summary>
    /// Control the model moving and other operation.控制人物模型所有操作
    /// </summary>
    public class PlayerControl : MonoBehaviour
	{
		[SerializeField, Header("人物移动速度")]
		float MoveSpeed;
        [SerializeField, Header("人物跳起速度")]
        float JumpSpeed = 5;

        float radius;

        bool inMoving;

        Vector3 pointBottom, pointTop;

        Animator ani;
        Rigidbody rig;
        CapsuleCollider capsuleCollider;

        void Start()
		{
            ani = GetComponent<Animator>();
            rig = GetComponent<Rigidbody>();

            capsuleCollider = GetComponent<CapsuleCollider>();

            radius = 0.35f;

            D.Correct(LayerMask.GetMask("Player"));
        }
		
		void Update()
		{
			float _h = Input.GetAxis("Horizontal") * MoveSpeed;
			float _v = Input.GetAxis("Vertical") * MoveSpeed;

			if (Mathf.Abs(_h) > 0.1f || Mathf.Abs(_v) > 0.1f)
            {
                if (!inMoving)
                {
					inMoving = true;
					ani.CrossFade("HumanMove", 0.15f);
                }
            }
			else
            {
                if (inMoving)
                {
                    inMoving = false;
                    ani.CrossFade("Idle", 0.15f);
                }
            }
            
            if (OnGround() && Input.GetKeyDown(KeyCode.Space))
                rig.AddForce(transform.up * JumpSpeed, ForceMode.Impulse);

            transform.position += MoveSpeed * Time.deltaTime * (transform.forward * _v + transform.right * _h);
        }
		
		void OnDestroy()
		{
            ani = null;
        }


        bool OnGround()
        {
            pointBottom = transform.position + transform.up * radius - transform.up * 3f;
            pointTop = transform.position + transform.up * capsuleCollider.height - transform.up * 0.9f;

            Collider[] colliders = Physics.OverlapCapsule(pointTop, pointBottom, radius, ~(1 << 6));
            Debug.DrawLine(pointTop, pointBottom, Color.red, 1f);
            return colliders.Length != 0;
        }
    }
}
