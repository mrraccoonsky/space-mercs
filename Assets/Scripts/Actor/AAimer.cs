using System;
using UnityEngine;
using UnityEngine.Animations;
using Data.Actor;
using DI.Services;
using ECS.Components;
using ECS.Utils;
using Tools;

namespace Actor
{
    using Leopotam.EcsLite;
    using Zenject;
    
    [Serializable]
    public class ConstraintData
    {
        public LookAtConstraint constraint;
        public Vector3 aimRotationOffset;
        public bool defaultState = true;
        public float disableThreshold = -1f;
        public float minDistance;
        public float speed;
        public float minWeight;
        public float maxWeight;
    }
    
    public class AAimer : MonoBehaviour, IActorModule
    {
        [Header("Target Origin:")]
        [SerializeField] private GameObject targetOriginPrefab;
        [SerializeField] private float defaultTargetDistance = 1f;
        [SerializeField] private float targetMoveSpeed = 50f; 
        
        [Header("Rotation:")]
        [SerializeField] private float rotationSpeed = 10f;
        [SerializeField] private bool aimTowardsAttackDirection;

        [SerializeField] private ConstraintData[] constraintData;
        
        private Transform _t;
        private Transform _targetOrigin;

        private Vector3 _lastOriginPos;
        private bool _isAiming;
        
        private IPoolService _poolService;
        
        public bool IsEnabled { get; private set; }
        public int EntityId { get; private set; }
        public EcsWorld World { get; private set; }

        [Inject]
        public void Construct(IPoolService poolService)
        {
            _poolService = poolService;
        }
        
        private void OnDisable()
        {
            if (_targetOrigin != null)
            {
                _poolService?.Return(targetOriginPrefab, _targetOrigin.gameObject);
            }
        }
        
        public void Init(ActorConfig cfg, int entityId, EcsWorld world)
        {
            IsEnabled = enabled;
            if (!IsEnabled) return;
            
            EntityId = entityId;
            World = world;
            
            _t = transform;
            
            GetTargetOrigin();
            InitAimConstraints();

            // init config
            if (cfg)
            {
                defaultTargetDistance = cfg.defaultTargetDistance;
                targetMoveSpeed = cfg.targetMoveSpeed;
                
                rotationSpeed = cfg.rotationSpeed;
                aimTowardsAttackDirection = cfg.aimTowardsAttackDirection;
            }
            
            // add component to pool
            var aimPool = World.GetPool<AimComponent>();
            aimPool.Add(EntityId);
            
            SyncEcsState();
        }

        public void Reset()
        {
            _lastOriginPos = _targetOrigin.position;
            _isAiming = false;
        }
        
        public void SyncEcsState()
        {
            // override transform component values
            if (EcsUtils.HasCompInPool<TransformComponent>(World, EntityId, out var transformPool))
            {
                ref var aTransform = ref transformPool.Get(EntityId);
                aTransform.Rotation = _t.rotation;
            }

            if (EcsUtils.HasCompInPool<AimComponent>(World, EntityId, out var aimPool))
            {
                ref var aAim = ref aimPool.Get(EntityId);
                aAim.TargetOrigin = _targetOrigin;
                aAim.AimPosition = _targetOrigin.position;
                aAim.AimDirection = (_targetOrigin.position - _t.position).normalized;
                aAim.IsAiming = _isAiming;
            }
        }

        public void Tick(float dt)
        {
            if (World == null) return;

            if (!EcsUtils.HasCompInPool<InputComponent>(World, EntityId, out var inputPool))
            {
                DebCon.Err($"Input component not found on {gameObject.name}!", "AAimer", gameObject);
                return;
            }

            // todo: think of a better way to kill switch logics 
            if (EcsUtils.HasCompInPool<HealthComponent>(World, EntityId, out var healthPool))
            {
                ref var aHealth = ref healthPool.Get(EntityId);
                if (aHealth.IsDead)
                {
                    foreach (var c in constraintData)
                    {
                        c.constraint.constraintActive = false;
                        c.constraint.weight = 0f;
                    }
                    
                    return;
                }
            }
            
            ref var aInput = ref inputPool.Get(EntityId);
            
            // aim towards attack direction
            var isAttacking = false;
            if (EcsUtils.HasCompInPool<AttackComponent>(World, EntityId, out var attackPool))
            {
                ref var aAttack = ref attackPool.Get(EntityId);
                isAttacking = aimTowardsAttackDirection && (aAttack.IsAttacking || aInput.IsAttackHeld);
            }
            
            _isAiming = UpdateTargetOrigin(aInput, isAttacking, dt);
            
            HandleRotation(dt);
            UpdateAimConstraints(dt);
        }

        private void InitAimConstraints()
        {
            if (constraintData == null || constraintData.Length == 0) return;
            
            var source = new ConstraintSource
            {
                sourceTransform = _targetOrigin,
                weight = 1f
            };

            foreach (var c in constraintData)
            {
                c.constraint.AddSource(source);
                c.constraint.rotationOffset = c.aimRotationOffset;
                c.constraint.weight = c.minWeight;
                c.constraint.constraintActive = c.defaultState;
            }
        }
        
        private void UpdateAimConstraints(float dt)
        {
            var distance = (_targetOrigin.position - _t.position).magnitude;
            foreach (var c in constraintData)
            {
                var curWeight = c.constraint.weight;
                float targetWeight;
                
                if (_isAiming)
                {
                    if (distance > c.minDistance)
                    {
                        targetWeight = Mathf.Lerp(curWeight, c.maxWeight, dt * c.speed);
                    }

                    else
                    {
                        var distFactor = distance / c.minDistance;
                        targetWeight = Mathf.Lerp(curWeight, distFactor, dt * c.speed);
                    }

                    // switch only if default state is true
                    if (c.defaultState)
                    {
                        c.constraint.constraintActive = true;
                    }
                }

                else
                {
                    targetWeight = Mathf.Lerp(curWeight, c.minWeight, dt * c.speed);
                    if (targetWeight < c.disableThreshold)
                    {
                        c.constraint.constraintActive = false;
                    }
                }
                
                c.constraint.weight = Mathf.Clamp(targetWeight, c.minWeight, c.maxWeight);
            }
        }
        
        private void GetTargetOrigin()
        {
            _targetOrigin = _poolService.Get<Transform>(targetOriginPrefab);
            _targetOrigin.position = _t.position + _t.forward * defaultTargetDistance;
            _lastOriginPos = _targetOrigin.position;
            
            // _targetOrigin.gameObject.SetActive(true);
        }

        private bool UpdateTargetOrigin(InputComponent aInput, bool isAttacking, float dt)
        {
            var mainCamera = aInput.MainCamera;
            var currentTargetPos = _targetOrigin.position;
            var targetWorldPos = _lastOriginPos;
            var move = aInput.Movement;
            
            var canAim = aInput.AimPosition != Vector3.zero;
            var isAiming = false;
            
            if (canAim && (isAttacking || aInput.IsAimHeld))
            {
                targetWorldPos = aInput.AimPosition;
                
                // adjust the target origin for camera-related aiming (aim cursor offset)
                if (mainCamera != null && mainCamera.orthographic)
                {
                    // todo: make this configurable and based on certain camera settings, projectile origin height, etc
                    targetWorldPos -= mainCamera.transform.forward * 1.2f;
                }
                
                // clamp the target position to ensure it doesn't go through the character
                var targetDir = targetWorldPos - _t.position;
                var targetDist = targetDir.magnitude;
                
                if (targetDist < defaultTargetDistance)
                {
                    targetWorldPos = _t.position + targetDir.normalized * defaultTargetDistance;
                }
                
                _lastOriginPos = targetWorldPos;
                isAiming = true;
            }
                
            else if (mainCamera != null && move.magnitude > 0.1f)
            {
                var camRotY = mainCamera.transform.rotation.eulerAngles.y;
                var targetRotation = Mathf.Atan2(move.x, move.y) * Mathf.Rad2Deg + camRotY;
                var moveDir = Quaternion.Euler(0f, targetRotation, 0f) * Vector3.forward;
                
                targetWorldPos = _t.position + moveDir.normalized * defaultTargetDistance;
                _lastOriginPos = targetWorldPos;
            }

            else if (move.magnitude > 0.1f)
            {
                targetWorldPos = _t.position + new Vector3(move.x, 0f, move.y).normalized * defaultTargetDistance;
                _lastOriginPos = targetWorldPos;
            }
            
            // adjust to character height
            // targetWorldPos.y = _t.position.y;
        
            _targetOrigin.position = Vector3.Lerp(
                currentTargetPos,
                targetWorldPos,
                targetMoveSpeed * dt
            );
            
            return isAiming;
        }
        
        private void HandleRotation(float dt)
        {
            var lookDir = _targetOrigin.position - _t.position;
            var targetRotation = Quaternion.LookRotation(new Vector3(lookDir.x, 0, lookDir.z));
    
            _t.rotation = Quaternion.Slerp(_t.rotation, targetRotation, rotationSpeed * dt);
        }
    }
}