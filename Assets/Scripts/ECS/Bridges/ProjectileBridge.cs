using UnityEngine;
using Data;
using Data.Explosion;
using Data.Weapon;
using DI.Services;

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
        [SerializeField] private GameObject hitParticlePrefab;

        [Space]
        [ReadOnly, SerializeField] private GlobalTag globalTag;
        [ReadOnly, SerializeField] private bool enableFriendlyFire;
        [ReadOnly, SerializeField] private bool canHitOnCooldown;
        [ReadOnly, SerializeField] private bool ignoreHitFx;

        [Space] 
        [ReadOnly, SerializeField] private float damage;
        [ReadOnly, SerializeField] private float speed;
        [ReadOnly, SerializeField] private float lifetime;
        [ReadOnly, SerializeField] private int penetrationCount;
        [ReadOnly, SerializeField] private float hitboxEnableDelay;
        
        [Space]
        [ReadOnly, SerializeField] private float pushForce;
        [ReadOnly, SerializeField] private float pushUpwardsMod;
        
        [Space]
        [ReadOnly, SerializeField] private bool enableRigidbody;
        [ReadOnly, SerializeField, ShowIf("enableRigidbody")] private bool rbHitObstacles;
        [ReadOnly, SerializeField, ShowIf("enableRigidbody")] private float rbTilt;
        [ReadOnly, SerializeField, ShowIf("enableRigidbody")] private float rbUpwardsMod;
        
        [Space]
        [ReadOnly, SerializeField] private ExplosionConfig explosionConfig;
        
        private IFXService _fxService;
        
        private Transform _t;
        private Collider _collider;
        private float _lifeTimer;
        private int _penetrationCount;
        
        private readonly Collider[] _hits = new Collider[1];
     
        public int EntityId { get; private set; }
        public EcsWorld World { get; private set; }
        public BoxCollider Hitbox { get; private set; }
        
        public ExplosionConfig ExplosionConfig => explosionConfig;

        private void OnDrawGizmosSelected()
        {
            if (hitboxParent == null) return;
            
            Gizmos.color = Color.orange;
            
            var hitboxPosition = hitboxParent.position;
            var hitboxRotationQuat = hitboxParent.rotation * Quaternion.Euler(hitboxRotation);
            
            Gizmos.matrix = Matrix4x4.TRS(hitboxPosition, hitboxRotationQuat, Vector3.one);
            Gizmos.DrawWireCube(hitboxOffset, hitboxSize);
        }
        
        // callers: ProjectileFactory -> ProjectileService
        [Inject]
        public void Construct(IFXService fxService)
        {
            _fxService = fxService;
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

        public void Reset()
        {
            _lifeTimer = 0f;
            _penetrationCount = 0;
            gameObject.SetActive(true);

            if (rb != null)
            {
                if (enableRigidbody)
                {
                    var tilt = Quaternion.Euler(rbTilt, 0f, 0f);
                    var rot = _t.rotation * tilt;
                    _t.rotation = rot;
                    rb.rotation = rot;
                
                    rb.isKinematic = false;
                    rb.linearVelocity = Vector3.zero;
                    rb.angularVelocity = Vector3.zero;
                
                    rb.AddForce(_t.forward * speed, ForceMode.Impulse);
                    rb.AddTorque(_t.right * rbUpwardsMod * -0.01f, ForceMode.Force);
                }
                else
                {
                    rb.isKinematic = true;
                }
            }

            SetTrailEnabled(true);
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
        
        public void SetTag(GlobalTag globalTag)
        {
            this.globalTag = globalTag;
        }

        public void SetData(WeaponConfig cfg)
        {
            enableFriendlyFire = cfg.enableFriendlyFire;
            canHitOnCooldown = cfg.canHitOnCooldown;
            ignoreHitFx = cfg.ignoreHitFx;
            
            damage = cfg.damage;
            speed = cfg.speed;
            lifetime = cfg.lifetime;
            penetrationCount = cfg.penetrationCount;
            hitboxEnableDelay = cfg.hitboxEnableDelay;
            
            pushForce = cfg.pushForce;
            pushUpwardsMod = cfg.pushUpwardsMod;

            enableRigidbody = cfg.enableRigidbody;
            rbHitObstacles = cfg.rbHitObstacles;
            rbTilt = cfg.rbTilt;
            rbUpwardsMod = cfg.rbUpwardsMod;

            explosionConfig = cfg.explosionConfig;
            
            if (particle != null)
            {
                var main = particle.main;
                main.startLifetime = lifetime;
            }
        }

        public bool CheckNeedsDestroy()
        {
            return _lifeTimer >= lifetime || _penetrationCount > penetrationCount;
        }

        public void Tick(float dt)
        {
            if (hitboxEnableDelay > 0f)
            {
                if (Hitbox != null)
                {
                    Hitbox.enabled = _lifeTimer >= hitboxEnableDelay;
                }

                if (_collider != null)
                {
                    _collider.enabled = _lifeTimer >= hitboxEnableDelay;
                }
            }
            
            // transform update if no rb
            if (!enableRigidbody || rb == null)
            {
                _t.Translate(_t.forward * speed * dt, Space.World);
            }
            
            _lifeTimer += dt;
        }

        public void RegisterHit()
        {
            _penetrationCount++;
            
            if (hitParticlePrefab != null)
            {
                _fxService.Spawn(hitParticlePrefab, _t.position, _t.rotation);
            }
        }
        
        public bool CheckCollisionWithObstacles()
        {
            if (rb != null)
            {
                var radius = Mathf.Max(Hitbox.size.x, Hitbox.size.y, Hitbox.size.z);
                var center = Hitbox.bounds.center;
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
            if (Hitbox != null) return;
            if (hitboxParent == null) return;
            
            var go = new GameObject("Hitbox")
            {
                layer = LayerMask.NameToLayer("Hitbox")
            };
            
            go.transform.SetParent(hitboxParent);
            go.transform.localRotation = Quaternion.Euler(hitboxRotation);
            go.transform.localPosition = Vector3.zero;
            
            Hitbox = go.AddComponent<BoxCollider>();
            
            Hitbox.isTrigger = true;
            Hitbox.size = hitboxSize;
            Hitbox.center = hitboxOffset;
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
    }
}