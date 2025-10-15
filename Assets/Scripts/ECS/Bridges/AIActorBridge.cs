using UnityEngine;
using UnityEngine.AI;
using Data.AI;
using ECS.Components;
using ECS.Utils;
using Tools;

namespace ECS.Bridges
{
    using Leopotam.EcsLite;
    using NaughtyAttributes;
    
    [SelectionBase]
    public class AIActorBridge : MonoBehaviour, IEcsBridge
    {
        [SerializeField] private NavMeshAgent agent;
        [SerializeField] private AIConfig config;
        [SerializeField] private LayerMask obstacleLayerMask;
     
        // debug values display
        [ReadOnly, SerializeField] private AIBehaviorState currentState;
        [ReadOnly, SerializeField] private float stateTimer;
        
        [ReadOnly, SerializeField] private int targetEntityId;
        [ReadOnly, SerializeField] private float distanceToTarget;
        [ReadOnly, SerializeField] private float timeSinceLastSawTarget;
        [ReadOnly, SerializeField] private Vector3 lastKnownTargetPos;
        
        [ReadOnly, SerializeField] private bool hasTarget;
        [ReadOnly, SerializeField] private bool detectionRadiusPass;
        [ReadOnly, SerializeField] private bool healthCheckPass;
        [ReadOnly, SerializeField] private bool lineOfSightPass;
        
        private Transform _t;
        
        public int EntityId { get; private set; }
        public EcsWorld World { get; private set; }

        private void OnDrawGizmos()
        {
            if (!_t) return;
            
            // last known target position / line of sight visualization
            if (lastKnownTargetPos != Vector3.zero)
            {
                Gizmos.color = Color.yellow;
                Gizmos.DrawWireCube(lastKnownTargetPos, Vector3.one);
                
                Gizmos.color = lineOfSightPass ? Color.red : Color.crimson;
                Gizmos.DrawLine(_t.position + Vector3.up, lastKnownTargetPos);
            }
        }

        public void Init(int entityId, EcsWorld world)
        {
            _t = transform;
            
            // init ecs
            EntityId = entityId;
            World = world;

            if (!EcsUtils.HasCompInPool<AIControlledComponent>(world, entityId, out var aiPool))
            {
                aiPool.Add(entityId);
                DebCon.Log($"'{gameObject.name}' marked as AI-controlled", "AIActorBridge", gameObject);
                
                ref var aAI = ref aiPool.Get(entityId);
                aAI.Bridge = this;
                aAI.Agent = agent;
                aAI.Config = config;
            }
        }

        public void Tick(float dt)
        {
            if (EcsUtils.HasCompInPool<AIBehaviorComponent>(World, EntityId, out var behaviorPool))
            {
                ref var behavior = ref behaviorPool.Get(EntityId);
                currentState = behavior.CurrentState;
                stateTimer = behavior.StateTimer;
            }

            if (EcsUtils.HasCompInPool<AIPerceptionComponent>(World, EntityId, out var perceptionPool))
            {
                ref var perception = ref perceptionPool.Get(EntityId);
                
                targetEntityId = perception.targetEntityId;
                distanceToTarget = perception.DistanceToTarget;
                timeSinceLastSawTarget = perception.TimeSinceLastSawTarget;
                lastKnownTargetPos = perception.LastKnownTargetPosition;
                
                detectionRadiusPass = perception.DetectionRadiusPass;
                healthCheckPass = perception.HealthCheckPass;
                lineOfSightPass = perception.LineOfSightPass;
                
                hasTarget = perception.HasTarget;
            }
        }
        
        public bool CheckLineOfSight(Vector3 from, Vector3 to)
        {
            var aFrom = from + Vector3.up;
            var aTo = to + Vector3.up;
            
            var targetDirection = aTo - aFrom;
            var targetDistance = targetDirection.magnitude;
            targetDirection = targetDirection.normalized;

            return !Physics.Raycast(aFrom, targetDirection, targetDistance, obstacleLayerMask);
        }
    }
}
