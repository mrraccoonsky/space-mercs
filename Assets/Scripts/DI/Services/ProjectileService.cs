using System;
using System.Collections.Generic;
using UnityEngine;
using Data.Projectile;
using DI.Factories;
using ECS.Components;
using ECS.Utils;

namespace DI.Services
{
    using Leopotam.EcsLite;
    
    public class ProjectileService : IProjectileService
    {
        private readonly EcsWorld _world;
        private readonly IProjectileFactory _factory;
        
        public ProjectileService(EcsWorld world, IProjectileFactory factory)
        {
            _world = world ?? throw new ArgumentNullException(nameof(world));
            _factory = factory ?? throw new ArgumentNullException(nameof(factory));
        }
        
        public void Spawn(ProjectileData data, Vector3 position, Quaternion rotation)
        {
            var bridge = _factory.Create(data, position, rotation);
            if (bridge == null) return;
            
            var entityId = _world.NewEntity();
            var projectilePool = _world.GetPool<ProjectileComponent>();
            
            ref var aProjectile = ref projectilePool.Add(entityId);
            aProjectile.Bridge = bridge;
            aProjectile.Tag = data.tag;
            aProjectile.HitEntities = HashSetPool.Get();
            aProjectile.CanHitOnCooldown = data.canHitOnCooldown;
            
            bridge.Init(entityId, _world);
            bridge.SetData(data);
            bridge.Reset();
        }
        
        public void Destroy(int entityId)
        {
            if (!EcsUtils.HasCompInPool<ProjectileComponent>(_world, entityId, out var projectilePool)) return;
            ref var aProjectile = ref projectilePool.Get(entityId);
            HashSetPool.Release(aProjectile.HitEntities);
        }
    }

    // helper pool to save up mem alloc
    internal static class HashSetPool
    {
        private static readonly Queue<HashSet<int>> Pool = new();
    
        public static HashSet<int> Get()
        {
            return Pool.Count > 0 ? Pool.Dequeue() : new HashSet<int>();
        }
    
        public static void Release(HashSet<int> set)
        {
            set.Clear();
            Pool.Enqueue(set);
        }
    }
}