using UnityEngine;
using Data.Projectile;
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

        public ProjectileBridge Create(ProjectileData data, Vector3 position, Quaternion rotation)
        {
            var bridge = _poolService.Get<ProjectileBridge>(data.prefab);
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
    }
}