using System.Collections.Generic;
using UnityEngine;
using Zenject;

namespace Factories
{
    public struct FXData
    {
        public readonly GameObject prefab;
        public readonly Vector3 position;
        public readonly Quaternion rotation;

        public FXData(GameObject prefab, Vector3 position, Quaternion rotation)
        {
            this.prefab = prefab;
            this.position = position;
            this.rotation = rotation;
            
            // todo: add more fields
        }
    }
    
    public class ParticlePoolHelper : MonoBehaviour
    {
        // todo: encapsulate it somehow
        public GameObject prefab;
        public ParticleSystem ps;
        
        private void OnParticleSystemStopped()
        {
            if (prefab == null || ps == null) return;
            FXFactory.ReturnToPool(this);
        }
    }
    
    public static class FXFactory
    {
        private static readonly Dictionary<GameObject, Queue<ParticlePoolHelper>> Pools = new();
        private static readonly Dictionary<GameObject, GameObject> PoolRoots = new();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void ResetPools()
        {
            Pools.Clear();
            PoolRoots.Clear();
        }
        
        public static void Create(FXData data)
        {
            GameObject root;
            
            if (!Pools.TryGetValue(data.prefab, out var pool))
            {
                root = new GameObject("[POOL] " + data.prefab.name);
                Object.DontDestroyOnLoad(root);
                PoolRoots[data.prefab] = root;
                
                pool = new Queue<ParticlePoolHelper>();
                Pools[data.prefab] = pool;
            }
            else
            {
                root = PoolRoots[data.prefab];
            }
            
            ParticlePoolHelper helper;
            if (pool.Count > 0)
            {
                helper = pool.Dequeue();
                helper.transform.position = data.position;
                helper.transform.rotation = data.rotation;
                helper.gameObject.SetActive(true);
            }
            else
            {
                var go = Object.Instantiate(data.prefab, data.position, data.rotation);
                go.transform.SetParent(root.transform);

                if (go.GetComponent<ParticlePoolHelper>() == null)
                {
                    helper = go.AddComponent<ParticlePoolHelper>();
                    helper.prefab = data.prefab;
                    
                    var ps = go.GetComponent<ParticleSystem>();
                    helper.ps = ps;
                    
                    var main = ps.main;
                    main.stopAction = ParticleSystemStopAction.Callback;
                }
            }
        }
        
        public static void ReturnToPool(ParticlePoolHelper helper)
        {
            if (helper == null) return;

            var prefab = helper.prefab;
            if (!Pools.TryGetValue(prefab, out var pool))
            {
                pool = new Queue<ParticlePoolHelper>();
                Pools[prefab] = pool;
            }
            
            helper.gameObject.SetActive(false);
            pool.Enqueue(helper);
        }
    }
}