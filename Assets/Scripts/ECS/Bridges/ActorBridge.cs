using System;
using UnityEngine;
using Actor;
using Data;
using Data.Actor;
using ECS.Components;
using Tools;

namespace ECS.Bridges
{
    using Leopotam.EcsLite;
    using Zenject;
    
    [SelectionBase]
    public class ActorBridge : MonoBehaviour, IEcsBridge
    {
        public event Action<ActorBridge> OnDisabled;
        
        [SerializeField] private ActorConfig config;

        private Transform _t;
        private IActorModule[] _modules;
        
        public int EntityId { get; private set; }
        public EcsWorld World { get; private set; }

        private GlobalVariablesConfig _globalVars;

        [Inject]
        public void Construct(DiContainer container)
        {
            _globalVars = container.Resolve<GlobalVariablesConfig>();

            foreach (var module in GetComponentsInChildren<IActorModule>())
            {
                container.Inject(module);
            }
        }

        private void OnDisable()
        {
            OnDisabled?.Invoke(this);
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
            
            // renaming and setting proper tags
            string strTag;
            if (gameObject.TryGetComponent(out AIActorBridge ai))
            {
                ai.Init(EntityId, World);
                strTag = "AI";
            }
            else
            {
                var inputPool = World.GetPool<InputComponent>();
                inputPool.Add(entityId);
                strTag = "Player";

                DebCon.Log($"Added input component to entity {entityId}", "ActorBridge");
            }
            
            if (_globalVars?.TagConfig != null && _globalVars.TagConfig.TryGetTag(strTag, out var globalTag))
            {
                gameObject.tag = globalTag;
                gameObject.name = $"[{globalTag}] {gameObject.name}";
            }
            
            _modules = GetComponentsInChildren<IActorModule>();
            foreach (var module in _modules)
            {
                module.Init(config, EntityId, World);
            }
            
            SyncEcsState();
            DebCon.Log($"{name}:{EntityId} Init done!", "ActorBridge", gameObject);
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
            foreach (var module in _modules)
            {
                if (module.IsEnabled)
                {
                    module.SyncEcsState();
                }
            }
        }
    }
}
