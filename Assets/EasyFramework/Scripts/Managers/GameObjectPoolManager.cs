/*
 * ================================================
 * Describe:        This is pool manager class.
 * Author:          Xiaohei.Wang(Wenhao)
 * CreationTime:    2022-05-14:33:01
 * ModifyAuthor:    Xiaohei.Wang(Wenhao)
 * ModifyTime:      2022-05-14:33:01
 * Version:         1.0
 * ===============================================
 */

using System.Collections.Generic;
using UnityEngine;
using XHTools;

namespace EasyFramework.Managers
{
    public class GameObjectPoolManager : Singleton<GameObjectPoolManager>, IManager
    {
        int IManager.ManagerLevel => AppConst.ManagerLevel.ObjectToolMgr;
        Dictionary<string, GameObjectPool<MonoBehaviour>> m_dic_Pool;

        void ISingleton.Init()
        {
            m_dic_Pool = new Dictionary<string, GameObjectPool<MonoBehaviour>>();
        }

        void ISingleton.Quit()
        {
            List<string> keys = new List<string>();
            foreach (var item in m_dic_Pool)
                keys.Add(item.Key);

            for (int i = keys.Count - 1; i >= 0; i--)
            {
                m_dic_Pool[keys[i]].ReleasePool();
                m_dic_Pool.Remove(keys[i]);
                keys.RemoveAt(i);
            }
            keys.Clear();
            m_dic_Pool.Clear();
            m_dic_Pool = null;
        }

        /// <summary>
        /// Create a object pool whit type T.创建一个对象池类型为 T
        /// </summary>
        /// <typeparam name="T">element type of T.元素类型</typeparam>
        /// <param name="obj">the type of T`s objcet.元素对象</param>
        /// <param name="parent">parent node.父节点</param>
        /// <param name="lockCount">lock pool count.是否锁池数</param>
        /// <param name="initPoolCount">initialize the pool count.初始化池 子节点数量</param>
        public void CreateTPool<T>(T obj, Transform parent = null, bool lockCount = false, int initPoolCount = 5) where T : MonoBehaviour
        {
            string _poolName = typeof(T).ToString();
            if (m_dic_Pool.ContainsKey(_poolName))
            {
                D.Error($"Current of type << {typeof(T)} >> pool exist.");
                return;
            }
            GameObjectPool<MonoBehaviour> pool = new GameObjectPool<MonoBehaviour>(obj, parent, lockCount, initPoolCount);
            m_dic_Pool.Add(_poolName, pool);
        }

        /// <summary>
        /// Get this pool count.获取T类型对象池的子节点数量
        /// </summary>
        /// <typeparam name="T">pool type of T. T 类型</typeparam>
        public int GetPoolCount<T>() where T : MonoBehaviour
        {
            string _poolName = typeof(T).ToString();
            if (!CheckPoolExist(_poolName))
                return -1;
            return m_dic_Pool[typeof(T).ToString()].PoolCount;
        }

        /// <summary>
        /// Get this pool allowance.获取T类型池中的剩余数量
        /// </summary>
        /// <typeparam name="T">pool type of T. T 类型</typeparam>
        public int GetPoolAllowance<T>() where T : MonoBehaviour
        {
            string _poolName = typeof(T).ToString();
            if (!CheckPoolExist(_poolName))
                return -1;
            return m_dic_Pool[typeof(T).ToString()].PoolAllowance;
        }

        /// <summary>
        /// Get a element in type of T pool.获取 T 类型池中的一个元素
        /// </summary>
        /// <typeparam name="T">element type of T. T 类型</typeparam>
        /// <param name="pos">position. 获取时的位置</param>
        /// <param name="rotation">rotation. 获取时的旋转角</param>
        /// <returns>返回一个T类型元素</returns>
        public T Get<T>(Vector3 pos = default, Quaternion rotation = default) where T: MonoBehaviour
        {
            T _item = null;
            string _poolName = typeof(T).ToString();
            if (!CheckPoolExist(_poolName))
                return null;

            _item = m_dic_Pool[_poolName].Get() as T;
            return _item;
        }

        /// <summary>
        /// Recyle element.回收元素
        /// </summary>
        /// <typeparam name="T">element type of T. T类型的元素</typeparam>
        /// <param name="item">element. 元素</param>
        public void Recycle<T>(T item) where T : MonoBehaviour
        {
            string _poolName = typeof(T).ToString();
            if (!CheckPoolExist(_poolName))
                return;
            m_dic_Pool[typeof(T).ToString()].Recycle(item);
        }

        /// <summary>
        /// Clear this pool.清空对象池
        /// </summary>
        /// <typeparam name="T">pool type of T. T类型的对象池</typeparam>
        public void ClearPool<T>() where T : MonoBehaviour
        {
            string _poolName = typeof(T).ToString();
            if (!CheckPoolExist(_poolName))
                return;
            m_dic_Pool[typeof(T).ToString()].ClearPool();
        }

        /// <summary>
        /// Release a pool.释放对象池
        /// </summary>
        /// <typeparam name="T">pool type of T. T类型的对象池</typeparam>
        public void ReleasePool<T>() where T : MonoBehaviour
        {
            string _poolName = typeof(T).ToString();
            if (!CheckPoolExist(_poolName))
                return;
            m_dic_Pool[_poolName].ReleasePool();
            m_dic_Pool.Remove(_poolName);
        }

        private bool CheckPoolExist(string poolName)
        {
            if (!m_dic_Pool.ContainsKey(poolName))
            {
                D.Error($"There is no pool of type << {poolName} >>");
                return false;
            }
            return true;
        }

        #region << GameObjectPool class >>
        /// <summary>
        /// Game object pool whit type T. 一个 T 类型的对象池，元素需要继承MonoBehaviour
        /// </summary>
        /// <typeparam name="T"></typeparam>
        class GameObjectPool<T> where T : MonoBehaviour
        {
            private T m_gameObject;
            private Transform m_parent;
            private int m_int_objsCount, m_int_currentCount;
            private bool m_bol_lockCount;
            private Queue<T> m_que_Pool;
            private List<T> m_lst_Pool;

            /// <summary>
            /// The pool count.池子数量
            /// </summary>
            public int PoolCount => m_int_objsCount;

            /// <summary>
            /// The pool allowance.池子剩余量
            /// </summary>
            public int PoolAllowance => m_int_objsCount - m_int_currentCount;

            /// <summary>
            /// Game object pool whit type T.获取一个 T 类型的对象
            /// </summary>
            /// <param name="obj">the type of T`s objcet. T 类型的对象</param>
            /// <param name="parent">parent node.父节点</param>
            /// <param name="lockCount">lock pool count.是否锁池数量</param>
            /// <param name="initPoolCount">initialize the pool count.池初始化子节点数量</param>
            public GameObjectPool(T obj, Transform parent = null, bool lockCount = false, int initPoolCount = 5)
            {
                m_parent = parent;
                m_gameObject = obj;
                m_bol_lockCount = lockCount;
                m_int_objsCount = initPoolCount;
                m_que_Pool = new Queue<T>();
                m_lst_Pool = new List<T>();

                obj.gameObject.SetActive(false);
                obj.transform.SetParent(m_parent);
                m_que_Pool.Enqueue(obj);
                m_lst_Pool.Add(obj);
                for (int i = 0; i < initPoolCount - 1; i++)
                    m_que_Pool.Enqueue(CreateT(true));
            }

            /// <summary>
            /// Get a element in type of T pool.获取 T 类型池中的一个元素
            /// </summary>
            /// <typeparam name="T">element type of T. T 类型</typeparam>
            /// <param name="pos">position. 获取时的位置</param>
            /// <param name="rotation">rotation. 获取时的旋转角</param>
            /// <returns>返回一个T类型元素</returns>
            public T Get(Vector3 pos = default, Quaternion rotation = default)
            {
                if (m_que_Pool.Count == 0 && m_bol_lockCount)
                {
                    D.Error($"Error!!  pool is empty.");
                    return null;
                }

                T item;

                if (m_que_Pool.Count == 0)
                    item = CreateT();
                else
                    item = m_que_Pool.Dequeue();

                item.transform.SetPositionAndRotation(pos, rotation);
                item.gameObject.SetActive(true);

                m_int_currentCount++;
                return item;
            }

            /// <summary>
            /// Recyle element.回收元素
            /// </summary>
            /// <param name="item">element. 元素</param>
            public void Recycle(T item)
            {
                if (m_que_Pool.Contains(item))
                {
                    D.Error($"{item.GetType()}`s pool was exit this item;");
                    return;
                }

                m_int_currentCount--;
                m_que_Pool.Enqueue(item);
                item.gameObject.SetActive(false);
            }

            /// <summary>
            /// Clear this pool.清空对象池
            /// </summary>
            public void ClearPool()
            {
                for (int i = m_que_Pool.Count - 1; i >= 0; i--)
                    m_que_Pool.Dequeue();
                m_que_Pool.Clear();

                for (int i = m_lst_Pool.Count - 1; i >= 0; i--)
                {
                    T item = m_lst_Pool[i];
                    m_lst_Pool.Remove(item);
                    Object.Destroy(item);
                    Object.Destroy(item.gameObject);
                }
                m_lst_Pool.Clear();

                m_int_currentCount = 0;
                m_int_objsCount = 0;
            }

            /// <summary>
            /// Release a pool.释放对象池
            /// </summary>
            public void ReleasePool()
            {
                ClearPool();
                m_parent = null;
                m_gameObject = null;
            }

            private T CreateT(bool init = false)
            {
                if (m_int_currentCount >= m_int_objsCount && m_bol_lockCount)
                {
                    D.Error($"Error! this pool is lock count, current count = {m_int_currentCount}");
                    return null;
                }
                if (!init) m_int_objsCount++;
                T item = Object.Instantiate(m_gameObject, m_parent);
                m_lst_Pool.Add(item);
                return item;
            }
        }
        #endregion
    }
}
