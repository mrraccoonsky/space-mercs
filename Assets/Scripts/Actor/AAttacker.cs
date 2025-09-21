using UnityEngine;
using Data.Actor;
using Data.Projectile;
using DI.Services;
using ECS.Components;
using ECS.Utils;
using Tools;

namespace Actor
{
    using Leopotam.EcsLite;
    using Zenject;
    
    public enum OriginCycleMode
    {
        None,
        Reset,
        PingPong
    }

    public enum ScatterType
    {
        None,
        Random,
        Cone
    }
    
    public class AAttacker : MonoBehaviour, IActorModule
    {
        [Header("Attack:")]
        [SerializeField] private float attackCooldown = 0.6f;
        
        [Space]
        [SerializeField] private bool holdBurstTransform;
        [SerializeField] private int burstCount = 2;
        [SerializeField] private float burstCooldown = 0.2f;

        [Space]
        [SerializeField] private float scatterAngle;
        [SerializeField] private ScatterType scatterType = ScatterType.None;
        
        [Space]
        [SerializeField] private GameObject projectilePrefab;
        [SerializeField] private int projectileCount = 3;
        [SerializeField] private float projectileCooldown = 0.05f;
        [SerializeField] private float projectileLifetime = 0.5f;
        [SerializeField] private float projectileSpeed = 15f;
        [SerializeField] private int projectilePenetrationCount = 1;
        [SerializeField] private bool canHitOnCooldown;
        
        [Space]
        [SerializeField] private Transform originsRoot;
        [SerializeField] private OriginCycleMode cycleMode = OriginCycleMode.None;
        
        private Transform[] _origins;
        private int _currentOriginIndex;
        private int _currentCycleDirection = 1;
        
        private Vector3 _holdSpawnPos;
        private Quaternion _holdSpawnRot;
        
        private float _attackCooldownTimer;
        
        private int _burstCount;
        private float _burstCooldownTimer;
        private float _scatterAngle;
        
        private int _projectileCount;
        private float _projectileCooldownTimer;
        
        private bool _isAttacking;
        private bool _isAttackTriggered;
        
        public bool IsEnabled { get; private set; }
        public int EntityId { get; private set; }
        public EcsWorld World { get; private set; }
        
        [Inject] private IProjectileService _projectileService;
        
        public void Init(ActorConfig cfg, int entityId, EcsWorld world)
        {
            IsEnabled = enabled;
            if (!IsEnabled) return;
            
            EntityId = entityId;
            World = world;
            
            // todo: make it switchable
            _origins = new Transform[originsRoot.childCount];
            for (var i = 0; i < originsRoot.childCount; i++)
            {
                var child = originsRoot.GetChild(i);
                _origins[i] = child;
            }
            
            // init config
            if (cfg)
            {
                attackCooldown = cfg.attackCooldown;
                
                holdBurstTransform = cfg.holdBurstTransform;
                burstCount = cfg.burstCount;
                burstCooldown = cfg.burstCooldown;
                
                scatterAngle = cfg.scatterAngle;
                scatterType = cfg.scatterType;
                
                projectilePrefab = cfg.projectilePrefab;
                projectileCount = cfg.projectileCount;
                projectileCooldown = cfg.projectileCooldown;
                projectileLifetime = cfg.projectileLifetime;
                projectileSpeed = cfg.projectileSpeed;
                projectilePenetrationCount = cfg.projectilePenetrationCount;
                canHitOnCooldown = cfg.canHitOnCooldown;
            }
            
            // add component to pool
            var attackPool = world.GetPool<AttackComponent>();
            attackPool.Add(entityId);
            
            SyncEcsState();
        }
        
        public void SyncEcsState()
        {
            if (EcsUtils.HasCompInPool<AttackComponent>(World, EntityId, out var attackPool))
            {
                ref var aAttack = ref attackPool.Get(EntityId);
                aAttack.IsAttacking = _isAttacking;
            }
        }
        
        public void Tick(float dt)
        {
            if (World == null) return;

            if (!EcsUtils.HasCompInPool<InputComponent>(World, EntityId, out var inputPool))
            {
                DebCon.Err($"Input component not found on {gameObject.name}!", "AAttacker", gameObject);
                return;
            }
            
            // todo: think of a better way to kill switch logics 
            if (EcsUtils.HasCompInPool<HealthComponent>(World, EntityId, out var healthPool))
            {
                ref var aHealth = ref healthPool.Get(EntityId);
                if (aHealth.IsDead) return;
            }
            
            ref var aInput = ref inputPool.Get(EntityId);
            HandleAttackLogic(aInput, dt);
        }

        private void HandleAttackLogic(InputComponent input, float dt)
        {
            // main attack cooldown timer
            if (_attackCooldownTimer > 0f)
            {
                _attackCooldownTimer -= dt;
                
                if (_attackCooldownTimer <= 0f)
                {
                    _attackCooldownTimer = 0f;
                    _burstCount = 0;
                }
            }
            
            // single burst (with multiple projectiles) cooldown timer
            _burstCooldownTimer -= dt;

            if (_burstCooldownTimer <= 0f)
            {
                _burstCooldownTimer = 0f;
            }
            
            // update origins root positions and rotation (hold logics)
            if (_holdSpawnPos != Vector3.zero && _holdSpawnRot != Quaternion.identity)
            {
                originsRoot.transform.position = _holdSpawnPos;
                originsRoot.transform.rotation = _holdSpawnRot;
            }
            else
            {
                originsRoot.transform.localPosition = Vector3.zero;
                originsRoot.transform.localRotation = Quaternion.identity;
            }

            if (_isAttackTriggered)
            {
                // interval between shots in single burst timer
                _projectileCooldownTimer -= dt;

                if (_projectileCooldownTimer <= 0f)
                {
                    _projectileCooldownTimer = 0f;
                }

                // end of single attack (burst)
                if (_projectileCount >= projectileCount)
                {
                    _isAttackTriggered = false;
                    _burstCooldownTimer = burstCooldown;
                    _projectileCooldownTimer = 0f;
                    _burstCount++;
                    _scatterAngle = 0;
                    _projectileCount = 0;
                    
                    // reset hold spawn position and rotation
                    _holdSpawnPos = Vector3.zero;
                    _holdSpawnRot = Quaternion.identity;
                    
                    // set attack cooldown timer if it's not set
                    if (_attackCooldownTimer == 0f)
                    {
                        _attackCooldownTimer = attackCooldown;
                    }
                    // add calculated attack cooldown based on performed bursts
                    else
                    {
                        var shotsLeft = _burstCount;
                        _attackCooldownTimer = attackCooldown / shotsLeft;
                    }
                }
                
                // projectile spawn
                var shotCountPass = _burstCount < burstCount;
                var shotCooldownPass = _burstCooldownTimer <= 0f;
                var shotIntervalPass = _projectileCooldownTimer <= 0f;

                if (!shotCountPass || !shotCooldownPass || !shotIntervalPass) return;

                if (_projectileCount == 0 && holdBurstTransform)
                {
                    _holdSpawnPos = originsRoot.transform.position;
                    _holdSpawnRot = originsRoot.transform.rotation;
                } 
                
                _projectileCount++;
                _projectileCooldownTimer = projectileCooldown;
                
                // scattering
                var angleStep = scatterAngle / Mathf.Max(1, _origins.Length - 1);
                var initAngle = 0f;

                if (scatterType == ScatterType.Cone && _origins.Length > 1)
                    initAngle = -angleStep * 2f;
                
                else if (scatterType == ScatterType.Random)
                    initAngle = Random.Range(-angleStep * 2f, angleStep * 2f);
                
                _scatterAngle = initAngle;
                
                // cycle for multiple origins
                if (cycleMode == OriginCycleMode.None)
                {
                    foreach (var o in _origins)
                    {
                        SpawnProjectile(o);

                        if (scatterType == ScatterType.Cone)
                            _scatterAngle += angleStep;
                    }
                }
                else
                {
                    SpawnProjectile(_origins[_currentOriginIndex]);
                    switch (cycleMode)
                    {
                        case OriginCycleMode.Reset:
                            _currentOriginIndex++;

                            if (scatterType == ScatterType.Cone)
                                _scatterAngle += angleStep;

                            if (_currentOriginIndex >= _origins.Length)
                            {
                                _currentOriginIndex = 0;

                                if (scatterType == ScatterType.Cone)
                                    _scatterAngle = initAngle;
                            }
                            
                            break;
                        case OriginCycleMode.PingPong:
                            _currentOriginIndex += _currentCycleDirection;
                            
                            if (scatterType == ScatterType.Cone)
                                _scatterAngle += angleStep * _currentCycleDirection;

                            if (_currentOriginIndex >= _origins.Length)
                            {
                                _currentOriginIndex = _origins.Length - 1;
                                _currentCycleDirection *= -1;
                                
                                if (scatterType == ScatterType.Cone)
                                    _scatterAngle = initAngle * -1;
                            }
                            else if (_currentOriginIndex < 0)
                            {
                                _currentOriginIndex = 0;
                                _currentCycleDirection *= -1;
                                
                                if (scatterType == ScatterType.Cone)
                                    _scatterAngle = initAngle;
                            }
                            break;
                    }
                }
            }
            else
            {
                // attack input check
                var shotCountPass = _burstCount < burstCount;
                var shotCooldownPass = _burstCooldownTimer <= 0f;
                
                if (input.IsAttackHeld && shotCountPass && shotCooldownPass)
                {
                    _isAttackTriggered = true;
                }
            }
        }
        
        private void SpawnProjectile(Transform origin)
        {
            var data = new ProjectileData(
                prefab: projectilePrefab,
                tag: gameObject.tag,
                speed: projectileSpeed,
                lifetime: projectileLifetime,
                penetrationCount: projectilePenetrationCount,
                canHitOnCooldown: canHitOnCooldown);

            var rotation = Quaternion.Euler(origin.eulerAngles + Vector3.up * _scatterAngle);
            _projectileService.Spawn(data, origin.position, rotation);
        }
    }
}