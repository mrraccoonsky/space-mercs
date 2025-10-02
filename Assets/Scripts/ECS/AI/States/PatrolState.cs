using UnityEngine;
using ECS.Components;
using Tools;

namespace ECS.AI.States
{
    public class PatrolState : BaseAIState
    {
        public PatrolState(AIContext context) : base(context) { }
        
        private Vector3 _targetPos;
        private Vector3 _direction;
        private float _cooldownTimer;
        
        public override void Enter()
        {
            base.Enter();
         
            _targetPos = Vector3.zero;
            _cooldownTimer = 0f;
        }
        
        public override void Update(float dt)
        {
            base.Update(dt);
            
            var behaviorPool = Context.World.GetPool<AIBehaviorComponent>();
            ref var aBehavior = ref behaviorPool.Get(Context.EntityId);
            
            if (_targetPos == Vector3.zero)
            {
                FindPatrolTarget(ref aBehavior);
                DebCon.Log($"Entity {Context.EntityId} set patrol target to {_targetPos}", "PatrolState");
            }
            else
            {
                if (_direction.magnitude < 0.5f)
                {
                    _cooldownTimer += dt;
                    
                    if (_cooldownTimer > 1f)
                    {
                        DebCon.Log($"Entity {Context.EntityId} has reached patrol target!", "PatrolState");
                        SwitchState(AIBehaviorState.Idle);
                        return;
                    }
                }
            }
            
            if (aBehavior.StateTimer > 5f + Random.value * 3f) // todo: use behavior config
            {
                SwitchState(AIBehaviorState.Idle);
            }
        }
        
        public override void GenerateInput(ref InputComponent input, float dt)
        {
            base.GenerateInput(ref input, dt);
            
            var transformPool = Context.World.GetPool<TransformComponent>();
            ref var aTransform = ref transformPool.Get(Context.EntityId);
            var pos = aTransform.Position;
            
            if (_targetPos != Vector3.zero)
            {
                _direction = _targetPos - pos;
                _direction.y = 0f;
                
                if (_direction.magnitude >= 0.5f)
                {
                    input.Movement = new Vector2(_direction.x, _direction.z).normalized;
                }
            }
            /* else
            {
                input.Movement = new Vector2(Mathf.Sin(Time.time * 0.5f), Mathf.Cos(Time.time * 0.3f));
            } */
        }
        
        private void FindPatrolTarget(ref AIBehaviorComponent behavior)
        {
            var transformPool = Context.World.GetPool<TransformComponent>();
            ref var transform = ref transformPool.Get(Context.EntityId);
            var position = transform.Position;
            
            for (var i = 0; i < 10; i++)
            {
                // todo: use a more reasonable patrol radius based on detection radius
                var radius = behavior.DetectionRadius;
                var randomDir = new Vector3(
                    Random.Range(-1f, 1f), 
                    0f, 
                    Random.Range(-1f, 1f)
                ).normalized * Random.Range(3f, radius);
                
                // todo: use navmesh to find valid patrol positions
                var raycastPos = position + randomDir;
                raycastPos.y += 10f;

                if (!Physics.Raycast(raycastPos, Vector3.down, out var hit, 20f, ~0)) continue;
                _targetPos = hit.point;
                return;
            }
            
            // fallback if no valid position found
            _targetPos = position + new Vector3(Random.Range(-3f, 3f), 0, Random.Range(-3f, 3f));
        }
    }
}