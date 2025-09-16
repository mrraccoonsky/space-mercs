using System.Collections.Generic;
using ECS.Bridges;
using ECS.Components;
using Leopotam.EcsLite;
using UnityEngine;

namespace Factories
{
    public struct ProjectileData
    {
        public readonly GameObject prefab;
        public readonly string tag;
        public readonly Vector3 position;
        public readonly Quaternion rotation;
        public readonly Vector3 initialVelocity;
        public readonly float speed;
        public readonly float lifetime;
        public readonly int penetrationCount;
        public readonly bool canHitOnCooldown;

        public ProjectileData(GameObject prefab, string tag, Vector3 position, Quaternion rotation, Vector3 initialVelocity, float speed, float lifetime, int penetrationCount, bool canHitOnCooldown)
        {
            this.prefab = prefab;
            this.tag = tag;
            this.position = position;
            this.rotation = rotation;
            this.initialVelocity = initialVelocity;
            this.speed = speed;
            this.lifetime = lifetime;
            this.penetrationCount = penetrationCount;
            this.canHitOnCooldown = canHitOnCooldown;
        }
    }
    
    public static class ProjectileFactory
    {
        private static readonly Dictionary<GameObject, Queue<ProjectileBridge>> Pools = new();
        private static readonly Dictionary<GameObject, GameObject> PoolRoots = new();
        
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void ResetPools()
        {
            Pools.Clear();
            PoolRoots.Clear();
        }
        
        public static void Create(EcsWorld world, ProjectileData data)
        {
            GameObject root;
            
            if (!Pools.TryGetValue(data.prefab, out var pool))
            {
                root = new GameObject("[POOL] " + data.prefab.name);
                Object.DontDestroyOnLoad(root);
                PoolRoots[data.prefab] = root;
                
                pool = new Queue<ProjectileBridge>();
                Pools[data.prefab] = pool;
            }
            else
            {
                root = PoolRoots[data.prefab];
            }
    
            var entityId = world.NewEntity();
            var projectilePool = world.GetPool<ProjectileComponent>();
            ref var aProjectile = ref projectilePool.Add(entityId);
            
            ProjectileBridge bridge;
            if (pool.Count > 0)
            {
                bridge = pool.Dequeue();
                bridge.transform.position = data.position;
                bridge.transform.rotation = data.rotation;
                bridge.gameObject.SetActive(true);
            }
            else
            {
                var go = Object.Instantiate(data.prefab, data.position, data.rotation);
                go.transform.SetParent(root.transform);

                bridge = go.GetComponent<ProjectileBridge>();
            }
            
            aProjectile.Bridge = bridge;
            aProjectile.Tag = data.tag;
            aProjectile.HitEntities = new HashSet<int>();
            aProjectile.CanHitOnCooldown = data.canHitOnCooldown;

            bridge.Init(entityId, world);
            bridge.SetData(data);
            bridge.Reset();
        }

        public static void ReturnToPool(ProjectileBridge bridge)
        {
            if (bridge == null) return;

            var prefab = bridge.Prefab;
            if (!Pools.TryGetValue(prefab, out var pool))
            {
                pool = new Queue<ProjectileBridge>();
                Pools[prefab] = pool;
            }
            
            bridge.gameObject.SetActive(false);
            pool.Enqueue(bridge);
        }
    }
}