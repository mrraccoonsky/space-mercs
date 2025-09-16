using System.Collections.Generic;
using UnityEngine;
using ECS.Components;
using ECS.Utils;

namespace ECS.AI.States
{
    public class AIStateMachine
    {
        private readonly AIContext context;
        private readonly Dictionary<AIBehaviorState, IAIState> _states = new();
        
        private IAIState _currentState;
        private AIBehaviorState _currentStateType;
        
        public AIStateMachine(AIContext context)
        {
            this.context = context;
            this.context.StateMachine = this;
        }
        
        public AIBehaviorState CurrentStateType => _currentStateType;
        
        public void RegisterState(AIBehaviorState stateType, IAIState state)
        {
            _states[stateType] = state;
        }
        
        public void Init(AIBehaviorState initialState)
        {
            _currentStateType = initialState;
            _currentState = _states[initialState];
            _currentState.Enter();
        }
        
        public void Tick(ref InputComponent input, float dt)
        {
            var behaviorPool = context.World.GetPool<AIBehaviorComponent>();
            ref var aBehavior = ref behaviorPool.Get(context.EntityId);
            aBehavior.StateTimer += dt;
            
            _currentState?.Update(dt);
            _currentState?.GenerateInput(ref input, dt);
        }
        
        public void SwitchState(AIBehaviorState newState, bool force = false)
        {
            if (!_states.ContainsKey(newState))
            {
                Debug.LogError($"[AIStateMachine] State {newState} not registered!");
                return;
            }

            if (!force && IsInState(newState))
            {
                // Debug.Log($"[AIStateMachine] Entity {context.EntityId} is already in state {newState}");
                return;
            }

            _currentState?.Exit();
            Debug.Log($"[AIStateMachine] Entity {context.EntityId} transitioning from {_currentStateType} to {newState}");
            
            _currentStateType = newState;
            _currentState = _states[newState];
            _currentState.Enter();
            
            if (EcsUtils.HasCompInPool<AIBehaviorComponent>(context.World, context.EntityId, out var behaviorPool))
            {
                ref var behavior = ref behaviorPool.Get(context.EntityId);
                behavior.CurrentState = newState;
                behavior.StateTimer = 0f;
            }
        }
        
        public bool IsInState(AIBehaviorState stateType)
        {
            return _currentStateType == stateType;
        }
    }
}