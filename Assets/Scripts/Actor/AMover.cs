using UnityEngine;
using Data.Actor;
using ECS.Components;
using ECS.Utils;
using Tools;

namespace Actor
{
    using Leopotam.EcsLite;
    
    public class AMover : MonoBehaviour, IActorModule
    {
        [SerializeField] private Transform tVisuals;
        
        [Header("Movement:")]
        [SerializeField] private float speed = 5f;
        
        [Space]
        [SerializeField] private float uphillStartAngle = 5f;
        [SerializeField] private float uphillMaxAngle = 30f;
        [SerializeField] private float uphillSpeedMultiplier = 0.7f;
        
        [Space]
        [SerializeField] private float downhillStartAngle = 5f;
        [SerializeField] private float downhillMaxAngle = 30f;
        [SerializeField] private float downhillSpeedMultiplier = 1.3f;

        [Header("Jumping:")]
        [SerializeField] private float jumpHeight = 1.5f;
        [SerializeField] private float jumpDelay = 0.1f;
        [SerializeField] private float jumpCooldown = 0.1f;
        [SerializeField] private float velocityDecrement = 10f;

        [Header("Gravity:")]
        [SerializeField] private float gravityMultiplier = 1f;
        [SerializeField] private float fallMultiplier = 2.5f;
        [SerializeField] private float lowJumpMultiplier = 2f;
        
        [Header("Ground Alignment:")]
        [SerializeField] private float maxGroundAngle = 10f;
        [SerializeField] private float rotationSpeed = 10f;
        [SerializeField] private float groundCheckDistance = 0.75f;
        [SerializeField] private float groundCheckOffset = 0.25f;
        [SerializeField] private float groundCheckRadius = 0.5f;
        [SerializeField] private int groundSampleCount = 6;
        
        private Transform _t;
        private CharacterController _controller;

        private float _slopeSpeedMult = 1f;
        private Vector3 _moveDir;
        private bool _isGrounded;
        
        private float _verticalVelocity;
        private float _jumpCooldownLeft = -1;
        private float _jumpDelayLeft = -1;
        
        private bool _isJumpTriggered;
        private bool _isJumpInputReleased = true;

        private float _currentSlopeAngle;
        
        public bool IsEnabled { get; private set; }
        public int EntityId { get; private set; }
        public EcsWorld World { get; private set; }

        public void Init(ActorConfig cfg, int entityId, EcsWorld world)
        {
            IsEnabled = enabled;
            if (!IsEnabled) return;
            
            EntityId = entityId;
            World = world;
            
            _t = transform;
            _controller = GetComponent<CharacterController>();
            
            if (_controller == null)
            {
                DebCon.Err($"Character controller not found on {gameObject.name}!", "AMover", gameObject);
            }

            // init config
            if (cfg)
            {
                speed = cfg.speed;
                
                jumpHeight = cfg.jumpHeight;
                jumpDelay = cfg.jumpDelay;
                jumpCooldown = cfg.jumpCooldown;
                velocityDecrement = cfg.velocityDecrement;
                
                gravityMultiplier = cfg.gravityMultiplier;
                fallMultiplier = cfg.fallMultiplier;
                lowJumpMultiplier = cfg.lowJumpMultiplier;
            }
            
            // add component to pool
            var movementPool = world.GetPool<MovementComponent>();
            movementPool.Add(entityId);
            
            SyncEcsState();
        }

        public void SyncEcsState()
        {
            if (EcsUtils.HasCompInPool<TransformComponent>(World, EntityId, out var transformPool))
            {
                ref var aTransform = ref transformPool.Get(EntityId);
                aTransform.Transform = _t;
                aTransform.Position = _t.position;
            }

            if (EcsUtils.HasCompInPool<MovementComponent>(World, EntityId, out var movementPool))
            {
                var move = _moveDir * (speed * _slopeSpeedMult);
                
                ref var aMovement = ref movementPool.Get(EntityId);
                aMovement.Velocity = new Vector3(move.x, _verticalVelocity, move.z);
                aMovement.IsGrounded = _isGrounded;
                aMovement.HasJumped = _isJumpTriggered && Mathf.Approximately(_jumpDelayLeft, jumpDelay);
            }
        }
        
        public void Tick(float dt)
        {
            if (_controller == null) return;
            if (World == null) return;
            
            var inputPool = World.GetPool<InputComponent>();
            if (!inputPool.Has(EntityId))
            {
                DebCon.Err($"Input component not found on {gameObject.name}!", "AMover",gameObject);
                return;
            }
            
            // todo: think of a better way to kill switch logics 
            if (EcsUtils.HasCompInPool<HealthComponent>(World, EntityId, out var healthPool))
            {
                ref var aHealth = ref healthPool.Get(EntityId);
                var isDead = aHealth.IsDead;

                if (isDead)
                {
                    _moveDir = Vector3.zero;
                    return;
                }
            }
            
            ref var aInput = ref inputPool.Get(EntityId);
            
            // jump related stuff
            HandleJumpLogic(aInput, dt);
            
            // movement related stuff
            _moveDir = CalculateMoveDirection(aInput);
            
            // gravity related stuff
            ApplyGravity(dt);
            
            var groundedInfo = GetGroundInfo();
            _isGrounded = groundedInfo.IsGrounded;
            AlignToGround(groundedInfo.AverageNormal, groundedInfo.HitCount, dt);
            
            // slope movement related stuff
            _slopeSpeedMult = CalculateSlopeSpeedMultiplier(_moveDir);
            var move = _moveDir * (speed * _slopeSpeedMult * dt);
            move.y = _verticalVelocity * dt;
            
            _controller.Move(move);
        }
        
        private Vector3 CalculateMoveDirection(InputComponent input)
        {
            var move = input.Movement;
            
            if (move.magnitude < 0.1f) return Vector3.zero;

            Vector3 moveDirection;
            
            if (input.MainCamera)
            {
                var camRotY = input.MainCamera.transform.eulerAngles.y;
                var camForward = Quaternion.Euler(0, camRotY, 0) * Vector3.forward;
                var camRight = Quaternion.Euler(0, camRotY, 0) * Vector3.right;
                moveDirection = (camForward * move.y + camRight * move.x).normalized;
            }

            else
            {
                moveDirection = new Vector3(move.x, 0, move.y).normalized;
            }

            return moveDirection;
        }
        
        private float CalculateSlopeSpeedMultiplier(Vector3 moveDirection)
        {
            if (!_isGrounded || moveDirection.magnitude < 0.1f) return 1f;
                
            // cast a ray to get the ground normal
            var ray = new Ray(_t.position + _t.up * groundCheckOffset, Vector3.down);
            if (!Physics.Raycast(ray, out var hit, groundCheckDistance)) return 1f;
                
            // calculate the angle between movement direction and ground normal
            var slopeAngle = Vector3.Angle(hit.normal, moveDirection) - 90f;
            
            // if the angle is greater than threshold, we're going uphill
            if (slopeAngle > uphillStartAngle)
            {
                // calculate a multiplier based on how steep the uphill is
                var t = Mathf.InverseLerp(uphillStartAngle, uphillMaxAngle, slopeAngle);
                return Mathf.Lerp(1.0f, uphillSpeedMultiplier, t);
            }
            
            // if the angle is less than negative threshold, we're going downhill
            if (slopeAngle < -downhillStartAngle)
            {
                // calculate a multiplier based on how steep the downhill is
                var t = Mathf.InverseLerp(-downhillStartAngle, -downhillMaxAngle, slopeAngle);
                return Mathf.Lerp(1f, downhillSpeedMultiplier, t);
            }
            
            // use normal speed on flat ground
            return 1f;
        }
        
        private void HandleJumpLogic(InputComponent input, float dt)
        {
            if (_isGrounded && _verticalVelocity < 0)
            {
                _verticalVelocity = -velocityDecrement;
            }

            var canJump = _isGrounded;

            if (input.IsJumpReleased)
            {
                _isJumpInputReleased = true;
            }

            if (_jumpDelayLeft > 0)
            {
                canJump = false;
                _jumpDelayLeft -= dt;
            }

            if (_jumpCooldownLeft > 0)
            {
                canJump = false;
                _jumpCooldownLeft -= dt;
            }

            if (!canJump) return;
            
            _jumpDelayLeft = 0;
            _jumpCooldownLeft = 0;

            if (_isJumpTriggered)
            {
                _isJumpTriggered = false;
                _jumpCooldownLeft = jumpCooldown;
                
                var jumpForce = Mathf.Sqrt(2f * jumpHeight * Mathf.Abs(Physics.gravity.y * gravityMultiplier));
                
                _verticalVelocity = jumpForce;
                canJump = false;
            }

            if (!_isJumpInputReleased
                || !input.IsJumpHeld
                || !canJump) return;
            
            _isJumpInputReleased = false;
            _isJumpTriggered = true;
            _jumpDelayLeft = jumpDelay;
        }
        
        private void ApplyGravity(float dt)
        {
            var gravity = Physics.gravity.y * gravityMultiplier;
            float gravityDelta;

            var vel = _verticalVelocity;
            var isReleased = _isJumpInputReleased;

            if (vel > 0 && isReleased) gravityDelta = gravity * lowJumpMultiplier;
            else if (vel < 0) gravityDelta = gravity * fallMultiplier;
            else gravityDelta = gravity;

            _verticalVelocity += gravityDelta * dt;
            _verticalVelocity = Mathf.Clamp(_verticalVelocity, gravity, 20f);
        }
        
        private void AlignToGround(Vector3 avgNormal, int hitCount, float dt)
        {
            if (!tVisuals) return;
            if (!_isGrounded) return;
    
            _currentSlopeAngle = Vector3.Angle(avgNormal, Vector3.up);
    
            if (_currentSlopeAngle > maxGroundAngle)
            {
                avgNormal = Vector3.RotateTowards(
                    Vector3.up,
                    avgNormal,
                    Mathf.Deg2Rad * maxGroundAngle,
                    0f
                ).normalized;
            }
    
            if (hitCount > 0)
            {
                avgNormal.Normalize();
                
                var targetRot = Quaternion.FromToRotation(Vector3.up, avgNormal) * _t.rotation;
                tVisuals.rotation = Quaternion.Slerp(tVisuals.rotation, targetRot, dt * rotationSpeed);
            }
            
            else
            {
                tVisuals.localRotation = Quaternion.Slerp(tVisuals.localRotation, Quaternion.identity, dt * rotationSpeed);
            }
        }
        
        private (bool IsGrounded, Vector3 AverageNormal, int HitCount) GetGroundInfo()
        {
            var avgNormal = Vector3.zero;
            var hitCount = 0;
    
            // center raycast
            var mainRay = new Ray(_t.position + _t.up * groundCheckOffset, Vector3.down);
            if (Physics.Raycast(mainRay, out var mainHit, groundCheckDistance))
            {
                avgNormal = mainHit.normal;
                hitCount++;
            }
            
            // additional raycasts in a circle around the character
            for (var i = 0; i < groundSampleCount; i++)
            {
                var angle = i * (2 * Mathf.PI / groundSampleCount);
                var offset = new Vector3(Mathf.Cos(angle) * groundCheckRadius, 0, Mathf.Sin(angle) * groundCheckRadius);
                var origin = _t.position + _t.up * groundCheckOffset + offset;
                var ray = new Ray(origin, Vector3.down);
                if (!Physics.Raycast(ray, out var hit, groundCheckDistance)) continue;
        
                avgNormal += hit.normal;
                hitCount++;
        
                Debug.DrawRay(origin, hit.normal, Color.green);
            }
    
            return (hitCount > 0, avgNormal, hitCount);
        }
    }
}