using UnityEngine;
using Data.Projectile;
using DI.Services;

namespace ECS.Bridges
{
    using NaughtyAttributes;
    using Leopotam.EcsLite;
    using Zenject;
    
    public class ProjectileBridge : MonoBehaviour, IEcsBridge
    {
        [Header("Hitbox:")]
        [SerializeField] private Transform hitboxParent;
        [SerializeField] private Vector3 hitboxOffset;
        [SerializeField] private Vector3 hitboxRotation;
        [SerializeField] private Vector3 hitboxSize = Vector3.one;

        [Header("Visuals:")]
        [SerializeField] private ParticleSystem particle;
        [SerializeField] private GameObject hitParticlePrefab;
        
        [Space]
        [ReadOnly, SerializeField] private float speed = 1f;
        [ReadOnly, SerializeField] private float lifetime = 1f;
        [ReadOnly, SerializeField] private int penetrationCount = 1;
        
        private IFXService _fxService;
        private ParticleSystem _hitFx;
        
        private Transform _t;
        private float _lifeTimer;
        private int _penetrationCount;
     
        public int EntityId { get; private set; }
        public EcsWorld World { get; private set; }
        public GameObject Prefab { get; private set; }
        public BoxCollider Hitbox { get; private set; }
        
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

        public void Reset()
        {
            _lifeTimer = 0f;
            _penetrationCount = 0;
        }
        
        public void Init(int entityId, EcsWorld world)
        {
            EntityId = entityId;
            World = world;
            
            _t = transform;

            if (Hitbox == null)
            {
                CreateHitbox();
            }
        }

        public void SetData(ProjectileData data)
        {
            // pool reference
            Prefab = data.prefab;
            
            speed = data.speed;
            lifetime = data.lifetime;
            penetrationCount = data.penetrationCount;

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
            _t.Translate(_t.forward * speed * dt, Space.World);
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
            var ray = new Ray(_t.position - _t.forward, _t.forward);
            return Physics.Raycast(ray, 1f, LayerMask.GetMask("Obstacle"));
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
    }
}