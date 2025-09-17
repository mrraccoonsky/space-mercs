using UnityEngine;
using ECS.Components;
using Tools;
using Random = UnityEngine.Random;

namespace ECS.AI.States
{
    public class IdleState : BaseAIState
    {
        private float _transitionTime;
        
        public IdleState(AIContext context) : base(context) { }
        
        public override void Enter()
        {
            base.Enter();
            
            _transitionTime = 3f + Random.value * 2f;
        }
        
        public override void Update(float dt)
        {
            base.Update(dt);
            
            var behaviorPool = Context.World.GetPool<AIBehaviorComponent>();
            ref var aBehavior = ref behaviorPool.Get(Context.EntityId);
            
            if (aBehavior.StateTimer > _transitionTime)
            {
                // DebCon.Info($"Entity {Context.EntityId} transitioning to PATROL state after {aBehavior.StateTimer:F1}s (threshold: {_transitionTime:F1}s)");
                SwitchState(AIBehaviorState.Patrol);
            }
        }
        
        public override void GenerateInput(ref InputComponent input, float dt)
        {
            base.GenerateInput(ref input, dt);
            
            var perceptionPool = Context.World.GetPool<AIPerceptionComponent>();
            ref var aPerception = ref perceptionPool.Get(Context.EntityId);
            
            if (aPerception.LastKnownTargetPosition != Vector3.zero)
            {
                input.IsAimHeld = true;
                input.AimPosition = aPerception.LastKnownTargetPosition;
            }
        }
    }
}