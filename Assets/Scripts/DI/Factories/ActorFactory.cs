using UnityEngine;
using DI.Services;
using ECS.Bridges;
using Tools;

namespace DI.Factories
{
    using Zenject;
    
    public class ActorFactory : IActorFactory
    {
        private readonly DiContainer _container;
        private readonly IPoolService _poolService;

        public ActorFactory(DiContainer container,IPoolService poolService)
        {
            _container = container;
            _poolService = poolService;
        }
        
        public ActorBridge Create(GameObject prefab, Vector3 position, Quaternion rotation)
        {
            var bridge = _poolService.Get<ActorBridge>(prefab);
            if (bridge == null)
            {
                DebCon.Err("Failed to create actor", "ActorFactory");
                return null;
            }
            
            // inject dependencies at runtime
            _container.Inject(bridge);
            
            bridge.transform.SetPositionAndRotation(position, rotation);
            return bridge;
        }
    }
}