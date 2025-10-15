using UnityEngine;
using DI.Services;
using ECS.Bridges;
using Tools;

namespace DI.Factories
{
    using Zenject;
    
    public class ProjectileFactory : IProjectileFactory
    {
        private readonly DiContainer _container;
        private readonly IPoolService _poolService;
        
        public ProjectileFactory(DiContainer container, IPoolService poolService)
        {
            _container = container;
            _poolService = poolService;
        }

        public ProjectileBridge CreateProjectile(GameObject prefab, Vector3 position, Quaternion rotation)
        {
            var bridge = _poolService.Get<ProjectileBridge>(prefab);
            if (bridge == null)
            {
                DebCon.Err("Failed to create projectile bridge", "ProjectileFactory");
                return null;
            }
            
            // inject dependencies at runtime
            _container.Inject(bridge);
            
            bridge.transform.SetPositionAndRotation(position, rotation);
            return bridge;
        }

        public ExplosionBridge CreateExplosion(GameObject prefab, Vector3 position)
        {
            var bridge = _poolService.Get<ExplosionBridge>(prefab);
            if (bridge == null)
            {
                DebCon.Err("Failed to create explosion bridge", "ProjectileFactory");
                return null;
            }
            
            // inject dependencies at runtime
            // _container.Inject(bridge);
            
            bridge.transform.SetPositionAndRotation(position, Quaternion.identity);
            return bridge;
        }
    }
}