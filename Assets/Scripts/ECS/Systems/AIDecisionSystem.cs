using System.Collections.Generic;
using UnityEngine;
using ECS.AI.States;
using ECS.Components;
using ECS.Utils;

namespace ECS.Systems
{
    using Leopotam.EcsLite;
    
    public class AIDecisionSystem : IEcsRunSystem, IEcsInitSystem
    {
        private readonly EcsWorld _world;
        private readonly EcsFilter _aiFilter;

        private readonly Dictionary<int, AIStateMachine> _stateMachines = new();
        
        public AIDecisionSystem(EcsWorld world)
        {
            _world = world;
            
            _aiFilter = _world.Filter<TransformComponent>()
                .Inc<InputComponent>()
                .Inc<AIControlledComponent>()
                .Inc<AIPerceptionComponent>()
                .Inc<AIBehaviorComponent>()
                .End();
        }
        
        public void Init(IEcsSystems systems)
        {
            // init input
            var filter = _world.Filter<AIControlledComponent>().Inc<ActorComponent>().End();
            foreach (var entity in filter)
            {
                if (!EcsUtils.HasCompInPool<InputComponent>(_world, entity, out var inputPool))
                {
                    inputPool.Add(entity);
                    Debug.Log($"[AIDecisionSystem] Added input component to entity {entity}");
                }
            }
                
            Debug.Log($"[AIDecisionSystem] Initialized with {_aiFilter.GetEntitiesCount()} AI entities");
            
            // init state machines for each AI entity
            foreach (var entity in _aiFilter)
            {
                var context = new AIContext(entity, _world);
                var stateMachine = new AIStateMachine(context);
                
                stateMachine.RegisterState(AIBehaviorState.Idle, new IdleState(context));
                stateMachine.RegisterState(AIBehaviorState.Patrol, new PatrolState(context));
                stateMachine.RegisterState(AIBehaviorState.Chase, new ChaseState(context));
                stateMachine.RegisterState(AIBehaviorState.Attack, new AttackState(context));
                stateMachine.RegisterState(AIBehaviorState.Dead, new DeadState(context));
                
                // init with current state
                var behaviorPool = _world.GetPool<AIBehaviorComponent>();
                ref var aBehavior = ref behaviorPool.Get(entity);
                stateMachine.Init(aBehavior.CurrentState);
                
                _stateMachines[entity] = stateMachine;
            }
        }
        
        public void Run(IEcsSystems systems)
        {
            var inputPool = _world.GetPool<InputComponent>();
            var perceptionPool = _world.GetPool<AIPerceptionComponent>();
            var behaviorPool = _world.GetPool<AIBehaviorComponent>();
            
            foreach (var entity in _aiFilter)
            {
                ref var aInput = ref inputPool.Get(entity);
                ref var aPerception = ref perceptionPool.Get(entity);
                ref var aBehavior = ref behaviorPool.Get(entity);
                
                if (!_stateMachines.TryGetValue(entity, out var stateMachine))
                {
                    Debug.LogWarning($"[AIDecisionSystem] No state machine found for entity {entity}");
                    continue;
                }
                
                CheckGlobalStateTransitions(entity, ref aBehavior, ref aPerception, ref stateMachine);
                
                var dt = Time.deltaTime;
                stateMachine.Tick(ref aInput, dt);
            }
        }
        
        private void CheckGlobalStateTransitions(int entity, ref AIBehaviorComponent aBehavior, ref AIPerceptionComponent aPerception, ref AIStateMachine stateMachine)
        {
            // check for SELF health-based transitions
            if (EcsUtils.HasCompInPool<HealthComponent>(_world, entity, out var healthPool))
            {
                ref var aHealth = ref healthPool.Get(entity);
                
                var isDead = aHealth.IsDead;
                if (isDead)
                {
                    stateMachine.SwitchState(AIBehaviorState.Dead);
                    return;
                }
                
                var isHit = aHealth.IsHit;
                if (isHit)
                {
                    switch (stateMachine.CurrentStateType)
                    {
                        case AIBehaviorState.Idle:
                        case AIBehaviorState.Patrol:
                            aPerception.LastKnownTargetPosition = aHealth.LastHitPosition - aHealth.LastHitDirection;
                            stateMachine.SwitchState(AIBehaviorState.Chase);
                            return;
                    }
                }
            }

            if (aPerception.HasTarget)
            {
                var detectionRadiusPass = aPerception.DetectionRadiusPass;
                var healthPass = aPerception.HealthCheckPass;
                var lineOfSightPass = aPerception.LineOfSightPass;
            
                if (detectionRadiusPass && healthPass && lineOfSightPass)
                {
                    // switch to attack if target is within attack range
                    if (aPerception.DistanceToTarget <= aBehavior.AttackRange)
                    {
                        stateMachine.SwitchState(AIBehaviorState.Attack);
                        return;
                    }
                
                    // switch to chase if target is within chase range
                    if (aBehavior.StateTimer > 1f)
                    {
                        stateMachine.SwitchState(AIBehaviorState.Chase);
                    }
                    
                    return;
                }
            
                // switch to search / idle state if target was previously seen but was lost
                if (aPerception.TimeSinceLastSawTarget > 1f)
                {
                    stateMachine.SwitchState(AIBehaviorState.Idle);
                }
            }
            else
            {
                switch (stateMachine.CurrentStateType)
                {
                    case AIBehaviorState.Attack:
                    case AIBehaviorState.Chase:
                        stateMachine.SwitchState(AIBehaviorState.Idle);
                        break;
                }
            }
        }
    }
}