using UnityEngine;
using DI.Services;
using ECS.Components;
using ECS.Utils;
using EventSystem;
using Tools;

namespace ECS.Systems
{
    using Leopotam.EcsLite;
    
    public class AIPerceptionSystem : IEcsRunSystem, IEcsInitSystem, IEcsDestroySystem
    {
        private readonly EcsWorld _world;
        private readonly IEventBusService _eventBus;
        
        private EcsFilter _aiFilter;
        private EcsFilter _targetFilter;
        
        public AIPerceptionSystem(EcsWorld world, IEventBusService eventBus)
        {
            _world = world;
            _eventBus = eventBus;
        }
        
        public void Init(IEcsSystems systems)
        {
            _aiFilter = _world.Filter<TransformComponent>()
                .Inc<AIControlledComponent>()
                .End();
            
            _targetFilter = _world.Filter<TransformComponent>()
                .Inc<ActorComponent>()
                .End();

            _eventBus.Subscribe<ActorSpawnedEvent>(HandleActorSpawned);
        }
        
        public void Destroy(IEcsSystems systems)
        {
            DebCon.Warn("Destroying AIPerceptionSystem...");
            _eventBus.Unsubscribe<ActorSpawnedEvent>(HandleActorSpawned);
        }
        
        public void Run(IEcsSystems systems)
        {
            var transformPool = _world.GetPool<TransformComponent>();
            var aiPool = _world.GetPool<AIControlledComponent>();
            var behaviorPool = _world.GetPool<AIBehaviorComponent>();
            var perceptionPool = _world.GetPool<AIPerceptionComponent>();
            
            // update perception for all AI entities
            foreach (var entity in _aiFilter)
            {
                ref var aAI = ref aiPool.Get(entity);
                ref var aBehavior = ref behaviorPool.Get(entity);
                ref var aPerception = ref perceptionPool.Get(entity);

                if (EcsUtils.HasCompInPool<HealthComponent>(_world, entity, out var healthPool))
                {
                    ref var aHealth = ref healthPool.Get(entity);
                    var isDead = aHealth.IsDead;

                    if (isDead)
                    {
                        ResetPerceptionData(ref aPerception);
                        continue;
                    }
                }
                
                if (!TryFindTargetEntity(entity, out var possibleTargetId))
                {
                    ResetPerceptionData(ref aPerception);
                    // continue;
                }
                
                if (possibleTargetId >= 0)
                {
                    // self transform data
                    ref var aTransform = ref transformPool.Get(entity);
                    var pos = aTransform.Position;
                
                    // target transform data
                    ref var aTargetTransform = ref transformPool.Get(possibleTargetId);
                    var targetPos = aTargetTransform.Position;
                    var targetDir = targetPos - pos;
                
                    // various checks
                    var detectionRadiusPass = CheckDistance(aBehavior.DetectionRadius, targetDir.magnitude);
                    var healthCheckPass = CheckHealth(possibleTargetId);
                    var lineOfSightPass = aAI.Bridge == null || aAI.Bridge.CheckLineOfSight(pos, targetPos);
                    
                    aPerception.DetectionRadiusPass = detectionRadiusPass;
                    aPerception.HealthCheckPass = healthCheckPass;
                    aPerception.LineOfSightPass = lineOfSightPass;
                
                    var perceptionChecksPass = detectionRadiusPass && healthCheckPass && lineOfSightPass;
                    if (perceptionChecksPass)
                    {
                        aPerception.targetEntityId = possibleTargetId;
                        aPerception.DistanceToTarget = targetDir.magnitude;
                        aPerception.DirectionToTarget = targetDir.normalized;
                        aPerception.TimeSinceLastSawTarget = 0f;
                        aPerception.LastKnownTargetPosition = targetPos + Vector3.up;
                    }
                    else
                    {
                        UpdateTimeSinceLastSawTarget(ref aPerception);
                    }
                }
                else
                {
                    UpdateTimeSinceLastSawTarget(ref aPerception);
                }
            }
        }
        
        private bool TryFindTargetEntity(int aiEntityId, out int targetEntityId)
        {
            var transformPool = _world.GetPool<TransformComponent>();
            var actorPool = _world.GetPool<ActorComponent>();

            ref var aTransform = ref transformPool.Get(aiEntityId);
            ref var aActor = ref actorPool.Get(aiEntityId);

            var pos = aTransform.Position;
            var tag = aActor.Tag;
            
            var minDistance = float.MaxValue;
            var targetId = -1;
            var result = false;
            
            foreach (var entity in _targetFilter)
            {
                ref var aTargetTransform = ref transformPool.Get(entity);
                var targetPos = aTargetTransform.Position;
                var distance = (targetPos - pos).magnitude;
                if (distance > minDistance) continue;
                
                // ignore own tag
                ref var aTargetActor = ref actorPool.Get(entity);
                if (aTargetActor.Tag == tag) continue;

                // ignore dead entities
                if (EcsUtils.HasCompInPool<HealthComponent>(_world, entity, out var healthPool))
                {
                    ref var aHealth = ref healthPool.Get(entity);
                    if (aHealth.IsDead) continue;
                }
                
                minDistance = distance;
                targetId = entity;
                result = true;
            }

            targetEntityId = result ? targetId : -1;
            return result;
        }

        private bool CheckDistance(float detectionRadius, float distanceToTarget)
        {
            return distanceToTarget <= detectionRadius;
        }
        
        private bool CheckHealth(int targetEntityId)
        {
            if (!EcsUtils.HasCompInPool<HealthComponent>(_world, targetEntityId, out var healthPool)) return true;
            ref var aHealth = ref healthPool.Get(targetEntityId);
            return !aHealth.IsDead;
        }
        
        private void UpdateTimeSinceLastSawTarget(ref AIPerceptionComponent aPerception)
        {
            aPerception.TimeSinceLastSawTarget += Time.deltaTime;
            
            if (aPerception.LastKnownTargetPosition != Vector3.zero &&
                aPerception.TimeSinceLastSawTarget >= 1f)
            {
                ResetPerceptionData(ref aPerception);
            }
        }

        private void ResetPerceptionData(ref AIPerceptionComponent aPerception)
        {
            aPerception.targetEntityId = -1;
            
            aPerception.DistanceToTarget = float.MaxValue;
            aPerception.DirectionToTarget = Vector3.zero;
            aPerception.TimeSinceLastSawTarget = 0f;
            aPerception.LastKnownTargetPosition = Vector3.zero;
            
            aPerception.DetectionRadiusPass = false;
            aPerception.HealthCheckPass = false;
            aPerception.LineOfSightPass = false;
        }

        private void HandleActorSpawned(ActorSpawnedEvent e)
        {
            var entityId = e.EntityId;
            
            if (!EcsUtils.HasCompInPool<AIControlledComponent>(_world, entityId))
            {
                DebCon.Info($"AI-controlled component not found on entity {entityId}", "AIPerceptionSystem");
                return;
            }
                
            // initialize with default values
            if (!EcsUtils.HasCompInPool<AIPerceptionComponent>(_world, entityId, out var perceptionPool))
            {
                perceptionPool.Add(entityId);
                DebCon.Log($"Added perception component to entity {entityId}", "AIPerceptionSystem");
            }
            
            ref var aPerception = ref perceptionPool.Get(entityId);
            ResetPerceptionData(ref aPerception);
            DebCon.Log($"Entity {entityId} initialized with default perception values", "AIPerceptionSystem");
        }
    }
}
