using UnityEngine;
using ECS.Components;

namespace ECS.AI.States
{
    public class DeadState : BaseAIState
    {
        private float _transitionTime;
        
        public DeadState(AIContext context) : base(context) { }
        
        public override void Enter()
        {
            base.Enter();
            // Debug.Log($"[IdleState] Entity {_context.EntityId} entered Dead state;
        }
        
        public override void Update(float dt)
        {
            base.Update(dt);
        }
        
        public override void GenerateInput(ref InputComponent input, float dt)
        {
            base.GenerateInput(ref input, dt);
        }
    }
}