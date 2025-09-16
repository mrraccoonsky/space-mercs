using UnityEngine;
using Factories;

namespace ECS.Bridges
{
    using Leopotam.EcsLite;
    using NaughtyAttributes;
    
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
        
        private BoxCollider _hitbox;
        private ParticleSystem _hitFx;
        
        private Transform _t;
        private Vector3 _initialVelocity;
        private float _lifeTimer;
        private int _penetrationCount;
     
        public GameObject Prefab { get; private set; }
        public int EntityId { get; private set; }
        public EcsWorld World { get; private set; }

        public BoxCollider Hitbox => _hitbox;

        private void OnDrawGizmosSelected()
        {
            if (hitboxParent == null) return;
            
            Gizmos.color = Color.orange;
            
            var hitboxPosition = hitboxParent.position;
            var hitboxRotationQuat = hitboxParent.rotation * Quaternion.Euler(hitboxRotation);
            
            Gizmos.matrix = Matrix4x4.TRS(hitboxPosition, hitboxRotationQuat, Vector3.one);
            Gizmos.DrawWireCube(hitboxOffset, hitboxSize);
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

            if (_hitbox == null)
            {
                CreateHitbox();
            }
        }

        public void SetData(ProjectileData data)
        {
            // factory pool reference
            Prefab = data.prefab;
            
            speed = data.speed;
            lifetime = data.lifetime;
            penetrationCount = data.penetrationCount;
            
            _initialVelocity = transform.forward * speed;

            // todo: debug this and set proper values
            /* var forwardComponent = Vector3.Dot(data.initialVelocity, _t.forward);
            if (forwardComponent > 0)
            {
                _initialVelocity += _t.forward * forwardComponent;
            }
            else if (forwardComponent < 0)
            {
                var reductionFactor = Mathf.Abs(forwardComponent);
                var minSpeed = speed * 0.5f;
                var newSpeed = Mathf.Max(speed - reductionFactor, minSpeed);
                
                _initialVelocity = transform.forward * newSpeed;
            } */

            if (particle != null)
            {
                var main = particle.main;
                main.startLifetime = lifetime;
            }
        }

        public bool ShouldBeDestroyed()
        {
            return _lifeTimer >= lifetime || _penetrationCount > penetrationCount;
        }

        public void Tick(float dt)
        {
            _t.Translate(_initialVelocity * dt, Space.World);
            _lifeTimer += dt;
        }

        public void RegisterHit()
        {
            _penetrationCount++;
            
            if (hitParticlePrefab == null) return;
            
            var data = new FXData(hitParticlePrefab, _t.position, _t.rotation);
            FXFactory.Create(data);
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
    }
}