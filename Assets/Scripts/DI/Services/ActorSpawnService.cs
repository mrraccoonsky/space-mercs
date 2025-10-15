using System;
using UnityEngine;
using Data;
using DI.Factories;
using ECS.Bridges;
using ECS.Components;
using EventSystem;
using Tools;

namespace DI.Services
{
    using Leopotam.EcsLite;
    
    public class ActorSpawnService : IActorSpawnService
    {
        private readonly EcsWorld _world;
        private readonly GlobalVarsConfig _globalVars;
        private readonly IActorFactory _factory;
        private readonly IEventBusService _eventBus;

        public ActorSpawnService(EcsWorld world, GlobalVarsConfig globalVars, IActorFactory factory, IEventBusService eventBus)
        {
            _world = world ?? throw new ArgumentNullException(nameof(world));
            _globalVars = globalVars ?? throw new ArgumentNullException(nameof(globalVars));
            _factory = factory ?? throw new ArgumentNullException(nameof(factory));
            _eventBus = eventBus ?? throw new ArgumentNullException(nameof(eventBus));
        }

        public ActorBridge Spawn(GameObject prefab, Vector3 position, Quaternion rotation)
        {
            var bridge = _factory.Create(prefab, position, rotation);
            if (bridge == null) return null;
            
            var entityId = _world.NewEntity();
            
            // renaming and setting proper tags
            GlobalTag tag;
            if (bridge.TryGetComponent(out AIActorBridge ai))
            {
                ai.Init(entityId, _world);
                tag = GlobalTag.Enemy;
            }
            else
            {
                var inputPool = _world.GetPool<InputComponent>();
                inputPool.Add(entityId);
                tag = GlobalTag.Player;

                DebCon.Log($"Added input component to entity {entityId}", "ActorBridge");
            }

            var tagCfg = _globalVars.TagConfig;
            if (tagCfg != null && tagCfg.TryGetEditorTag(tag, out var editorTag))
            {
                bridge.gameObject.tag = editorTag;

                /* var displayTag = tagCfg.GetDisplayTag(tag);
                if (!bridge.name.Contains(displayTag))
                {
                    bridge.name = $"[{displayTag}] {bridge.name}";
                } */
            }
            
            bridge.Init(entityId, _world);
            bridge.SetTag(tag);
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