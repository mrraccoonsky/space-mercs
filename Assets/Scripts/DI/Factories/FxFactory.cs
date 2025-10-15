using UnityEngine;
using DI.Services;
using Tools;

namespace DI.Factories
{
    public class FxFactory : IFxFactory
    {
        // private readonly DiContainer _container;
        private readonly IPoolService _poolService;
        
        public FxFactory(IPoolService poolService)
        {
            // _container = container;
            _poolService = poolService;
        }
        
        public ParticleSystem Create(GameObject prefab, Vector3 position, Quaternion rotation)
        {
            var fx = _poolService.Get<ParticleSystem>(prefab);
            if (fx == null)
            {
                DebCon.Err("Failed to create FX", "FxFactory");
                return null;
            }
            
            // inject dependencies at runtime
            // _container.Inject(fx);
            
            fx.transform.SetPositionAndRotation(position, rotation);
            return fx;
        }
    }
}