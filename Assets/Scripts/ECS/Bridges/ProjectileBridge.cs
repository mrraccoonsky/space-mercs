using UnityEngine;
using Data;
using Data.Explosion;
using Data.Weapon;
using DI.Services;
using ECS.Components;
using ECS.Utils;

namespace ECS.Bridges
{
    using NaughtyAttributes;
    using Leopotam.EcsLite;
    using Zenject;
    
    [SelectionBase]
    public class ProjectileBridge : MonoBehaviour, IEcsBridge
    {
        [SerializeField] private Rigidbody rb;
        [SerializeField] private TrailRenderer trail;
        [SerializeField] private LayerMask obstacleLayerMask;
        
        [Header("Hitbox")]
        [SerializeField] private Transform hitboxParent;
        [SerializeField] private Vector3 hitboxOffset;
        [SerializeField] private Vector3 hitboxRotation;
        [SerializeField] private Vector3 hitboxSize = Vector3.one;

        [Header("Visuals")]
        [SerializeField] private ParticleSystem particle;
        [SerializeField] private GameObject hitFxPrefab;
        [SerializeField] private bool scaleAffectsHitFx;

        [Space]
        [ReadOnly, SerializeField] private GlobalTag globalTag;
        [ReadOnly, SerializeField] private bool enableFriendlyFire;
        [ReadOnly, SerializeField] private bool canHitOnCooldown;
        [ReadOnly, SerializeField] private bool ignoreHitFx;

        [Space]
        [ReadOnly, SerializeField] private float scale;
        [ReadOnly, SerializeField] private float tilt;
        [ReadOnly, SerializeField] private float damage;
        [ReadOnly, SerializeField] private float speed;
        [ReadOnly, SerializeField] private float lifetime;
        [ReadOnly, SerializeField] private float knockbackForce;
        [ReadOnly, SerializeField] private float knockbackDuration;
        [ReadOnly, SerializeField] private int penetrationCount;
        [ReadOnly, SerializeField] private float hitboxEnableDelay;
        
        [Space]
        [ReadOnly, SerializeField] private bool enableAim;
        [ReadOnly, SerializeField, ShowIf("enableAim")] private bool canReuseTarget;
        [ReadOnly, SerializeField, ShowIf("enableAim")] private float aimDot;
        [ReadOnly, SerializeField, ShowIf("enableAim")] private float aimRange;
        [ReadOnly, SerializeField, ShowIf("enableAim")] private float aimSpeed;
        [ReadOnly, SerializeField, ShowIf("enableAim")] private float aimDelay;
        [ReadOnly, SerializeField, ShowIf("enableAim")] private float aimMoveSpeedMult;
        
        [Space]
        [ReadOnly, SerializeField] private bool enableRigidbody;
        [ReadOnly, SerializeField, ShowIf("enableRigidbody")] private bool rbHitObstacles;
        [ReadOnly, SerializeField, ShowIf("enableRigidbody")] private float rbTorque;
        
        [Space]
        [ReadOnly, SerializeField] private float pushForce;
        [ReadOnly, SerializeField] private float pushUpwardsMod;
        
        [Space]
        [ReadOnly, SerializeField] private ExplosionConfig explosionConfig;
        
        private IFxService _fxService;
        
        private Transform _t;
        private BoxCollider _hitbox;
        private Collider _collider; // only for rb-based projectiles

        private float _lifeTimer;
        private int _penetrationCount;
        private Vector3 _aimTarget;
        
        private readonly Collider[] _hits = new Collider[1]; // only for rb-based projectiles
     
        public int EntityId { get; private set; }
        public EcsWorld World { get; private set; }

        public Vector3 Position => _t.position;
        public ExplosionConfig ExplosionConfig => explosionConfig;

        private void OnDrawGizmosSelected()
        {
            if (Application.isPlaying) return;
            if (hitboxParent == null) return;
            
            Gizmos.color = Color.orange;
            
            var hitboxPosition = hitboxParent.position;
            var hitboxRotationQuat = hitboxParent.rotation * Quaternion.Euler(hitboxRotation);
            
            Gizmos.matrix = Matrix4x4.TRS(hitboxPosition, hitboxRotationQuat, Vector3.one);
            Gizmos.DrawWireCube(hitboxOffset, hitboxSize);
        }
        
        // callers: ProjectileFactory -> ProjectileService
        [Inject]
        public void Construct(IFxService fxService)
        {
            _fxService = fxService;
        }
        
        private void OnDrawGizmos()
        {
            if (!_t) return;
            
            // last known target position / line of sight visualization
            if (_aimTarget != Vector3.zero)
            {
                Gizmos.color = Color.blue;
                Gizmos.DrawWireCube(_aimTarget, Vector3.one);
                Gizmos.DrawLine(_t.position, _aimTarget);
            }
        }

        private void OnDisable()
        {
            if (rb != null && !rb.isKinematic)
            {
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
                rb.isKinematic = true;
            }

            SetTrailEnabled(false, true);
        }
        
        public void Init(int entityId, EcsWorld world)
        {
            EntityId = entityId;
            World = world;
            
            _t = transform;

            if (_collider == null)
            {
                _collider = GetComponentInChildren<Collider>();
            }

            CreateHitbox();
        }
        
        public void Reset()
        {
            _t.localScale = Vector3.one * scale;
            
            _lifeTimer = 0f;
            _penetrationCount = 0;
            _aimTarget = Vector3.zero;

            SyncEcsState();
            gameObject.SetActive(true);
            
            var tiltRot = Quaternion.Euler(tilt, 0f, 0f);
            var rot = _t.rotation * tiltRot;
            _t.rotation = rot;
            
            if (rb != null)
            {
                if (enableRigidbody)
                {
                    rb.rotation = rot;
                
                    rb.isKinematic = false;
                    rb.linearVelocity = Vector3.zero;
                    rb.angularVelocity = Vector3.zero;
                
                    rb.AddForce(_t.forward * speed, ForceMode.Impulse);
                    rb.AddRelativeTorque(Vector3.right * rbTorque, ForceMode.Force);
                }
                else
                {
                    rb.isKinematic = true;
                }
            }
            
            if (_collider != null)
            {
                _collider.enabled = hitboxEnableDelay == 0f;
            }
            
            if (particle != null)
            {
                var main = particle.main;
                main.startLifetime = lifetime;
            }

            SetTrailEnabled(true);
        }
        
        public void SetTag(GlobalTag globalTag)
        {
            this.globalTag = globalTag;
        }

        public void SetData(WeaponConfig cfg)
        {
            enableFriendlyFire = cfg.enableFriendlyFire;
            canHitOnCooldown = cfg.canHitOnCooldown;
            ignoreHitFx = cfg.ignoreHitFx;
                
            scale = cfg.scale;
            damage = cfg.damage;
            speed = cfg.speed;
            lifetime = cfg.lifetime;
            knockbackForce = cfg.knockbackForce;
            knockbackDuration = cfg.knockbackDuration;
            penetrationCount = cfg.penetrationCount;
            hitboxEnableDelay = cfg.hitboxEnableDelay;

            enableAim = cfg.enableAim;
            canReuseTarget = cfg.canReuseTarget;
            aimDot = cfg.aimDot;
            aimRange = cfg.aimRange;
            aimSpeed = cfg.aimSpeed;
            aimDelay = cfg.aimDelay;
            aimMoveSpeedMult = cfg.aimMoveSpeedMult;
            
            enableRigidbody = cfg.enableRigidbody;
            rbHitObstacles = cfg.rbHitObstacles;
            tilt = cfg.tilt;
            rbTorque = cfg.rbTorque;
            
            pushForce = cfg.pushForce;
            pushUpwardsMod = cfg.pushUpwardsMod;
            
            explosionConfig = cfg.explosionConfig;
        }

        public bool CheckNeedsDestroy()
        {
            return _lifeTimer >= lifetime || _penetrationCount > penetrationCount;
        }

        public void Tick(float dt)
        {
            if (hitboxEnableDelay > 0f)
            {
                if (_hitbox != null)
                {
                    _hitbox.enabled = _lifeTimer >= hitboxEnableDelay;
                }
        
                if (_collider != null)
                {
                    _collider.enabled = _lifeTimer >= hitboxEnableDelay;
                }
            }
    
            var currentSpeed = speed;
            
            // aim-related code (non-rb only)
            if (enableAim && _aimTarget != Vector3.zero)
            {
                var isAimActive = _lifeTimer > aimDelay;
                if (isAimActive)
                {
                    currentSpeed *= aimMoveSpeedMult;
            
                    if (!enableRigidbody || rb == null)
                    {
                        var targetDir = (_aimTarget - _t.position).normalized;
                        var step = aimSpeed * dt;
                        
                        var currentRot = _t.rotation;
                        var targetRot = Quaternion.LookRotation(targetDir);
                        _t.rotation = Quaternion.Slerp(currentRot, targetRot, step);
                    }

                    if (trail != null && !trail.emitting)
                    {
                        trail.emitting = true;
                    }
                }
            }

            if (!enableRigidbody || rb == null)
            {
                _t.Translate(_t.forward * currentSpeed * dt, Space.World);
            }
    
            _lifeTimer += dt;
            SyncEcsState();
        }

        public void FixedTick(float dt)
        {
            if (!enableAim || _aimTarget == Vector3.zero) return;
            if (!enableRigidbody || rb == null) return;
            if (_lifeTimer <= aimDelay) return;
            
            var targetDir = (_aimTarget - _t.position).normalized;
            var step = aimSpeed * dt;
            
            var currentRot = rb.rotation;
            var targetRot = Quaternion.LookRotation(targetDir);
            rb.rotation = Quaternion.Slerp(currentRot, targetRot, step);

            rb.linearVelocity = _t.forward * (speed * aimMoveSpeedMult);
            rb.angularVelocity = Vector3.zero;
        }

        private void SyncEcsState()
        {
            if (EcsUtils.HasCompInPool<ProjectileComponent>(World, EntityId, out var projectilePool))
            {
                ref var aProjectile = ref projectilePool.Get(EntityId);
                
                aProjectile.Bridge = this;
                aProjectile.Tag = globalTag;
                aProjectile.HitBox = _hitbox;
                
                aProjectile.CanHitOnCooldown = canHitOnCooldown;
                aProjectile.IgnoreHitFx = ignoreHitFx;

                aProjectile.Scale = scale;
                aProjectile.Damage = damage;
                aProjectile.KnockbackForce = knockbackForce;
                aProjectile.KnockbackDuration = knockbackDuration;
                
                aProjectile.EnableAim = enableAim;
                aProjectile.CanReuseTarget = canReuseTarget;
                aProjectile.AimDot = aimDot;
                aProjectile.AimRange = aimRange;
                
                aProjectile.PushForce = pushForce;
                aProjectile.PushUpwardsMod = pushUpwardsMod;

                // in this case, the AimTarget are being set in ProjectileSystem
                _aimTarget = aProjectile.AimTarget;
            }
        }

        public void RegisterHit()
        {
            _penetrationCount++;
            
            if (hitFxPrefab != null)
            {
                var fxScale = scaleAffectsHitFx ? _t.localScale : Vector3.one;
                _fxService.Spawn(hitFxPrefab, _t.position, _t.rotation, fxScale);
            }
        }
        
        public bool CheckCollisionWithObstacles()
        {
            if (rb != null && enableRigidbody)
            {
                var radius = Mathf.Max(_hitbox.size.x, _hitbox.size.y, _hitbox.size.z) * scale;
                var center = _hitbox.bounds.center;
                var hits = Physics.OverlapSphereNonAlloc(center, radius, _hits, obstacleLayerMask);

                // disable trail on hit
                if (hits > 0)
                {
                    SetTrailEnabled(false);
                }
                
                return rbHitObstacles && hits > 0;
            }
            
            var ray = new Ray(_t.position - _t.forward, _t.forward);
            return Physics.Raycast(ray, 1f, obstacleLayerMask);
        }
        
        private void CreateHitbox()
        {
            if (_hitbox != null) return;
            if (hitboxParent == null) return;
            
            var go = new GameObject("Hitbox")
            {
                layer = LayerMask.NameToLayer("Hitbox")
            };
            
            go.transform.SetParent(hitboxParent);
            go.transform.localRotation = Quaternion.Euler(hitboxRotation);
            go.transform.localPosition = Vector3.zero;
            
            _hitbox = go.AddComponent<BoxCollider>();
            
            _hitbox.isTrigger = true;
            _hitbox.size = hitboxSize;
            _hitbox.center = hitboxOffset;
        }

        private void SetTrailEnabled(bool state, bool clear = false)
        {
            if (trail == null) return;
            trail.emitting = state;

            if (clear)
            {
                trail.Clear();
            }
        }

        public void SetHomingTarget(Vector3 target)
        {
            _aimTarget = target;
        }
    }
}