using System;
using UnityEngine;
using Actor;
using Data;
using Data.Actor;
using ECS.Components;
using ECS.Utils;
using Tools;

namespace ECS.Bridges
{
    using Leopotam.EcsLite;
    using NaughtyAttributes;
    using Zenject;
    
    [SelectionBase]
    public class ActorBridge : MonoBehaviour, IEcsBridge
    {
        public event Action<ActorBridge> OnDisabled;
        
        [SerializeField, Expandable] private ActorConfig config;
        
        [SerializeField, ReadOnly] private GlobalTag globalTag;

        private Transform _t;
        private IActorModule[] _modules;
        
        public int EntityId { get; private set; }
        public EcsWorld World { get; private set; }
        
        // exposed to make use of pool init to pre-spawn projectiles and explosions
        public ActorConfig Config => config;
        
        [Inject]
        public void Construct(DiContainer container)
        {
            UpdateModules();
            foreach (var module in _modules)
            {
                container.Inject(module);
            }
        }

        private void OnDisable()
        {
            OnDisabled?.Invoke(this);
        }
        
        public void Init(int entityId, EcsWorld world)
        {
            // init ecs
            EntityId = entityId;
            World = world;
            
            _t = transform;
            
            var actorPool = World.GetPool<ActorComponent>();
            ref var aActor = ref actorPool.Add(EntityId);
            aActor.Bridge = this;
            
            // init self + modules
            var transformPool = World.GetPool<TransformComponent>();
            ref var aTransform = ref transformPool.Add(EntityId);
            
            aTransform.Transform = _t;
            aTransform.Position = _t.position;
            aTransform.Rotation = _t.rotation;

            UpdateModules();
            foreach (var module in _modules)
            {
                module.Init(config, EntityId, World);
            }
            
            DebCon.Log($"{name}:{EntityId} Init done!", "ActorBridge", gameObject);
        }
        
        public void Reset()
        {
            foreach (var module in _modules)
            {
                module.Reset();
            }
            
            SyncEcsState();
            gameObject.SetActive(true);
        }
        
        public void SetTag(GlobalTag globalTag)
        {
            this.globalTag = globalTag;
            
            UpdateModules();
            foreach (var module in _modules)
            {
                module.SetTag(globalTag);
            }
        }
        
        public void Tick(float dt)
        {
            foreach (var module in _modules)
            {
                if (module.IsEnabled)
                {
                    module.Tick(dt);
                }
            }

            SyncEcsState();
        }

        private void SyncEcsState()
        {
            if (EcsUtils.HasCompInPool<ActorComponent>(World, EntityId, out var actorPool))
            {
                ref var aActor = ref actorPool.Get(EntityId);
                aActor.Tag = globalTag;
            }
            
            foreach (var module in _modules)
            {
                if (module.IsEnabled)
                {
                    module.SyncEcsState();
                }
            }
        }

        private void UpdateModules(bool force = false)
        {
            if (_modules != null && !force) return;
            _modules = GetComponentsInChildren<IActorModule>();
        }
    }
}
