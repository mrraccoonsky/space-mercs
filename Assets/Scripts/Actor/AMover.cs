using UnityEngine;
using Core.Camera;
using Data.Actor;
using ECS.Components;
using ECS.Utils;
using Tools;

namespace Actor
{
    using Leopotam.EcsLite;
    using NaughtyAttributes;
    using Zenject;
    
    public class AMover : MonoBehaviour, IActorModule
    {
        [SerializeField] private Transform tVisuals;
        [SerializeField] private float screenClampBuffer;   // clamps the actor's position to the camera's viewport if > 0 

        [Header("Slope Modifiers")]
        [SerializeField] private bool enableSlopeSpeedCalculation;
        
        [Space]
        [SerializeField] private float uphillStartAngle = 5f;
        [SerializeField] private float uphillMaxAngle = 30f;
        [SerializeField] private float uphillSpeedMultiplier = 0.7f;
        
        [Space]
        [SerializeField] private float downhillStartAngle = 5f;
        [SerializeField] private float downhillMaxAngle = 30f;
        [SerializeField] private float downhillSpeedMultiplier = 1.3f;

        [Header("Ground Alignment")]
        [SerializeField] private LayerMask walkableLayerMask;
        [SerializeField] private float maxGroundAngle = 10f;
        [SerializeField] private float rotationSpeed = 10f;
        [SerializeField] private float groundCheckDistance = 0.75f;
        [SerializeField] private float groundCheckOffset = 0.25f;
        [SerializeField] private float groundCheckRadius = 0.5f;
        [SerializeField] private int groundSampleCount = 6;

        [Space]
        [ReadOnly, SerializeField] private float speed;
        
        [Space]
        [ReadOnly, SerializeField] private float jumpHeight;
        [ReadOnly, SerializeField] private float jumpDelay;
        [ReadOnly, SerializeField] private float jumpCooldown;
        [ReadOnly, SerializeField] private float velocityDecrement;

        [Space]
        [ReadOnly, SerializeField] private float gravityMultiplier;
        [ReadOnly, SerializeField] private float fallMultiplier;
        [ReadOnly, SerializeField] private float lowJumpMultiplier;
        
        private Transform _t;
        private CharacterController _controller;
        private readonly RaycastHit[] _hits = new RaycastHit[1];

        private bool _isDead;
        private Vector3 _moveDir;
        private bool _isGrounded;
        
        private float _verticalVelocity;
        private float _jumpCooldownLeft = -1f;
        private float _jumpDelayLeft = -1f;
        
        private bool _isJumpTriggered;
        private bool _isJumpInputReleased = true;
        
        private float _slopeSpeedMult = 1f;
        private float _currentSlopeAngle;

        [Inject] private CameraController _cameraController;
        
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
                return;
            }
            
            // init config
            if (cfg == null)
            {
                DebCon.Err($"Actor config is null on {gameObject.name}!", "AMover", gameObject);
                return;
            }
                
            speed = cfg.speed;
            
            jumpHeight = cfg.jumpHeight;
            jumpDelay = cfg.jumpDelay;
            jumpCooldown = cfg.jumpCooldown;
            velocityDecrement = cfg.velocityDecrement;
            
            gravityMultiplier = cfg.gravityMultiplier;
            fallMultiplier = cfg.fallMultiplier;
            lowJumpMultiplier = cfg.lowJumpMultiplier;
            
            // add component to pool
            var moverPool = world.GetPool<MoverComponent>();
            moverPool.Add(entityId);
            
            SyncEcsState();
        }
        
        public void Reset()
        {
            if (!enabled) return;

            _moveDir = Vector3.zero;
            _isGrounded = false;
            
            _verticalVelocity = 0f;
            
            _jumpCooldownLeft = -1;
            _jumpDelayLeft = -1;
            
            _isJumpTriggered = false;
            _isJumpInputReleased = true;
            
            _slopeSpeedMult = 1f;
            _currentSlopeAngle = 0f;
        }
        
        public void SyncEcsState()
        {
            if (EcsUtils.HasCompInPool<HealthComponent>(World, EntityId, out var healthPool))
            {
                ref var aHealth = ref healthPool.Get(EntityId);
                _isDead = aHealth.IsDead;

                if (_isDead) return;
            }
            
            if (EcsUtils.HasCompInPool<TransformComponent>(World, EntityId, out var transformPool))
            {
                ref var aTransform = ref transformPool.Get(EntityId);
                aTransform.Transform = _t;
                aTransform.Position = _t.position;
            }

            if (EcsUtils.HasCompInPool<MoverComponent>(World, EntityId, out var moverPool))
            {
                var move = _moveDir * (speed * _slopeSpeedMult);
                
                ref var aMovement = ref moverPool.Get(EntityId);
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
            
            _controller.enabled = !_isDead;
            if (_isDead) return;
            
            ref var aInput = ref inputPool.Get(EntityId);
            
            // jump related stuff
            HandleJump(aInput, dt);
            
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
            
            // todo: think of a better place to do this, get rid of jitter when moving towards clamped direction
            ClampToViewport(ref aInput);
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
            if (!enableSlopeSpeedCalculation) return 1f;
            if (!_isGrounded || moveDirection.magnitude < 0.1f) return 1f;
            
            var ray = new Ray(_t.position + _t.up * groundCheckOffset, Vector3.down);
            if (Physics.RaycastNonAlloc(ray, _hits, groundCheckDistance) == 0) return 1f;
            
            var slopeAngle = Vector3.Angle(_hits[0].normal, moveDirection) - 90f;
            
            // if the angle is greater than threshold, we're going uphill
            if (slopeAngle > uphillStartAngle)
            {
                var t = Mathf.InverseLerp(uphillStartAngle, uphillMaxAngle, slopeAngle);
                return Mathf.Lerp(1f, uphillSpeedMultiplier, t);
            }
            
            // if the angle is less than negative threshold, we're going downhill
            if (slopeAngle < -downhillStartAngle)
            {
                var t = Mathf.InverseLerp(-downhillStartAngle, -downhillMaxAngle, slopeAngle);
                return Mathf.Lerp(1f, downhillSpeedMultiplier, t);
            }
            
            // use normal speed on flat ground
            return 1f;
        }
        
        private void HandleJump(InputComponent input, float dt)
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
            if (Physics.RaycastNonAlloc(mainRay, _hits, groundCheckDistance, walkableLayerMask) > 0)
            {
                avgNormal = _hits[0].normal;
                hitCount++;
            }
            
            // additional raycasts in a circle around the character
            for (var i = 0; i < groundSampleCount; i++)
            {
                var angle = i * (2 * Mathf.PI / groundSampleCount);
                var offset = new Vector3(Mathf.Cos(angle) * groundCheckRadius, 0, Mathf.Sin(angle) * groundCheckRadius);
                var origin = _t.position + _t.up * groundCheckOffset + offset;
                
                var ray = new Ray(origin, Vector3.down);
                if (Physics.RaycastNonAlloc(ray, _hits, groundCheckDistance, walkableLayerMask) == 0) continue;
        
                avgNormal += _hits[0].normal;
                hitCount++;
        
                Debug.DrawRay(origin, _hits[0].normal, Color.green);
            }
    
            return (hitCount > 0, avgNormal, hitCount);
        }

        private void ClampToViewport(ref InputComponent input)
        {
            if (input.Movement.magnitude < 0.1f) return;
            if (screenClampBuffer <= 0f || _cameraController == null) return;
            
            var isVisible = _cameraController.CheckIfPointIsVisible(_t.position);
            if (!isVisible) return;
            
            _t.position = _cameraController.GetClampedViewportPosition(_t.position, screenClampBuffer);
        }
    }
}