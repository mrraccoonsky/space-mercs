using System.Collections.Generic;
using UnityEngine;
using DI.Services;
using ECS.Bridges;
using ECS.Components;

namespace ECS.Systems
{
    using Leopotam.EcsLite;
    
    public class ProjectileSystem : IEcsRunSystem
    {
        private readonly EcsWorld _world;
        private readonly IProjectileService _projectileService;
        
        private readonly EcsPool<ProjectileComponent> _projectilePool;
        private readonly EcsFilter _projectilesFilter;
        
        private readonly EcsPool<HealthComponent> _healthPool;
        private readonly EcsFilter _targetFilter;
        
        private readonly List<int> _entitiesToDestroy = new();
        
        public ProjectileSystem(EcsWorld world, IProjectileService projectileService)
        {
            _world = world;
            _projectileService = projectileService;
            
            _projectilesFilter = _world.Filter<ProjectileComponent>().End();
            _projectilePool = _world.GetPool<ProjectileComponent>();
            
            _healthPool = _world.GetPool<HealthComponent>();
            _targetFilter = _world.Filter<HealthComponent>().End();
        }
        
        public void Run(IEcsSystems systems)
        {
            _entitiesToDestroy.Clear();
            
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
                
                if (bridge.Hitbox == null) continue;
                
                var t = bridge.transform;
                var bounds = bridge.Hitbox.bounds;
                
                foreach (var targetEntity in _targetFilter)
                {
                    if (aProjectile.HitEntities.Contains(targetEntity)) continue;
                    
                    ref var aHealth = ref _healthPool.Get(targetEntity);
                    if (aHealth.Tag == aProjectile.Tag) continue;
                    if (aHealth.IsOnCooldown && !aProjectile.CanHitOnCooldown) continue;
                    
                    if (aHealth.HitBox == null) continue;
                    if (aHealth.HitBox.bounds.Intersects(bounds))
                    {
                        aHealth.Module.RegisterHitData(t.position, t.eulerAngles);
                        aHealth.Module.ChangeHealth(-10f); // todo: make it configurable
                        
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
            
            foreach (var entity in _entitiesToDestroy)
            {
                _world.DelEntity(entity);
            }
        }
        
        private void DestroyProjectile(ProjectileBridge bridge, int entityId)
        {
            bridge.gameObject.SetActive(false);
            _projectileService.Destroy(entityId);
            _entitiesToDestroy.Add(entityId);
        }
    }
}