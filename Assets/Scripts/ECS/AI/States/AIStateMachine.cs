using System.Collections.Generic;
using UnityEngine;
using ECS.Components;
using ECS.Utils;
using Tools;

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
        
        private static string GetStateColor(AIBehaviorState state)
        {
            var hash = state.ToString().GetHashCode();
            var r = (byte)System.Math.Max(127, System.Math.Abs(hash % 256));
            var g = (byte)System.Math.Max(127, System.Math.Abs((hash >> 8) % 256));
            var b = (byte)System.Math.Max(127, System.Math.Abs((hash >> 16) % 256));
            
            return $"{r:X2}{g:X2}{b:X2}FF";
        }
        
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
                DebCon.Err($"State {newState} not registered on entity {context.EntityId}!", "AIStateMachine");
                return;
            }

            if (!force && IsInState(newState))
            {
                // DebCon.Log($"Entity {context.EntityId} is already in state {newState}", "AIStateMachine");
                return;
            }

            _currentState?.Exit();
            
            // Format the state names as colored text using string interpolation
            var fromState = $"<color=#{GetStateColor(_currentStateType)}>{_currentStateType}</color>";
            var toState = $"<color=#{GetStateColor(newState)}>{newState}</color>";
            DebCon.Log($"Entity {context.EntityId} transitioning from {fromState} to {toState}...", "AIStateMachine");
            
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