using UnityEngine;
using ECS.Components;
using Tools;

namespace ECS.AI.States
{
    public class DeadState : BaseAIState
    {
        private float _transitionTime;
        
        public DeadState(AIContext context) : base(context) { }
        
        public override void Enter()
        {
            base.Enter();
            // DebCon.Log($"Entity {Context.EntityId} entered Dead state", "DeadState");
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