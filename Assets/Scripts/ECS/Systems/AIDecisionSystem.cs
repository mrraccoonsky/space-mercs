using System.Collections.Generic;
using Data.AI;
using DI.Services;
using UnityEngine;
using ECS.AI.States;
using ECS.Components;
using ECS.Utils;
using EventSystem;
using Tools;
using Zenject;

namespace ECS.Systems
{
    using Leopotam.EcsLite;
    
    public class AIDecisionSystem : IEcsRunSystem, IEcsInitSystem, IEcsDestroySystem
    {
        private readonly EcsWorld _world;
        private readonly IEventBusService _eventBus;
        
        private EcsFilter _aiFilter;

        private readonly Dictionary<int, AIStateMachine> _stateMachines = new();
        
        public AIDecisionSystem(EcsWorld world, IEventBusService eventBus)
        {
            _world = world;
            _eventBus = eventBus;
        }
        
        public void Init(IEcsSystems systems)
        {
            _aiFilter = _world.Filter<TransformComponent>()
                .Inc<InputComponent>()
                .Inc<AIControlledComponent>()
                .Inc<AIPerceptionComponent>()
                .Inc<AIBehaviorComponent>()
                .End();
            
            _eventBus.Subscribe<ActorSpawnedEvent>(HandleActorSpawned);
        }
        
        public void Destroy(IEcsSystems systems)
        {
            DebCon.Warn("Destroying AIDecisionSystem...");
            
            _eventBus.Unsubscribe<ActorSpawnedEvent>(HandleActorSpawned);
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
                    DebCon.Warn($"No state machine found for entity {entity}", "AIDecisionSystem");
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
                            aPerception.LastKnownTargetPosition = aHealth.LastHit.Pos - aHealth.LastHit.Dir;
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
                    if (aBehavior.CurrentState == AIBehaviorState.Idle || aBehavior.StateTimer > aBehavior.AttackCooldown)
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
        
        private void HandleActorSpawned(ActorSpawnedEvent e)
        {
            var entityId = e.EntityId;
            
            if (!EcsUtils.HasCompInPool<AIControlledComponent>(_world, entityId, out var aiPool))
            {
                DebCon.Info($"AI-controlled component not found on entity {entityId}", "AIDecisionSystem");
                return;
            }
            
            if (!EcsUtils.HasCompInPool<InputComponent>(_world, entityId, out var inputPool))
            {
                inputPool.Add(entityId);
                DebCon.Log($"Added input component to entity {entityId}", "AIDecisionSystem");
            }
            
            if (!EcsUtils.HasCompInPool<AIBehaviorComponent>(_world, entityId, out var behaviorPool))
            {
                behaviorPool.Add(entityId);
                DebCon.Log($"Added behavior component to entity {entityId}", "AIDecisionSystem");
            }
            
            ref var aAI = ref aiPool.Get(entityId);
            var cfg = aAI.Config;
            
            // init with current state
            ref var aBehavior = ref behaviorPool.Get(entityId);
            aBehavior.CurrentState = AIBehaviorState.Idle;
            aBehavior.StateTimer = 0f;
            LoadBehaviorConfig(ref aBehavior, cfg);
            
            var context = new AIContext(entityId, _world);
            var stateMachine = new AIStateMachine(context);
            
            stateMachine.RegisterState(AIBehaviorState.Idle, new IdleState(context));
            stateMachine.RegisterState(AIBehaviorState.Patrol, new PatrolState(context));
            stateMachine.RegisterState(AIBehaviorState.Chase, new ChaseState(context));
            stateMachine.RegisterState(AIBehaviorState.Attack, new AttackState(context));
            stateMachine.RegisterState(AIBehaviorState.Dead, new DeadState(context));
            stateMachine.Init(aBehavior.CurrentState);
            
            _stateMachines[entityId] = stateMachine;
            DebCon.Log($"Entity {entityId} initialized", "AIDecisionSystem");
        }
        
        private void LoadBehaviorConfig(ref AIBehaviorComponent aBehavior, AIConfig cfg)
        {
            if (cfg != null)
            {
                aBehavior.DetectionRadius = cfg.detectionRadius;
                aBehavior.AttackRange = cfg.attackRange;
                aBehavior.AttackCooldown = cfg.attackCooldown;
                aBehavior.RandomChaseAttackMinTime = cfg.randomChaseAttackMinTime;
                aBehavior.RandomChaseAttackMaxTime = cfg.randomChaseAttackMaxTime;
            }
            else
            {
                aBehavior.DetectionRadius = 10f;
                aBehavior.AttackRange = 1.5f;
                aBehavior.AttackCooldown = 1f;
                aBehavior.RandomChaseAttackMinTime = 1f;
                aBehavior.RandomChaseAttackMaxTime = 3f;
            }
        }
    }
}