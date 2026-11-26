/*
 * ================================================
 * Describe:        ScrollListPro 分部类 — 对象池访问与管理
 * Author:          Alvin8412
 * CreationTime:    2026-11-26 15:40:00
 * ModifyAuthor:    Alvin8412
 * ModifyTime:      2026-11-26 15:40:00
 * ScriptVersion:   0.1
 * ================================================
 */

using UnityEngine;

namespace EasyFramework.Ui
{
    public partial class ScrollListPro
    {
        // 对象池 ================================================================

        // 安全获取池管理器：框架未运行（未 Initialize 或正在 QuitGames）时短路返回 false，
        // 避免 Instance getter 打错误日志并返回 null；框架运行时走 Instance 触发按需创建
        // （PoolManager 是 [Manager] 按需单例，TryGetSingleton 只查注册表、不会创建）。
        private static bool TryGetPool(out PoolManager pool)
        {
            if (!EFC.CanCreateSingleton)
            {
                pool = null;
                return false;
            }

            pool = PoolManager.Instance;
            return pool != null;
        }

        // 初始化对象池
        private void InitPool()
        {
            if (_poolInitialized || itemPrefab == null) return;
            if (!TryGetPool(out var pool)) return; // 框架未运行，暂不建池，后续可重试

            _poolInitialized = true;

            float avgSize = _prefabDefaultSize > 0 ? _prefabDefaultSize : FallbackItemSize;
            _estimatedVisibleCount =
                Mathf.CeilToInt((_viewportSize + _viewportSize * ViewportEstimateFactor) / (avgSize + itemSpacing)) +
                (bufferCount + preCreateBuffer) * 2;

            int initial = Mathf.Max(poolPreAlloc, _estimatedVisibleCount);
            pool.CreateGameObjectPool(itemPrefab, _contentRect, initial, poolMaxSize, poolIdleTimeout);
        }

        // 获取一个游戏物体
        private GameObject Rent()
        {
            if (!TryGetPool(out var pool)) return null;
            var go = pool.Spawn(itemPrefab);
            if (go != null)
            {
                go.transform.SetParent(_contentRect, false);
                go.SetActive(false);

                int id = go.GetInstanceID();
                if (!_onCreateCalled.Contains(id))
                {
                    var item = go.GetComponent<IScrollProItem>();
                    if (item != null)
                    {
                        item.OnCreate(go.transform as RectTransform);
                        _onCreateCalled.Add(id);
                    }
                }
            }

            return go;
        }

        // 回收一个游戏物体
        private void Recycle(GameObject go)
        {
            if (go == null) return;
            if (!TryGetPool(out var pool)) return;
            pool.Despawn(go);
        }

        // 销毁对象池
        private void DestroyPool()
        {
            if (!_poolInitialized) return;
            _poolInitialized = false;
            if (!TryGetPool(out var pool)) return;
            pool.DestroyGameObjectPool(itemPrefab);
        }
    }
}