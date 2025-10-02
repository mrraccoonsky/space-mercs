using UnityEngine;
using ECS.Components;

namespace ECS.AI.States
{
    public class ChaseState : BaseAIState
    {
        public ChaseState(AIContext context) : base(context) { }

        private float _attackSwitchTime;
        
        public override void Enter()
        {
            _attackSwitchTime = 0f;
            
            var behaviorPool = Context.World.GetPool<AIBehaviorComponent>();
            ref var aBehavior = ref behaviorPool.Get(Context.EntityId);

            if (aBehavior is { RandomChaseAttackMinTime: > 0f, RandomChaseAttackMaxTime: > 0f })
            {
                _attackSwitchTime = Random.Range(aBehavior.RandomChaseAttackMinTime, aBehavior.RandomChaseAttackMaxTime);
            }
        }
        
        public override void Update(float dt)
        {
            // randomly switch to attack state if possible
            if (_attackSwitchTime > 0f)
            {
                _attackSwitchTime -= dt;
                
                if (_attackSwitchTime <= 0f)
                {
                    SwitchState(AIBehaviorState.Attack);
                }
            }
        }
        
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