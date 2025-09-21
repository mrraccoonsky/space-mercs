using UnityEngine;
using Data;
using Data.AI;
using ECS.Components;
using ECS.Utils;

namespace ECS.Systems
{
    using Leopotam.EcsLite;
    using Zenject;
    
    public class AIPerceptionSystem : IEcsRunSystem, IEcsInitSystem
    {
        private readonly EcsWorld _world;
        private readonly EcsFilter _aiFilter;
        private readonly EcsFilter _targetFilter;
        
        public AIPerceptionSystem(EcsWorld world)
        {
            _world = world;
            
            _aiFilter = _world.Filter<TransformComponent>()
                .Inc<AIControlledComponent>()
                .End();
            
            _targetFilter = _world.Filter<TransformComponent>()
                .Inc<ActorComponent>()
                .End();
        }
        
        public void Init(IEcsSystems systems)
        {
            var aiControlledPool = _world.GetPool<AIControlledComponent>();
            foreach (var entity in _aiFilter)
            {
                ref var aiControlled = ref aiControlledPool.Get(entity);
                var cfg = aiControlled.Config;
                
                if (!EcsUtils.HasCompInPool<AIBehaviorComponent>(_world, entity, out var behaviorPool))
                {
                    ref var aBehavior = ref behaviorPool.Add(entity);
                    aBehavior.CurrentState = AIBehaviorState.Idle;
                    aBehavior.StateTimer = 0f;
                    LoadBehaviorConfig(ref aBehavior, cfg);
                }
                
                // initialize with default values
                if (!EcsUtils.HasCompInPool<AIPerceptionComponent>(_world, entity, out var perceptionPool))
                {
                    ref var aPerception = ref perceptionPool.Add(entity);
                    ResetPerceptionData(ref aPerception);
                }
            }
        }
        
        public void Run(IEcsSystems systems)
        {
            var transformPool = _world.GetPool<TransformComponent>();
            var aiControlledPool = _world.GetPool<AIControlledComponent>();
            var behaviorPool = _world.GetPool<AIBehaviorComponent>();
            var perceptionPool = _world.GetPool<AIPerceptionComponent>();
            
            // update perception for all AI entities
            foreach (var entity in _aiFilter)
            {
                ref var aiControlled = ref aiControlledPool.Get(entity);
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
                    var lineOfSightPass = aiControlled.Bridge == null || aiControlled.Bridge.CheckLineOfSight(pos, targetPos, possibleTargetId);
                    
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
            var tag = aActor.Bridge.gameObject.tag;
            
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
                // todo: use inner (fraction) tag instead of gameObject based one
                ref var aTargetActor = ref actorPool.Get(entity);
                if (aTargetActor.Bridge.gameObject.CompareTag(tag)) continue;

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
        
        private void LoadBehaviorConfig(ref AIBehaviorComponent aBehavior, AIConfig cfg)
        {
            if (cfg != null)
            {
                aBehavior.DetectionRadius = cfg.detectionRadius;
                aBehavior.FieldOfViewAngle = cfg.fieldOfViewAngle;
                aBehavior.AttackRange = cfg.attackRange;
                aBehavior.AttackCooldown = cfg.attackCooldown;
            }
            else
            {
                aBehavior.DetectionRadius = 10f;
                aBehavior.FieldOfViewAngle = 60f;
                aBehavior.AttackRange = 1.5f;
                aBehavior.AttackCooldown = 1f;
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
    }
}
