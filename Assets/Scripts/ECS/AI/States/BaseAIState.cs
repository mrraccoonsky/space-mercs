using UnityEngine;
using ECS.Components;
using Tools;

namespace ECS.AI.States
{
    public abstract class BaseAIState : IAIState
    {
        protected readonly AIContext Context;

        protected BaseAIState(AIContext context)
        {
            Context = context;
        }
        
        public virtual void Enter()
        {
            if (Context.World == null)
            {
                DebCon.Err($"Entity {Context.EntityId} has no World reference!", "BaseAIState");
                return;
            }
        }

        public virtual void Update(float dt)
        {
            if (Context.World == null)
            {
                // DebCon.Err($"Entity {Context.EntityId} has no World reference!", "BaseAIState");
                return;
            }
        }

        public virtual void Exit()
        {
            if (Context.World == null)
            {
                DebCon.Err($"Entity {Context.EntityId} has no World reference!", "BaseAIState");
                return;
            }
        }

        public virtual void GenerateInput(ref InputComponent input, float dt)
        {
            // reset input at the start of each tick
            input.Movement = Vector2.zero;
            input.IsJumpHit = false;
            input.IsJumpHeld = false;
            input.IsJumpReleased = false;
            input.IsAimHit = false;
            input.IsAimHeld = false;
            input.IsAimReleased = false;
            input.IsAttackHit = false;
            input.IsAttackHeld = false;
            input.IsAttackReleased = false;
            input.AimPosition = Vector3.zero;
            
            if (Context.World == null)
            {
                // DebCon.Err($"Entity {Context.EntityId} has no World reference!", "BaseAIState");
                return;
            }
        }
        
        protected void SwitchState(AIBehaviorState newState, bool force = false)
        {
            Context.StateMachine.SwitchState(newState, force);
        }
    }
}