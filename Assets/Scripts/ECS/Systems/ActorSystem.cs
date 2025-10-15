using System.Collections.Generic;
using UnityEngine;
using Core.Camera;
using ECS.Bridges;
using ECS.Components;
using ECS.Utils;

namespace ECS.Systems
{
    using Leopotam.EcsLite;
    
    public class ActorSystem : IEcsRunSystem
    {
        private readonly EcsWorld _world;
        private readonly EcsFilter _actorFilter;
        private readonly EcsFilter _playerFilter;
        private readonly EcsFilter _aiFilter;
        private readonly CameraController _cameraController;
        
        private readonly List<int> _entitiesToDestroy = new();
        
        private const float AI_DESPAWN_TIME = 3f;
        private const float AI_DESPAWN_THRESHOLD = 0.2f;
        
        public ActorSystem(EcsWorld world, CameraController cameraController)
        {
            _world = world;
            
            _actorFilter = _world.Filter<ActorComponent>().End();
            _playerFilter = _world.Filter<ActorComponent>().Exc<AIControlledComponent>().End();
            _aiFilter = _world.Filter<ActorComponent>().Inc<AIControlledComponent>().End();
            
            _cameraController = cameraController;
        }
        
        public void Run(IEcsSystems systems)
        {
            _entitiesToDestroy.Clear();
            
            var actorPool = _world.GetPool<ActorComponent>();
            var aiPool = _world.GetPool<AIControlledComponent>();
            var dt = Time.deltaTime;
            
            foreach (var e in _actorFilter)
            {
                ref var aActor = ref actorPool.Get(e);
                var bridge = aActor.Bridge;

                if (EcsUtils.HasCompInPool<HealthComponent>(_world, e, out var healthPool))
                {
                    ref var aHealth = ref healthPool.Get(e);
                
                    if (CheckNeedsDestroy(ref aHealth))
                    {
                        if (_entitiesToDestroy.Contains(e)) continue;
                    
                        DestroyActor(bridge, e);
                        continue;
                    }
                }
                
                bridge?.Tick(dt);
            }

            foreach (var e in _aiFilter)
            {
                if (_entitiesToDestroy.Contains(e)) continue;
                
                ref var aAI = ref aiPool.Get(e);
                var aiBridge = aAI.Bridge;
                aiBridge?.Tick(dt);
                
                // despawn AI if it's out of view for too long
                if (_cameraController != null)
                {
                    var transformPool = _world.GetPool<TransformComponent>();
                    ref var aTransform = ref transformPool.Get(e);

                    // w/ some threshold to prevent AI from being despawned when it's just out of view
                    if (!_cameraController.CheckIfPointIsVisible(aTransform.Position, AI_DESPAWN_THRESHOLD))
                    {
                        aAI.LastVisibleTime += dt;
                        if (aAI.LastVisibleTime < AI_DESPAWN_TIME) continue;
                        
                        ref var aActor = ref actorPool.Get(e);
                        var bridge = aActor.Bridge;
                            
                        DestroyActor(bridge, e);
                    }
                    else
                    {
                        aAI.LastVisibleTime = 0f;
                    }
                }
            }
            
            foreach (var entity in _entitiesToDestroy)
            {
                _world.DelEntity(entity);
            }
            
            // reacquire camera target if it's null
            UpdateCameraTarget(ref actorPool);
        }

        private bool CheckNeedsDestroy(ref HealthComponent health)
        {
            return health is { IsDead: true, DeadTimer: <= 0f };
        }
        
        private void DestroyActor(ActorBridge bridge, int entityId)
        {
            TryResetCameraTarget(bridge.transform);
            
            bridge.gameObject.SetActive(false);
            _entitiesToDestroy.Add(entityId);
        }

        // todo: try to find a better place to handle this
        private void TryResetCameraTarget(Transform target)
        {
            if (_cameraController.CurrentTarget == target)
            {
                _cameraController.SetCameraTarget(null);
            }
        }

        // todo: try to find a better place to handle this
        private void UpdateCameraTarget(ref EcsPool<ActorComponent> actorPool)
        {
            if (_cameraController.CurrentTarget != null) return;
            
            foreach (var e in _playerFilter)
            {
                ref var aActor = ref actorPool.Get(e);
                if (aActor.Bridge != null)
                {
                    _cameraController.SetCameraTarget(aActor.Bridge.transform);
                    _cameraController.ResetForwardOnly(1f);
                }
            }
        }
    }
}