/*
 * ================================================
 * Describe:      高频率、大数据量压力测试（扩展版）
 *                 支持多种 GameObject 池、多种自定义类池和基础类型池
 * Author:        Alvin5100
 * CreationTime:  2026-05-11 10:24:40
 * ModifyAuthor:  AI Assistant
 * ModifyTime:    2026-05-11 14:30:00
 * ScriptVersion: 0.2
 * ===============================================
 */

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using EasyFramework.Managers.Pool;

namespace EasyFramework.Managers.Pool.Tests
{
    /// <summary>
    /// 高频率、大数据量的持续压力测试（用于展示监控面板的动态效果）
    /// 扩展版：增加更多池类型、更多测试数据、更真实的模拟
    /// </summary>
    public class PoolTest : MonoBehaviour
    {
        [Header("测试配置")]
        [SerializeField] private bool _startOnPlay = true;
        [SerializeField] private float _testDuration = 60f;          // 运行时长（秒），0表示无限
        [SerializeField] private int _poolCapacity = 50;             // 每个池的最大空闲数量（增大）
        [SerializeField] private int _spawnBatchSize = 8;            // 每批次产生的对象数（增大）
        [SerializeField] private float _minDelay = 0.03f;            // 最小延迟（秒）
        [SerializeField] private float _maxDelay = 0.15f;            // 最大延迟（秒）
        [SerializeField] private Transform _poolRoot;                // 池根节点（可选）

        private PoolManager _PoolManager;
        private bool _isRunning = true;
        private int _totalSpawned;
        private int _totalRecycled;

        // ========== 1. 多个 GameObject 池 ==========
        private GameObject _cubePrefab;
        private GameObject _spherePrefab;
        private GameObject _capsulePrefab;
        private List<GameObject> _activeCubes = new List<GameObject>();
        private List<GameObject> _activeSpheres = new List<GameObject>();
        private List<GameObject> _activeCapsules = new List<GameObject>();

        // ========== 2. 多个自定义类池 ==========
        private List<EnemyData> _activeEnemies = new List<EnemyData>();
        private List<BulletData> _activeBullets = new List<BulletData>();
        private List<ParticleData> _activeParticles = new List<ParticleData>();
        private List<TransformData> _activeTransforms = new List<TransformData>();

        // ========== 3. 基础类型池（String / Vector3 包装） ==========
        private List<string> _activeStrings = new List<string>();
        private List<Vector3Wrapper> _activeVectors = new List<Vector3Wrapper>();

        // 记录池引用（用于预热等操作，非必须）
        private ObjectPool<EnemyData> _enemyPool;
        private ObjectPool<BulletData> _bulletPool;
        private ObjectPool<ParticleData> _particlePool;
        private ObjectPool<TransformData> _transformPool;
        private ObjectPool<string> _stringPool;
        private ObjectPool<Vector3Wrapper> _vectorPool;

        private void Start()
        {
            if (_startOnPlay)
            {
                StartCoroutine(RunContinuousTest());
            }
        }

        private IEnumerator RunContinuousTest()
        {
            _PoolManager =PoolManager.Instance;
            if (_PoolManager == null)
            {
                Debug.LogError("PoolManager 不存在，测试终止");
                yield break;
            }

            _PoolManager.SetOpenDebug(true);
            Debug.Log($"[PoolTest] 开始扩展压力测试，将持续 {(_testDuration > 0 ? _testDuration + "秒" : "无限")}");
            Debug.Log($"池容量: {_poolCapacity}, 每批数量: {_spawnBatchSize}, 间隔: {_minDelay}~{_maxDelay}s");

            // 创建所有扩展池
            CreateAllPools();

            yield return null; // 等待一帧初始化

            float startTime = Time.time;

            while (_isRunning && (_testDuration <= 0 || Time.time - startTime < _testDuration))
            {
                // 产生一批对象
                yield return StartCoroutine(SpawnBatch());

                // 随机延迟后回收
                float delay = UnityEngine.Random.Range(_minDelay, _maxDelay);
                yield return new WaitForSeconds(delay);

                // 回收一批对象（随机数量 1~batchSize+2）
                int recycleCount = UnityEngine.Random.Range(1, _spawnBatchSize + 3);
                yield return StartCoroutine(RecycleRandom(recycleCount));

                yield return new WaitForSeconds(UnityEngine.Random.Range(0.02f, 0.1f));

                // 每 3 秒输出统计
                if (Time.frameCount % 180 == 0)
                {
                    Debug.Log($"[统计] 总产生: {_totalSpawned}, 总回收: {_totalRecycled}, " +
                              $"激活: Cube={_activeCubes.Count}, Sphere={_activeSpheres.Count}, Capsule={_activeCapsules.Count}, " +
                              $"Enemy={_activeEnemies.Count}, Bullet={_activeBullets.Count}, Particle={_activeParticles.Count}, " +
                              $"Transform={_activeTransforms.Count}, String={_activeStrings.Count}, Vector={_activeVectors.Count}");
                }
            }

            Debug.Log("[PoolTest] 测试结束。请查看 PoolManager Inspector 中的最终状态。");
        }

        private void CreateAllPools()
        {
            // 确保根节点存在
            if (_poolRoot == null)
            {
                var rootGo = new GameObject("PoolTest_Root");
                _poolRoot = rootGo.transform;
            }

            // ---------- 1. 创建多个 GameObject 池 ----------
            CreatePrimitivePrefab(PrimitiveType.Cube, "CubePrefab", out _cubePrefab);
            CreatePrimitivePrefab(PrimitiveType.Sphere, "SpherePrefab", out _spherePrefab);
            CreatePrimitivePrefab(PrimitiveType.Capsule, "CapsulePrefab", out _capsulePrefab);

            // 注册到 PoolManager
            _PoolManager.CreateGameObjectPool(_cubePrefab, _poolRoot, initial: 5, _poolCapacity, idleTimeout: 10f);
            _PoolManager.CreateGameObjectPool(_spherePrefab, _poolRoot, initial: 5, _poolCapacity, idleTimeout: 10f);
            _PoolManager.CreateGameObjectPool(_capsulePrefab, _poolRoot, initial: 5, _poolCapacity, idleTimeout: 10f);
            Debug.Log($"创建 3 个 GameObject 池，容量={_poolCapacity}");

            // ---------- 2. 自定义类池 ----------
            // EnemyData
            _PoolManager.CreateObjectPool<EnemyData>(
                max: _poolCapacity,
                factory: () => new EnemyData(),
                reset: (d) => d.Reset()
            );
            _enemyPool = _PoolManager.GetObjectPool<EnemyData>();

            // BulletData
            _PoolManager.CreateObjectPool<BulletData>(
                max: _poolCapacity,
                factory: () => new BulletData(),
                reset: (d) => d.Reset()
            );
            _bulletPool = _PoolManager.GetObjectPool<BulletData>();

            // ParticleData
            _PoolManager.CreateObjectPool<ParticleData>(
                max: _poolCapacity,
                factory: () => new ParticleData(),
                reset: (d) => d.Reset()
            );
            _particlePool = _PoolManager.GetObjectPool<ParticleData>();

            // TransformData (模拟变换数据)
            _PoolManager.CreateObjectPool<TransformData>(
                max: _poolCapacity,
                factory: () => new TransformData(),
                reset: (d) => d.Reset()
            );
            _transformPool = _PoolManager.GetObjectPool<TransformData>();

            Debug.Log($"创建 4 个自定义类池，容量={_poolCapacity}");

            // ---------- 3. 基础类型池（包装类） ----------
            // 字符串池（虽不可变但可测试池管理逻辑）
            _PoolManager.CreateObjectPool<string>(
                max: _poolCapacity,
                factory: () => "DynamicString_" + UnityEngine.Random.Range(0, 100000),
                reset: null
            );
            _stringPool = _PoolManager.GetObjectPool<string>();

            // Vector3 包装类池（因为 ObjectPool<T> 要求 T 是 class）
            _PoolManager.CreateObjectPool<Vector3Wrapper>(
                max: _poolCapacity,
                factory: () => new Vector3Wrapper(),
                reset: (v) => v.Reset()
            );
            _vectorPool = _PoolManager.GetObjectPool<Vector3Wrapper>();

            Debug.Log($"创建 2 个基础类型池，容量={_poolCapacity}");
        }

        // 辅助：创建简单预制体并添加 Poolable 标记
        private void CreatePrimitivePrefab(PrimitiveType type, string name, out GameObject prefab)
        {
            prefab = GameObject.CreatePrimitive(type);
            prefab.name = name;
            prefab.SetActive(false);
            // 添加 IPoolable 组件，以便池系统管理生命周期
            var poolable = prefab.AddComponent<TestPoolableBehaviour>();
            poolable.Initialize($"{name}_Poolable");
            // 可选：添加一些随机颜色材质用于视觉区分
            var renderer = prefab.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.material = new Material(Shader.Find("Standard"));
                renderer.material.color = type switch
                {
                    PrimitiveType.Cube => Color.red,
                    PrimitiveType.Sphere => Color.green,
                    PrimitiveType.Capsule => Color.blue,
                    _ => Color.white
                };
            }
        }

        // 每批次随机产生各种类型的对象
        private IEnumerator SpawnBatch()
        {
            for (int i = 0; i < _spawnBatchSize; i++)
            {
                // 扩展类型范围：0~8 共9种池类型
                int poolType = UnityEngine.Random.Range(0, 9);
                switch (poolType)
                {
                    case 0: SpawnGameObject(_cubePrefab, _activeCubes); break;
                    case 1: SpawnGameObject(_spherePrefab, _activeSpheres); break;
                    case 2: SpawnGameObject(_capsulePrefab, _activeCapsules); break;
                    case 3: SpawnEnemyData(); break;
                    case 4: SpawnBulletData(); break;
                    case 5: SpawnParticleData(); break;
                    case 6: SpawnTransformData(); break;
                    case 7: SpawnString(); break;
                    case 8: SpawnVectorWrapper(); break;
                }
                _totalSpawned++;
                yield return null;
            }
        }

        // ----- GameObject 产生 -----
        private void SpawnGameObject(GameObject prefab, List<GameObject> activeList)
        {
            var go = _PoolManager.Spawn(prefab);
            if (go != null)
            {
                // 随机位置、旋转，模拟使用
                go.transform.position = UnityEngine.Random.insideUnitSphere * 8f;
                go.transform.rotation = UnityEngine.Random.rotation;
                activeList.Add(go);
            }
        }

        // ----- 自定义类产生 -----
        private void SpawnEnemyData()
        {
            var data = _PoolManager.GetFromPool<EnemyData>();
            data.EnemyId = UnityEngine.Random.Range(1, 50000);
            data.Health = UnityEngine.Random.Range(50, 500);
            data.Position = UnityEngine.Random.insideUnitSphere * 20f;
            _activeEnemies.Add(data);
        }

        private void SpawnBulletData()
        {
            var data = _PoolManager.GetFromPool<BulletData>();
            data.Damage = UnityEngine.Random.Range(5, 100);
            data.Speed = UnityEngine.Random.Range(10f, 50f);
            data.Direction = UnityEngine.Random.onUnitSphere;
            _activeBullets.Add(data);
        }

        private void SpawnParticleData()
        {
            var data = _PoolManager.GetFromPool<ParticleData>();
            data.Lifetime = UnityEngine.Random.Range(0.5f, 3f);
            data.Color = UnityEngine.Random.ColorHSV();
            data.Size = UnityEngine.Random.Range(0.1f, 1f);
            _activeParticles.Add(data);
        }

        private void SpawnTransformData()
        {
            var data = _PoolManager.GetFromPool<TransformData>();
            data.Position = UnityEngine.Random.insideUnitSphere * 10f;
            data.Rotation = UnityEngine.Random.rotation;
            data.Scale = Vector3.one * UnityEngine.Random.Range(0.5f, 2f);
            _activeTransforms.Add(data);
        }

        private void SpawnString()
        {
            string str = _PoolManager.GetFromPool<string>();
            // 模拟使用：拼接内容（实际应用中会重新赋值）
            string used = str + "_used_" + UnityEngine.Random.Range(0, 100);
            _activeStrings.Add(str);
        }

        private void SpawnVectorWrapper()
        {
            var wrapper = _PoolManager.GetFromPool<Vector3Wrapper>();
            wrapper.Value = UnityEngine.Random.insideUnitSphere * 15f;
            _activeVectors.Add(wrapper);
        }

        // 随机回收指定数量的对象（从所有激活列表中随机选择）
        private IEnumerator RecycleRandom(int count)
        {
            int recycledThisBatch = 0;
            // 为避免死循环，最多尝试 count * 3 次
            int maxAttempts = count * 3;
            int attempts = 0;

            while (recycledThisBatch < count && attempts < maxAttempts)
            {
                attempts++;
                int type = UnityEngine.Random.Range(0, 9);
                bool recycled = false;

                switch (type)
                {
                    case 0: recycled = RecycleFromList(_activeCubes, go => _PoolManager.Despawn(go)); break;
                    case 1: recycled = RecycleFromList(_activeSpheres, go => _PoolManager.Despawn(go)); break;
                    case 2: recycled = RecycleFromList(_activeCapsules, go => _PoolManager.Despawn(go)); break;
                    case 3: recycled = RecycleFromList(_activeEnemies, data => _PoolManager.ReturnToPool(data)); break;
                    case 4: recycled = RecycleFromList(_activeBullets, data => _PoolManager.ReturnToPool(data)); break;
                    case 5: recycled = RecycleFromList(_activeParticles, data => _PoolManager.ReturnToPool(data)); break;
                    case 6: recycled = RecycleFromList(_activeTransforms, data => _PoolManager.ReturnToPool(data)); break;
                    case 7: recycled = RecycleFromList(_activeStrings, str => _PoolManager.ReturnToPool(str)); break;
                    case 8: recycled = RecycleFromList(_activeVectors, v => _PoolManager.ReturnToPool(v)); break;
                }

                if (recycled)
                {
                    recycledThisBatch++;
                    _totalRecycled++;
                }
                yield return null;
            }
        }

        // 通用回收辅助：从列表中随机移除一个元素并调用回收方法
        private bool RecycleFromList<T>(List<T> list, Action<T> recycleAction)
        {
            if (list.Count == 0) return false;
            int idx = UnityEngine.Random.Range(0, list.Count);
            T item = list[idx];
            list.RemoveAt(idx);
            recycleAction(item);
            return true;
        }

        private void OnDestroy()
        {
            _isRunning = false;
            if (_PoolManager != null)
            {
                _PoolManager.ClearAll();
                Debug.Log("[PoolTest] 已清空所有池");
            }

            // 清理动态创建的预制体（可选）
            if (_cubePrefab != null) Destroy(_cubePrefab);
            if (_spherePrefab != null) Destroy(_spherePrefab);
            if (_capsulePrefab != null) Destroy(_capsulePrefab);
        }
    }

    // ========== 扩展的自定义数据类 ==========
    public class EnemyData
    {
        public int EnemyId;
        public int Health;
        public Vector3 Position;

        public void Reset()
        {
            EnemyId = 0;
            Health = 0;
            Position = Vector3.zero;
        }
    }

    public class BulletData
    {
        public int Damage;
        public float Speed;
        public Vector3 Direction;

        public void Reset()
        {
            Damage = 0;
            Speed = 0;
            Direction = Vector3.zero;
        }
    }

    public class ParticleData
    {
        public float Lifetime;
        public Color Color;
        public float Size;

        public void Reset()
        {
            Lifetime = 0;
            Color = Color.white;
            Size = 0;
        }
    }

    public class TransformData
    {
        public Vector3 Position;
        public Quaternion Rotation;
        public Vector3 Scale;

        public void Reset()
        {
            Position = Vector3.zero;
            Rotation = Quaternion.identity;
            Scale = Vector3.one;
        }
    }

    // Vector3 包装类（使 Vector3 可池化）
    public class Vector3Wrapper
    {
        public Vector3 Value;

        public void Reset()
        {
            Value = Vector3.zero;
        }
    }

    // 改进的 IPoolable 组件（用于 GameObject）
    public class TestPoolableBehaviour : MonoBehaviour, IPoolable
    {
        public bool IsFromPool { get; set; }
        private string _debugName;

        public void Initialize(string name)
        {
            _debugName = name;
        }

        public void OnSpawn()
        {
            // 激活时的逻辑：例如播放特效、重置状态
            // 此处可添加简单日志（调试用）
            // Debug.Log($"{_debugName} spawned");
        }

        public void OnDespawn()
        {
            // 停用前的清理：停止协程、重置材质等
            // Debug.Log($"{_debugName} despawned");
        }
    }
}