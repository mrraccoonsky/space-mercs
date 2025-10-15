using System.Collections.Generic;
using UnityEngine;
using Data;
using Data.Explosion;
using DI.Services;
using ECS.Bridges;
using ECS.Components;
using ECS.Utils;

namespace ECS.Systems
{
    using Leopotam.EcsLite;
    
    public class ProjectileSystem : IEcsRunSystem
    {
        private readonly EcsWorld _world;
        private readonly IProjectileService _projectileService;
        
        private readonly EcsPool<ProjectileComponent> _projectilePool;
        private readonly EcsFilter _projectilesFilter;

        private readonly EcsPool<ExplosionComponent> _explosionPool;
        private readonly EcsFilter _explosionsFilter;
        
        private readonly EcsPool<HealthComponent> _healthPool;
        private readonly EcsFilter _targetFilter;
        
        private readonly List<int> _entitiesToDestroy = new();
        
        public ProjectileSystem(EcsWorld world, IProjectileService projectileService)
        {
            _world = world;
            _projectileService = projectileService;
            
            _projectilesFilter = _world.Filter<ProjectileComponent>().End();
            _projectilePool = _world.GetPool<ProjectileComponent>();
            
            _explosionsFilter = _world.Filter<ExplosionComponent>().End();
            _explosionPool = _world.GetPool<ExplosionComponent>();
            
            _healthPool = _world.GetPool<HealthComponent>();
            _targetFilter = _world.Filter<HealthComponent>().End();
        }
        
        public void Run(IEcsSystems systems)
        {
            _entitiesToDestroy.Clear();
            
            TickExplosions();
            TickProjectiles();
            
            foreach (var entity in _entitiesToDestroy)
            {
                _world.DelEntity(entity);
            }
        }

        private void TickProjectiles()
        {
            foreach (var e in _projectilesFilter)
            {
                ref var aProjectile = ref _projectilePool.Get(e);
                var bridge = aProjectile.Bridge;

                if (bridge == null) continue;
                if (bridge.CheckNeedsDestroy())
                {
                    DestroyProjectile(bridge, e);
                    continue;
                }
                
                bridge.Tick(Time.deltaTime);
                
                if (bridge.Hitbox == null || !bridge.Hitbox.enabled) continue;
                
                var pBounds = bridge.Hitbox.bounds;
                
                foreach (var targetEntity in _targetFilter)
                {
                    if (aProjectile.HitEntities.Contains(targetEntity)) continue;
                    
                    ref var aHealth = ref _healthPool.Get(targetEntity);
                    if (aHealth.IsDead) continue;
                    
                    if (aHealth.Tag == aProjectile.Tag) continue;
                    if (aHealth.IsOnCooldown && !aProjectile.CanHitOnCooldown) continue;
                    
                    if (aHealth.HitBox == null) continue;
                    if (aHealth.HitBox.bounds.Intersects(pBounds))
                    {
                        HandleHitboxIntersect(ref aProjectile, ref aHealth);
                        
                        bridge.RegisterHit();
                        aProjectile.HitEntities.Add(targetEntity);
                        break;
                    }
                }

                if (_entitiesToDestroy.Contains(e)) continue;

                if (bridge.CheckCollisionWithObstacles())
                {
                    bridge.RegisterHit();
                    DestroyProjectile(bridge, e);
                }
            }
        }

        private void TickExplosions()
        {
            foreach (var e in _explosionsFilter)
            {
                ref var aExplosion = ref _explosionPool.Get(e);
                var bridge = aExplosion.Bridge;

                if (bridge == null) continue;
                if (bridge.CheckNeedsDestroy())
                {
                    DestroyExplosion(bridge, e);
                    continue;
                }
                
                bridge.Tick(Time.deltaTime);
                
                if (bridge.HitArea == null || !bridge.HitArea.enabled) continue;
                
                var pCenter = bridge.HitArea.bounds.center;
                foreach (var targetEntity in _targetFilter)
                {
                    if (aExplosion.HitEntities.Contains(targetEntity)) continue;
                    
                    ref var aHealth = ref _healthPool.Get(targetEntity);
                    if (aHealth.IsDead) continue;
                    
                    if (aHealth.Tag == aExplosion.Tag) continue;
                    if (aHealth.IsOnCooldown && !aExplosion.CanHitOnCooldown) continue;
                    
                    if (aHealth.HitBox == null) continue;
                    
                    var hBounds = aHealth.HitBox.bounds;
                    var hCenter = hBounds.center;
                    var distance = (hCenter - pCenter).magnitude;

                    if (distance <= aExplosion.Radius)
                    {
                        HandleHitAreaIntersect(ref aExplosion, ref aHealth, distance);
                        aExplosion.HitEntities.Add(targetEntity);
                        break;
                    }
                }
            }
        }

        private void DestroyProjectile(ProjectileBridge bridge, int entityId)
        {
            if (bridge.ExplosionConfig != null)
            {
                var tag = EcsUtils.HasCompInPool<ProjectileComponent>(_world, entityId, out var projectilePool)
                    ? projectilePool.Get(entityId).Tag
                    : GlobalTag.Default;
                
                SpawnExplosion(bridge.ExplosionConfig, tag, bridge.transform.position, entityId);
            }
            
            bridge.gameObject.SetActive(false);
            _projectileService.DestroyProjectile(entityId);
            _entitiesToDestroy.Add(entityId);
        }
        
        private void SpawnExplosion(ExplosionConfig cfg, GlobalTag tag, Vector3 pos, int projectileEntityId = -1)
        {
            if (cfg == null) return;
            
            var eBridge = _projectileService.SpawnExplosion(cfg, tag, pos);
            
            if (eBridge == null) return;
            if (projectileEntityId == -1) return;
            
            var explosionPool = _world.GetPool<ExplosionComponent>();
            ref var aExplosion = ref explosionPool.Get(eBridge.EntityId);
            
            if (EcsUtils.HasCompInPool<ProjectileComponent>(_world, projectileEntityId, out var projectilePool))
            {
                ref var aProjectile = ref projectilePool.Get(projectileEntityId);
                
                // copy projectile hit entities to new explosion
                foreach (var e in aProjectile.HitEntities)
                {
                    aExplosion.HitEntities.Add(e);
                    // DebCon.Log($"Cloned hit entities from projectile with id {projectileEntityId} => {eBridge.gameObject.name}", "ProjectileSystem", eBridge.gameObject);
                }
                
                // clone necessary values if necessary
                if (cfg.cloneDamage)
                {
                    eBridge.SetDamageValue(aProjectile.Damage);
                    aExplosion.Damage = aProjectile.Damage;
                    // DebCon.Log($"Cloned damage value from projectile with id {projectileEntityId} => {eBridge.gameObject.name}", "ProjectileSystem", eBridge.gameObject);
                }

                if (cfg.clonePush)
                {
                    eBridge.SetPushValues(aProjectile.PushForce, aProjectile.PushUpwardsMod);
                    aExplosion.PushForce = aProjectile.PushForce;
                    aExplosion.PushUpwardsMod = aProjectile.PushUpwardsMod;
                    // DebCon.Log($"Cloned push values from projectile with id {projectileEntityId} => {eBridge.gameObject.name}", "ProjectileSystem", eBridge.gameObject);
                }
            }
        }
        
        private void DestroyExplosion(ExplosionBridge bridge, int entityId)
        {
            bridge.gameObject.SetActive(false);
            _projectileService.DestroyExplosion(entityId);
            _entitiesToDestroy.Add(entityId);
        }
        
        private void HandleHitboxIntersect(ref ProjectileComponent aProjectile, ref HealthComponent aHealth)
        {
            var pCenter = aProjectile.Bridge.Hitbox.bounds.center;
            var hCenter = aHealth.HitBox.bounds.center;
            // var closestPoint = aHealth.HitBox.ClosestPointOnBounds(pCenter);
            
            var hitDir = (hCenter - pCenter).normalized;
            var force = aProjectile.PushForce;
            var upwardsMod = aProjectile.PushUpwardsMod;
            var ignoreHitFix = aProjectile.IgnoreHitFx;
            
            aHealth.Module.StoreHitData(pCenter, hitDir, force, upwardsMod, ignoreHitFix);
            aHealth.Module.ChangeHealth(-aProjectile.Damage);
        }

        private void HandleHitAreaIntersect(ref ExplosionComponent aExplosion, ref HealthComponent aHealth, float distance)
        {
            var eCenter = aExplosion.Bridge.HitArea.bounds.center;
            var hCenter = aHealth.HitBox.bounds.center;
            var radius = aExplosion.Bridge.HitArea.radius;
            // var closestPoint = aHealth.HitBox.ClosestPointOnBounds(eCenter);
            
            var hitDir = (hCenter - eCenter).normalized;
            var force = aExplosion.PushForce;
            var upwardsMod = aExplosion.PushUpwardsMod;
            var ignoreHitFix = aExplosion.IgnoreHitFx;

            var damageMult = distance / radius * aExplosion.DistanceMult;
            var calcDamage = Mathf.Round(aExplosion.Damage * damageMult);
            
            aHealth.Module.StoreHitData(eCenter, hitDir, force, upwardsMod, ignoreHitFix);
            aHealth.Module.ChangeHealth(-calcDamage);
        }
    }
}