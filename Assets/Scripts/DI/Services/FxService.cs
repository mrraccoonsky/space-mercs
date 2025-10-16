using System;
using DI.Factories;
using UnityEngine;

namespace DI.Services
{
    public class FxService : IFxService
    {
        private readonly IFxFactory _factory;
        
        public FxService(IFxFactory factory)
        {
            _factory = factory ?? throw new ArgumentNullException(nameof(factory));
        }
        
        public void Spawn(GameObject prefab, Vector3 position, Quaternion rotation, Vector3 scale)
        {
            var fx = _factory.Create(prefab, position, rotation, scale);
            if (fx == null) return;

            fx.gameObject.SetActive(true);
            fx.Play();
        }
    }
}
