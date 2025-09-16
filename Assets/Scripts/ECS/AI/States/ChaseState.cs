using UnityEngine;
using ECS.Components;

namespace ECS.AI.States
{
    public class ChaseState : BaseAIState
    {
        public ChaseState(AIContext context) : base(context) { }
        
        public override void GenerateInput(ref InputComponent input, float dt)
        {
            base.GenerateInput(ref input, dt);
            
            var perceptionPool = Context.World.GetPool<AIPerceptionComponent>();
            ref var aPerception = ref perceptionPool.Get(Context.EntityId);

            var direction = aPerception.DirectionToTarget.normalized;
            input.Movement = new Vector2(direction.x, direction.z);

            // aim at target during chase
            if (aPerception.LastKnownTargetPosition != Vector3.zero)
            {
                input.IsAimHeld = true;
                input.AimPosition = aPerception.LastKnownTargetPosition;
            }
        }
    }
}