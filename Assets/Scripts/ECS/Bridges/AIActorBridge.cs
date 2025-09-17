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
        [ReadOnly, SerializeField] private bool fovAnglePass;
        
        private Transform _t;
        
        public int EntityId { get; private set; }
        public EcsWorld World { get; private set; }

        private void OnDrawGizmos()
        {
            if (!_t) return;

            // FOV cone visualization
            /* Color color;
            if (!fovAnglePass)
            {
                color = Color.gray;
            }
            else if (!lineOfSightPass || !healthCheckPass || !detectionRadiusPass)
            {
                color = Color.darkGreen;
            }
            else
            {
                color = Color.green;
            }

            Gizmos.color = color;
            var displayPos = _t.position + Vector3.up;
            var angle = config.fieldOfViewAngle / 2f;
            var forward = _t.forward * config.detectionRadius;
            
            var leftConeEdge = displayPos + Quaternion.Euler(0, angle, 0) * forward;
            var rightConeEdge = displayPos + Quaternion.Euler(0, -angle, 0) * forward;
                
            Gizmos.DrawLine(displayPos, displayPos + forward);
            Gizmos.DrawLine(displayPos, leftConeEdge);
            Gizmos.DrawLine(displayPos, rightConeEdge); */
            
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
            
            /* if (_agent == null)
            {
                if (!TryGetComponent(out _agent))
                {
                    _agent = gameObject.AddComponent<NavMeshAgent>();
                }
            } */

            if (!EcsUtils.HasCompInPool<AIControlledComponent>(world, entityId, out var aiPool))
            {
                aiPool.Add(entityId);
                DebCon.Log($"'{gameObject.name}' marked as AI-controlled", "AIActorBridge", gameObject);
                
                ref var aiControlled = ref aiPool.Get(entityId);
                aiControlled.Bridge = this;
                aiControlled.Agent = agent;
                aiControlled.Config = config;
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
    }
}
