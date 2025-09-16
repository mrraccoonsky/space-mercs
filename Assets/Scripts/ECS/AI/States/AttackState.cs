using ECS.Components;

namespace ECS.AI.States
{
    public class AttackState : BaseAIState
    {
        public AttackState(AIContext context) : base(context) { }

        private float _attackCooldownTimer;

        public override void Enter()
        {
            base.Enter();
            
            _attackCooldownTimer = 0f;
        }
        
        public override void Update(float dt)
        {
            base.Update(dt);
            
            if (_attackCooldownTimer > 0f)
            {
                _attackCooldownTimer -= dt;
            }
        }
        
        public override void GenerateInput(ref InputComponent input, float dt)
        {
            base.GenerateInput(ref input, dt);
            
            var behaviorPool = Context.World.GetPool<AIBehaviorComponent>();
            var perceptionPool = Context.World.GetPool<AIPerceptionComponent>();
            
            ref var aBehavior = ref behaviorPool.Get(Context.EntityId);
            ref var aPerception = ref perceptionPool.Get(Context.EntityId);
            
            input.IsAimHeld = true;
            input.AimPosition = aPerception.LastKnownTargetPosition;
            input.IsAttackHeld = _attackCooldownTimer <= 0f;
            
            if (_attackCooldownTimer <= 0f)
            {
                _attackCooldownTimer = aBehavior.AttackCooldown;
            }
        }
    }
}