/*
 * ================================================
 * Describe:      This script is used to .
 * Author:        Alvin5100(Wang)
 * CreationTime:  2025-12-10 09:58:38
 * ModifyAuthor:  Alvin5100(Wang)
 * ModifyTime:    2025-12-10 09:58:38
 * ScriptVersion: 0.1
 * ===============================================
*/

using System;
using UnityEngine;
using Object = UnityEngine.Object;

namespace EasyFramework.Managers.Pool
{
    /// <summary>
    /// GameObject 工厂
    /// </summary>
    public class GameObjectFactory : IObjectFactory<GameObject>
    {
        private readonly GameObject _prefab;
        private readonly Transform _parent;
        private readonly string _namePrefix;

        public GameObjectFactory(GameObject prefab, Transform parent = null, string namePrefix = null)
        {
            _prefab = prefab ?? throw new ArgumentNullException(nameof(prefab));
            _parent = parent;
            _namePrefix = namePrefix ?? prefab.name;
        }

        public GameObject Create()
        {
            if (_prefab == null)
            {
                Debug.LogError("Prefab is null, cannot create GameObject");
                return null;
            }

            GameObject obj = _parent != null ? Object.Instantiate(_prefab, _parent) : Object.Instantiate(_prefab);
            obj.name = $"{_namePrefix}_{Guid.NewGuid():N}";
            return obj;
        }

        public void Dispose(GameObject item)
        {
            if (item != null)
            {
                Object.Destroy(item);
            }
        }

        public bool IsValidity(GameObject item)
        {
            return item != null && item.activeInHierarchy;
        }

        public void ResetState(GameObject item)
        {
            if (item == null) return;

            item.SetActive(false);
            item.transform.localPosition = Vector3.zero;
            item.transform.localRotation = Quaternion.identity;
            item.transform.localScale = Vector3.one;

            // 重置 Rigidbody
            var rb = item.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.velocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
                rb.Sleep();
            }

            // 重置 Rigidbody2D
            var rb2d = item.GetComponent<Rigidbody2D>();
            if (rb2d != null)
            {
                rb2d.velocity = Vector2.zero;
                rb2d.angularVelocity = 0f;
                rb2d.Sleep();
            }
        }
    }

}
