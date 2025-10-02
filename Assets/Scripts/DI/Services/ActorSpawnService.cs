using System;
using UnityEngine;
using DI.Factories;
using ECS.Bridges;
using EventSystem;

namespace DI.Services
{
    using Leopotam.EcsLite;
    
    public class ActorSpawnService : IActorSpawnService
    {
        private readonly EcsWorld _world;
        private readonly IActorFactory _factory;
        private readonly IEventBusService _eventBus;

        public ActorSpawnService(EcsWorld world, IActorFactory factory, IEventBusService eventBus)
        {
            _world = world ?? throw new ArgumentNullException(nameof(world));
            _factory = factory ?? throw new ArgumentNullException(nameof(factory));
            _eventBus = eventBus ?? throw new ArgumentNullException(nameof(eventBus));
        }

        public ActorBridge Spawn(GameObject prefab, Vector3 position, Quaternion rotation)
        {
            var bridge = _factory.Create(prefab, position, rotation);
            if (bridge == null) return null;
            
            var entityId = _world.NewEntity();
            bridge.Init(entityId, _world);
            bridge.Reset();
            
            // used to properly count active actors for spawn-related stuff
            bridge.OnDisabled += HandleBridgeDisabled;
            _eventBus.Publish(new ActorSpawnedEvent(entityId, prefab.name, position));
            
            return bridge;
        }

        private void HandleBridgeDisabled(ActorBridge bridge)
        {
            bridge.OnDisabled -= HandleBridgeDisabled;
            _eventBus.Publish(new ActorDestroyedEvent(bridge.EntityId, "Actor destroyed"));
        }
    }
}