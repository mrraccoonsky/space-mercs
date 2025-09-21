using UnityEngine;
using Data.Actor;
using ECS.Components;
using ECS.Utils;
using Tools;

namespace Actor
{
    using Leopotam.EcsLite;
    
    public class AAnimator : MonoBehaviour, IActorModule
    {
        [SerializeField] private Animator animator;
        [SerializeField] private float movementVelocityMult = 0.2f;
        [SerializeField] private float movementSmoothTime = 0.1f;
        
        private Transform _t;
        
        private Vector2 _curMoveVelocity;
        private float _curForwardVelocity;
        private float _curStrafeVelocity;
        
        // mover
        private static readonly int MoveX = Animator.StringToHash("moveX");
        private static readonly int MoveY = Animator.StringToHash("moveY");
        private static readonly int IsJumping = Animator.StringToHash("isJumping");
        private static readonly int IsGrounded = Animator.StringToHash("isGrounded");
        
        // aimer
        private static readonly int IsAiming = Animator.StringToHash("isAiming");
        
        // attacker
        private static readonly int IsAttacking = Animator.StringToHash("isAttacking");
        private static readonly int IsAttackLanded = Animator.StringToHash("isAttackLanded");
        
        // health
        private static readonly int IsHit = Animator.StringToHash("isHit");
        private static readonly int IsDead = Animator.StringToHash("isDead");
        
        private const float MovementEpsilon = 0.01f;
        
        public bool IsEnabled { get; private set; }
        public int EntityId { get; private set; }
        public EcsWorld World { get; private set; }
        
        public void Init(ActorConfig cfg, int entityId, EcsWorld world)
        {
            IsEnabled = enabled;
            if (!IsEnabled) return;
            
            EntityId = entityId;
            World = world;
            
            var animationPool = World.GetPool<AnimationComponent>();
            animationPool.Add(EntityId);
            
            if (animator == null)
            {
                var component = GetComponentInChildren<Animator>();
                if (component == null)
                {
                    DebCon.Err($"Animator not found on {gameObject.name}!", "AAnimator", gameObject);
                    return;
                }
                
                animator = component;
            }
            
            _t = transform;
        }
        
        public void SyncEcsState()
        {
            // nothing to sync... yet
        }

        public void Tick(float dt)
        {
            if (animator == null) return;
            if (World == null) return;
            
            // Input component
            var moveInput = Vector3.zero;
            Transform relativeTo = null;

            if (EcsUtils.HasCompInPool<InputComponent>(World, EntityId, out var inputPool))
            {
                ref var aInput = ref inputPool.Get(EntityId);
                moveInput = aInput.Movement;
                
                if (aInput.MainCamera)
                {
                    relativeTo = aInput.MainCamera.transform;
                }
            }
            
            // Aim component
            var isRelative = false;
            var isAiming = false;
            
            if (EcsUtils.HasCompInPool<AimComponent>(World, EntityId, out var aimPool))
            {
                ref var aAim = ref aimPool.Get(EntityId);

                if (relativeTo == null)
                {
                    relativeTo = aAim.TargetOrigin;
                }
                
                isAiming = aimPool.Has(EntityId) && aAim.IsAiming;
                isRelative = aimPool.Has(EntityId) && isAiming && relativeTo != null;
            }
            
            // Movement component
            var movement = Vector3.zero;
            var hasJumped = false;
            var isGrounded = true;

            if (EcsUtils.HasCompInPool<MovementComponent>(World, EntityId, out var movementPool))
            {
                ref var aMovement = ref movementPool.Get(EntityId);
                var velocity = aMovement.Velocity;
                isGrounded = aMovement.IsGrounded || velocity.magnitude < 0.1f;
                hasJumped = aMovement.HasJumped;
                
                velocity.y = 0f;
                movement = moveInput * velocity.magnitude * movementVelocityMult;
            }
            
            var move = isRelative
                ? UpdateMovementRelative(relativeTo, movement) 
                : UpdateMovementAbsolute(movement);
            
            if (Mathf.Abs(move.x) < MovementEpsilon) move.x = 0f;
            if (Mathf.Abs(move.y) < MovementEpsilon) move.y = 0f;
            
            // Attack component
            var isAttacking = false;
            var isAttackLanded = false;

            if (EcsUtils.HasCompInPool<AttackComponent>(World, EntityId, out var attackPool))
            {
                ref var aAttack = ref attackPool.Get(EntityId);
                isAttacking = aAttack.IsAttacking;
                isAttackLanded = false;
            }
            
            // Health Component
            var isDead = false;
            var isHit = false;
            
            if (EcsUtils.HasCompInPool<HealthComponent>(World, EntityId, out var healthPool))
            {
                ref var aHealth = ref healthPool.Get(EntityId);
                isDead = aHealth.IsDead;
                isHit = aHealth.IsHit;
            }
            
            // Pass values
            animator.SetFloat(MoveX, move.x);
            animator.SetFloat(MoveY, move.y);
            animator.SetBool(IsJumping, hasJumped);
            animator.SetBool(IsGrounded, isGrounded);
            
            // aimer
            animator.SetBool(IsAiming, isAiming);
            
            // attacker
            animator.SetBool(IsAttacking, isAttacking);
            animator.SetBool(IsAttackLanded, isAttackLanded);
            
            // health
            animator.SetBool(IsDead, isDead);
            animator.SetBool(IsHit, isHit);
        }

        private Vector2 UpdateMovementRelative(Transform relativeTo, Vector2 movement, bool invert = false)
        {
            // get relative forward and right vectors (ignoring Y to keep movement horizontal)
            var forward = relativeTo.forward;
            forward.y = 0;
            forward.Normalize();
            
            if (invert)
                forward *= -1;
                
            var right = relativeTo.right;
            right.y = 0;
            right.Normalize();
            
            if (invert)
                right *= -1;
            
            // calculate movement direction to relative transform and convert to character's local space
            var moveDir = (forward * movement.y + right * movement.x).normalized;
            var localMove = _t.InverseTransformDirection(moveDir);
            
            // smoothly interpolate the movement values
            var tarX = localMove.x;
            var tarY = localMove.z;
                
            var curX = Mathf.SmoothDamp(animator.GetFloat(MoveX),
                tarX,
                ref _curStrafeVelocity,
                movementSmoothTime);
                
            var curY = Mathf.SmoothDamp(animator.GetFloat(MoveY),
                tarY,
                ref _curForwardVelocity,
                movementSmoothTime);
                
            return new Vector2(curX, curY);
        }
        
        private Vector2 UpdateMovementAbsolute(Vector2 movement)
        {
            // when not aiming, smoothly interpolate to forward movement
            var targetSpeed = movement.magnitude;
                
            // smoothly return strafe to zero
            var curX = Mathf.SmoothDamp(animator.GetFloat(MoveX),
                0,
                ref _curStrafeVelocity,
                movementSmoothTime);
            
            var curY = Mathf.SmoothDamp(animator.GetFloat(MoveY),
                targetSpeed,
                ref _curForwardVelocity,
                movementSmoothTime);
            
            return new Vector2(curX, curY);
        }
    }
}