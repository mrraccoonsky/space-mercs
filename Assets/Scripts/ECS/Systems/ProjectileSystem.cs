using System.Collections.Generic;
using UnityEngine;
using ECS.Bridges;
using ECS.Components;
using Factories;

namespace ECS.Systems
{
    using Leopotam.EcsLite;
    
    public class ProjectileSystem : IEcsRunSystem
    {
        private readonly EcsWorld _world;
        private readonly EcsFilter _projectilesFilter;
        
        private readonly EcsPool<ProjectileComponent> _projectilePool;
        private readonly EcsPool<HealthComponent> _healthPool;
        
        private readonly EcsFilter _healthFilter;
        private readonly int _obstacleLayer;

        private readonly List<int> _entitiesToDestroy = new();
        
        public ProjectileSystem(EcsWorld world)
        {
            _world = world;
            _projectilesFilter = _world.Filter<ProjectileComponent>().End();
            _projectilePool = _world.GetPool<ProjectileComponent>();
            
            _healthPool = _world.GetPool<HealthComponent>();
            _healthFilter = _world.Filter<HealthComponent>().End();
            
            _obstacleLayer = LayerMask.GetMask("Obstacle");
        }
        
        public void Run(IEcsSystems systems)
        {
            _entitiesToDestroy.Clear();
            
            foreach (var e in _projectilesFilter)
            {
                ref var aProjectile = ref _projectilePool.Get(e);
                var bridge = aProjectile.Bridge;
                var t = bridge.transform;

                if (bridge == null) continue;
                if (bridge.ShouldBeDestroyed())
                {
                    DestroyProjectile(bridge, e);
                    continue;
                }
                
                bridge.Tick(Time.deltaTime);
                
                if (bridge.Hitbox == null) continue;
                var bounds = bridge.Hitbox.bounds;
                
                foreach (var targetEntity in _healthFilter)
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
                
                var ray = new Ray(t.position - t.forward, t.forward);
                if (!Physics.Raycast(ray, 1f, _obstacleLayer)) continue;
                
                bridge.RegisterHit();
                DestroyProjectile(bridge, e);
            }
            
            foreach (var entity in _entitiesToDestroy)
            {
                _world.DelEntity(entity);
            }
        }
        
        private void DestroyProjectile(ProjectileBridge bridge, int entityId)
        {
            ProjectileFactory.ReturnToPool(bridge);
            _entitiesToDestroy.Add(entityId);
        }
    }
}