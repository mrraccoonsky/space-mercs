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

            var dt = Time.deltaTime;
            var fdt = Time.fixedDeltaTime;
            TickProjectiles(dt, fdt);
            TickExplosions(dt);
            
            foreach (var entity in _entitiesToDestroy)
            {
                _world.DelEntity(entity);
            }
        }

        private void TickProjectiles(float dt, float fdt)
        {
            foreach (var entityId in _projectilesFilter)
            {
                ref var aProjectile = ref _projectilePool.Get(entityId);
                var pBridge = aProjectile.Bridge;

                if (pBridge == null) continue;
                if (pBridge.CheckNeedsDestroy())
                {
                    DestroyProjectile(pBridge, entityId);
                    continue;
                }
                
                pBridge.Tick(dt);
                pBridge.FixedTick(fdt);
                
                // collision check
                var hitbox = aProjectile.HitBox;
                if (hitbox == null || !hitbox.enabled) continue;
                var pBounds = hitbox.bounds;
                
                // homing-related values
                var curTarget = Vector3.zero;
                var minDistance = aProjectile.AimRange;
                
                foreach (var targetEntity in _targetFilter)
                {
                    if (aProjectile.HitEntities.Contains(targetEntity)) continue;

                    ref var aHealth = ref _healthPool.Get(targetEntity);
                    if (aHealth.IsDead) continue;
                    if (aHealth.Tag == aProjectile.Tag) continue;
                    if (aHealth.HitBox == null) continue;
                    
                    if (aProjectile.EnableAim)
                    {
                        var hCenter = aHealth.HitBox.bounds.center;

                        if (aProjectile.AimTarget != Vector3.zero)
                        {
                            var distance = (hCenter - aProjectile.AimTarget).magnitude;
                            if (distance < 0.1f)
                            {
                                curTarget = hCenter;
                            }
                        }
                        else
                        {
                            var targetPass = CheckAimTargetPass(entityId, aProjectile, hCenter);

                            var currentDir = pBridge.transform.forward;
                            var targetDir = (hCenter - pBounds.center).normalized;
                            var dot = Vector3.Dot(currentDir, targetDir);
                            var dotPass = dot > aProjectile.AimDot;
                        
                            var distance = (hCenter - pBounds.center).magnitude;
                            var distancePass = distance < minDistance;
                            if (targetPass && dotPass && distancePass)
                            {
                                minDistance = distance;
                                curTarget = hCenter;
                            }
                        }
                    }
                    
                    if (aHealth.IsOnCooldown && !aProjectile.CanHitOnCooldown) continue;
                    if (aHealth.HitBox.bounds.Intersects(pBounds))
                    {
                        HandleHitboxIntersect(ref aProjectile, ref aHealth);
                        
                        pBridge.RegisterHit();
                        aProjectile.HitEntities.Add(targetEntity);
                        break;
                    }
                }
                    
                aProjectile.AimTarget = curTarget;
                
                // early exit if projectile is already marked for destroy
                if (_entitiesToDestroy.Contains(entityId)) continue;
                
                if (pBridge.CheckCollisionWithObstacles())
                {
                    pBridge.RegisterHit();
                    DestroyProjectile(pBridge, entityId);
                }
            }
        }
        
        private void TickExplosions(float dt)
        {
            foreach (var entityId in _explosionsFilter)
            {
                ref var aExplosion = ref _explosionPool.Get(entityId);
                var eBridge = aExplosion.Bridge;

                if (eBridge == null) continue;
                if (eBridge.CheckNeedsDestroy())
                {
                    DestroyExplosion(eBridge, entityId);
                    continue;
                }
                
                eBridge.Tick(dt);

                var hitArea = aExplosion.HitArea;
                if (hitArea == null || !hitArea.enabled) continue;
                var eCenter = hitArea.bounds.center;
                
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
                    var distance = (hCenter - eCenter).magnitude;

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
                
                SpawnExplosion(bridge.ExplosionConfig, tag, bridge.Position, entityId);
            }
            
            bridge.gameObject.SetActive(false);
            _projectileService.DestroyProjectile(entityId);
            _entitiesToDestroy.Add(entityId);
        }
        
        private void DestroyExplosion(ExplosionBridge bridge, int entityId)
        {
            bridge.gameObject.SetActive(false);
            _projectileService.DestroyExplosion(entityId);
            _entitiesToDestroy.Add(entityId);
        }
        
        private void SpawnExplosion(ExplosionConfig cfg, GlobalTag tag, Vector3 pos, int projectileEntityId = -1)
        {
            if (cfg == null) return;
            
            var eBridge = _projectileService.SpawnExplosion(cfg, tag, pos);
            if (eBridge == null) return;
            
            var explosionPool = _world.GetPool<ExplosionComponent>();
            ref var aExplosion = ref explosionPool.Get(eBridge.EntityId);
            
            // clone necessary values if necessary
            if (projectileEntityId == -1) return;
            if (!EcsUtils.HasCompInPool<ProjectileComponent>(_world, projectileEntityId, out var projectilePool)) return;
            
            ref var aProjectile = ref projectilePool.Get(projectileEntityId);
            
            // copy projectile hit entities to new explosion to avoid double hits
            foreach (var e in aProjectile.HitEntities)
            {
                aExplosion.HitEntities.Add(e);
            }

            // force pass data to bridge if we are cloning something from projectile
            if (TryCloneProjectileData(cfg, ref aExplosion, ref aProjectile))
            {
                eBridge.ForceUpdateFromComponent();
            }
        }
        
        private bool TryCloneProjectileData(ExplosionConfig cfg, ref ExplosionComponent aExplosion, ref ProjectileComponent aProjectile)
        {
            // clone scale-radius
            if (cfg.cloneScale)
            {
                aExplosion.Scale = aProjectile.Scale;
                aExplosion.Radius = cfg.scaleAffectsRadius ? aProjectile.Scale * cfg.radius : cfg.radius;;
            }
                
            // clone damage
            if (cfg.cloneDamage)
            {
                aExplosion.Damage = aProjectile.Damage;
            }
            
            // clone knockback
            if (cfg.cloneKnockback)
            {
                aExplosion.KnockbackForce = aProjectile.KnockbackForce;
                aExplosion.KnockbackDuration = aProjectile.KnockbackDuration;
            }

            // clone ragdoll-related forces
            if (cfg.clonePush)
            {
                aExplosion.PushForce = aProjectile.PushForce;
                aExplosion.PushUpwardsMod = aProjectile.PushUpwardsMod;
            }

            return cfg.cloneScale || cfg.cloneDamage || cfg.cloneKnockback || cfg.clonePush;
        }
        
        private bool CheckAimTargetPass(int entityId, ProjectileComponent aProjectile, Vector3 targetPos)
        {
            if (aProjectile.CanReuseTarget) return true; // skip if can reuse target that are taken by other projectiles
            
            foreach (var eId in _projectilesFilter)
            {
                if (eId == entityId) continue; // skip self
                                
                ref var pProjectile = ref _projectilePool.Get(eId);
                if (!pProjectile.EnableAim) continue; // skip non-aiming projectiles
                    
                var distance = (pProjectile.AimTarget - targetPos).magnitude;
                if (distance > 0.1f) continue; // skip projectiles that are not targeting the same target

                return false;
            }

            return true;
        }
        
        private void HandleHitboxIntersect(ref ProjectileComponent aProjectile, ref HealthComponent aHealth)
        {
            var pCenter = aProjectile.HitBox.bounds.center;
            var hCenter = aHealth.HitBox.bounds.center;
            
            var hitData = new HitData
            {
                Pos = aHealth.HitBox.ClosestPointOnBounds(pCenter),
                Dir = (hCenter - pCenter).normalized,
                IgnoreFx = aProjectile.IgnoreHitFx,
                KnockbackForce = aProjectile.KnockbackForce,
                KnockbackDuration = aProjectile.KnockbackDuration,
                PushForce = aProjectile.PushForce,
                PushUpwardsMod = aProjectile.PushUpwardsMod
            };
            
            aHealth.Module.StoreHitData(ref hitData);
            aHealth.Module.ChangeHealth(-aProjectile.Damage);
        }

        private void HandleHitAreaIntersect(ref ExplosionComponent aExplosion, ref HealthComponent aHealth, float distance)
        {
            var eCenter = aExplosion.HitArea.bounds.center;
            var eRadius = aExplosion.HitArea.radius;
            var hCenter = aHealth.HitBox.bounds.center;
            
            // todo: make those calculations optional based on cfg flag
            var distanceMult = distance / eRadius * aExplosion.DistanceMult;
            distanceMult = Mathf.Round(distanceMult * 100f) / 100f;
            
            var hitData = new HitData
            {
                Pos = aHealth.HitBox.ClosestPointOnBounds(eCenter),
                Dir = (hCenter - eCenter).normalized,
                IgnoreFx = aExplosion.IgnoreHitFx,
                KnockbackForce = aExplosion.KnockbackForce * distanceMult,
                KnockbackDuration = aExplosion.KnockbackDuration,
                PushForce = aExplosion.PushForce * distanceMult,
                PushUpwardsMod = aExplosion.PushUpwardsMod * distanceMult
            };

            var calcDamage = aExplosion.Damage * distanceMult;
            
            aHealth.Module.StoreHitData(ref hitData);
            aHealth.Module.ChangeHealth(-calcDamage);
        }
    }
}