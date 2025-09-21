using System;
using UnityEngine;
using System.Collections.Generic;
using Tools;
using Object = UnityEngine.Object;

namespace DI.Services
{
    public class PoolService : IPoolService
    {
        private readonly Dictionary<GameObject, Queue<GameObject>> _pools = new();
        private readonly Dictionary<GameObject, GameObject> _poolRoots = new();

        public T Get<T>(GameObject prefab) where T : Component
        {
            if (prefab == null)
            {
                DebCon.Err("Prefab is null", "PoolService");
                return null;
            }
            
            if (!_pools.TryGetValue(prefab, out var queue))
            {
                queue = new Queue<GameObject>();
                _pools[prefab] = queue;
                DebCon.Log($"No pool exists for prefab {prefab.name}. Creating one.", "PoolService");
            }

            var root = GetRoot(prefab);
            GameObject instance = null;

            if (queue.Count > 0)
            {
                instance = queue.Dequeue();
                instance.SetActive(true);
            }
            else
            {
                instance = Object.Instantiate(prefab, Vector3.zero, Quaternion.identity, root.transform);
                var pooledObj = instance.gameObject.AddComponent<PooledObject>();
                pooledObj.Prefab = prefab;
                pooledObj.Pool = this;
            }

            return instance.GetComponent<T>();
        }

        public void Return(GameObject prefab, GameObject item)
        {
            if (prefab == null || item == null)
            {
                DebCon.Err("Prefab or item is null", "PoolService");
                return;
            }

            if (!_pools.TryGetValue(prefab, out var queue))
            {
                queue = new Queue<GameObject>();
                _pools[prefab] = queue;
            }

            item.SetActive(false);
            queue.Enqueue(item);
        }

        public void Clear()
        {
            foreach (var root in _poolRoots.Values)
            {
                if (root != null)
                {
                    Object.Destroy(root);
                }
            }
            
            _pools.Clear();
            _poolRoots.Clear();
        }
        
        private GameObject GetRoot(GameObject prefab)
        {
            if (!_poolRoots.TryGetValue(prefab, out var root))
            {
                DebCon.Log($"No root pool exists for prefab {prefab.name}. Creating one.", "PoolService");
                root = new GameObject($"[POOL] {prefab.name}");
                Object.DontDestroyOnLoad(root);
                _poolRoots[prefab] = root;
            }

            return root;
        }
    }

    public class PooledObject : MonoBehaviour
    {
        public GameObject Prefab { get; set; }
        public IPoolService Pool { get; set; }

        private void OnDisable()
        {
            // Automatically return to pool when disabled
            if (Pool != null && Prefab != null)
            {
                Pool.Return(Prefab, gameObject);
            }
        }
    }
}