/*
 * ================================================
 * Describe:      This script is used to .
 * Author:        Xiaohei.Wang(Wenhao)
 * CreationTime:  2023-08-18 16:42:37
 * ModifyAuthor:  Xiaohei.Wang(Wenhao)
 * ModifyTime:    2023-08-18 16:42:37
 * ScriptVersion: 0.1
 * ===============================================
*/
using EasyFramework;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;

namespace AimGame
{
    /// <summary>
    /// Please modify the description。
    /// </summary>
    public class AimGameController : MonoSingleton<AimGameController>, ISingleton
    {
        public bool InPause;


        bool m_MouseLeftDown;

        float m_RotateSpeed = 1.0f;
        float bulletMoveSpeed = 100.0f;

        Vector2 m_CamerRos;

        Transform m_CameraTran, humanModel;

        Gun m_Gun;

        Transform bullet, bulletHole, bulletAll;
        List<Transform> bulletMoving;
        ObjectPool<Transform> bulletPool, bulletHolePool;

        void ISingleton.Init()
        {
            m_CameraTran = Camera.main.transform;

            m_CamerRos = new Vector2();

            AimGameConfig.Instance.MouseChanged += speed =>
            {
                m_RotateSpeed = speed;
            };
            m_RotateSpeed = AimGameConfig.Instance.MouseSpeed;

            m_Gun = new Standard();

            bulletAll = new GameObject("bulletAll").transform;
            bulletMoving = new List<Transform>();
            bullet = EF.Load.LoadInResources<GameObject>("Prefabs/AimGame/Bullet").transform;
            bulletPool = new ObjectPool<Transform>(OnCreateBullet, OnGetBullet, OnReleaseBullet);
            bulletHole = EF.Load.LoadInResources<GameObject>("Prefabs/AimGame/BulletHole").transform;
            bulletHolePool = new ObjectPool<Transform>(OnCreateBulletHole, OnGetBulletHole, OnReleaseBulletHole);


            humanModel = Instantiate(EF.Load.LoadInResources<GameObject>("Prefabs/AimGame/Human")).transform;
            m_CameraTran.SetParent(humanModel);
            m_CameraTran.position = humanModel.position + Vector3.up * 0.5f;
        }

        void Update()
        {
            if (InPause)
                return;

            m_CamerRos.y += Input.GetAxis("Mouse X") * m_RotateSpeed;
            m_CamerRos.x -= Input.GetAxis("Mouse Y") * m_RotateSpeed;
            m_CamerRos.x = Mathf.Clamp(m_CamerRos.x, -89.9f, 89.9f);
            m_CameraTran.eulerAngles = m_CamerRos;

            Ray _ray = new Ray(m_CameraTran.position, m_CameraTran.forward);
            bool _rayBol = Physics.Raycast(_ray, out RaycastHit hitInfo);

            if (Input.GetMouseButtonDown(0))
                m_MouseLeftDown = true;
            if (Input.GetMouseButtonUp(0))
                m_MouseLeftDown = false;

            if (_rayBol && m_MouseLeftDown)
            {
                switch (m_Gun.FireType)
                {
                    case BFireType.Single:
                        OpenFier(hitInfo);
                        m_MouseLeftDown = false;
                        break;
                    case BFireType.Triple:
                        for (int i = 0; i < 3; i++)
                        {
                            OpenFier(hitInfo);
                        }
                        m_MouseLeftDown = false;
                        break;
                    case BFireType.Sustained:
                        OpenFier(hitInfo);
                        break;
                }
            }
        }

        void LateUpdate()
        {
            BulletUpdate();

            humanModel.eulerAngles = Vector3.up * m_CameraTran.eulerAngles.y;
        }

        void ISingleton.Quit()
        {
            m_Gun = null;
            bulletHole = null;
            bulletHolePool.Clear();
            bulletHolePool.Dispose();
            bulletHolePool = null;

            m_CameraTran = null;
        }

        public void ReStart()
        {

        }

        public void QuitGame()
        {
            EF.Unregister(this);
            Destroy(humanModel.gameObject);
            Destroy(this);
            Destroy(gameObject);
        }

        #region Pool
        #region Bullet
        Transform OnCreateBullet()
        {
            Transform _t = Instantiate(bullet);
            _t.SetParent(bulletAll);
            return _t;
        }

        void OnGetBullet(Transform go)
        {
            go.gameObject.SetActive(true);
            EF.Timer.AddCountdownEvent(.25f, delegate
            {
                bulletPool.Release(go);
            });
            bulletMoving.Add(go);
        }

        void OnReleaseBullet(Transform go)
        {
            bulletMoving.Remove(go);
            go.gameObject.SetActive(false);
        }
        #endregion
        #region Bullet Hole
        Transform OnCreateBulletHole()
        {
            Transform _t = Instantiate(bulletHole);
            _t.SetParent(bulletAll);
            return _t;
        }

        void OnGetBulletHole(Transform go)
        {
            go.gameObject.SetActive(true);
            EF.Timer.AddCountdownEvent(2.0f, delegate
            {
                bulletHolePool.Release(go);
            });
        }

        void OnReleaseBulletHole(Transform go)
        {
            go.gameObject.SetActive(false);
        }
        #endregion
        #endregion

        void BulletUpdate()
        {
            for (int i = 0; i < bulletMoving.Count; i++)
            {
                bulletMoving[i].position += bulletMoving[i].forward * Time.deltaTime * bulletMoveSpeed;
            }
        }

        void OpenFier(RaycastHit hitInfo)
        {
            D.Log($"{hitInfo.collider.name}     {hitInfo.point}");

            if (hitInfo.collider.CompareTag("Wall") || hitInfo.collider.CompareTag("Ground"))
            {
                Transform _bh = bulletHolePool.Get();
                Transform _buttle = bulletPool.Get();
                _buttle.SetPositionAndRotation(m_CameraTran.position, m_CameraTran.rotation);
                _buttle.LookAt(hitInfo.point);
                _bh.transform.position = hitInfo.point + hitInfo.normal * 0.025f;

                //让弹孔与射线碰撞体的法线垂直（让弹孔总是贴在物体的每一个面的表面）
                _bh.transform.LookAt(hitInfo.point - hitInfo.normal);
            }
        }
    }
}
