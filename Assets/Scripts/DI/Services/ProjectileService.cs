using System;
using System.Collections.Generic;
using UnityEngine;
using Data;
using Data.Explosion;
using Data.Weapon;
using DI.Factories;
using ECS.Bridges;
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

        public ProjectileBridge SpawnProjectile(WeaponConfig cfg, GlobalTag tag, Vector3 position, Quaternion rotation)
        {
            var bridge = _factory.CreateProjectile(cfg.projectilePrefab, position, rotation);
            if (bridge == null) return null;
            
            var entityId = _world.NewEntity();
            var projectilePool = _world.GetPool<ProjectileComponent>();
            
            ref var aProjectile = ref projectilePool.Add(entityId);
            var globalTag = cfg.enableFriendlyFire ? GlobalTag.Default : tag;
            
            aProjectile.Bridge = bridge;
            aProjectile.Tag = globalTag;
            aProjectile.HitEntities = HashSetPool.Get();
            
            aProjectile.CanHitOnCooldown = cfg.canHitOnCooldown;
            aProjectile.IgnoreHitFx = cfg.ignoreHitFx;

            aProjectile.Scale = cfg.scale;
            aProjectile.Damage = cfg.damage;
            aProjectile.PushForce = cfg.pushForce; 
            aProjectile.PushUpwardsMod = cfg.pushUpwardsMod;
            
            bridge.Init(entityId, _world);
            bridge.SetTag(globalTag);
            bridge.SetData(cfg);
            bridge.Reset();
            return bridge;
        }
        
        public void DestroyProjectile(int entityId)
        {
            if (!EcsUtils.HasCompInPool<ProjectileComponent>(_world, entityId, out var projectilePool)) return;
            ref var aProjectile = ref projectilePool.Get(entityId);
            HashSetPool.Release(aProjectile.HitEntities);
        }

        public ExplosionBridge SpawnExplosion(ExplosionConfig cfg, GlobalTag tag, Vector3 position)
        {
            var bridge = _factory.CreateExplosion(cfg.explosionPrefab, position);
            if (bridge == null) return null;
            
            var entityId = _world.NewEntity();
            var explosionPool = _world.GetPool<ExplosionComponent>();
            
            ref var aExplosion = ref explosionPool.Add(entityId);
            var globalTag = cfg.enableFriendlyFire ? GlobalTag.Default : tag;
            
            aExplosion.Bridge = bridge;
            aExplosion.Tag = globalTag;
            aExplosion.HitEntities = HashSetPool.Get();
            
            aExplosion.CanHitOnCooldown = cfg.canHitOnCooldown;
            aExplosion.IgnoreHitFx = cfg.ignoreHitFx;
            aExplosion.DistanceMult = cfg.distanceMult;
            
            // those values can be modded in ProjectileSystem if
            // cfg.clone*** are true
            aExplosion.Scale = cfg.scale;
            aExplosion.Damage = cfg.damage;
            aExplosion.Radius = cfg.scaleAffectsRadius ? cfg.scale * cfg.radius : cfg.radius;
            aExplosion.PushForce = cfg.pushForce;
            aExplosion.PushUpwardsMod = cfg.pushUpwardsMod;
            
            bridge.Init(entityId, _world);
            bridge.SetTag(globalTag);
            bridge.SetData(cfg);
            bridge.Reset();
            return bridge;
        }

        public void DestroyExplosion(int entityId)
        {
            if (!EcsUtils.HasCompInPool<ExplosionComponent>(_world, entityId, out var explosionPool)) return;
            ref var aExplosion = ref explosionPool.Get(entityId);
            HashSetPool.Release(aExplosion.HitEntities);
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