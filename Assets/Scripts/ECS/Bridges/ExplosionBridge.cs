using UnityEngine;
using Data;
using Data.Explosion;

namespace ECS.Bridges
{
    using NaughtyAttributes;
    using Leopotam.EcsLite;
    
    [SelectionBase]
    public class ExplosionBridge : MonoBehaviour, IEcsBridge
    {
        [SerializeField, ReadOnly] private GlobalTag globalTag;
        
        [Header("Hit Area")]
        [SerializeField] private Transform hitAreaParent;
        [SerializeField] private Vector3 hitAreaOffset;
        [SerializeField] private float hitAreaRadius = 1f;

        [Space]
        [ReadOnly, SerializeField] private bool enableFriendlyFire;
        [ReadOnly, SerializeField] private bool canHitOnCooldown;
        [ReadOnly, SerializeField] private bool ignoreHitFx;
        
        [Space]
        [ReadOnly, SerializeField] private float damage;
        [ReadOnly, SerializeField] private float radius;
        [ReadOnly, SerializeField] private float lifetime;
        [ReadOnly, SerializeField] private float hitAreaLifetime;
        [ReadOnly, SerializeField] private float distanceMult = 1f;
        
        [Space]
        [ReadOnly, SerializeField] private float pushForce;
        [ReadOnly, SerializeField] private float pushUpwardsMod;

        private float _lifeTimer;
        private float _hitAreaLifeTimer;
        
        public int EntityId { get; private set; }
        public EcsWorld World { get; private set; }
        public SphereCollider HitArea { get; private set; }

        private void OnDrawGizmosSelected()
        {
            if (hitAreaParent == null) return;

            Gizmos.color = Color.orange;
            
            var hitAreaPosition = hitAreaParent.position;
            Gizmos.DrawWireSphere(hitAreaPosition + hitAreaOffset, hitAreaRadius);
        }

        public void Reset()
        {
            _lifeTimer = 0f;
            _hitAreaLifeTimer = 0f;
            gameObject.SetActive(true);
        }

        public void Init(int entityId, EcsWorld world)
        {
            EntityId = entityId;
            World = world;

            CreateHitArea();
        }
        
        public void SetTag(GlobalTag globalTag)
        {
            this.globalTag = globalTag;
        }

        public void SetData(ExplosionConfig cfg)
        {
            enableFriendlyFire = cfg.enableFriendlyFire;
            canHitOnCooldown = cfg.canHitOnCooldown;
            ignoreHitFx = cfg.ignoreHitFx;

            if (!cfg.cloneDamage)
            {
                damage = cfg.damage;
            }
            
            radius = cfg.radius;
            lifetime = cfg.lifetime;
            hitAreaLifetime = cfg.hitAreaLifetime;
            distanceMult = cfg.distanceMult;

            if (!cfg.clonePush)
            {
                pushForce = cfg.pushForce;
                pushUpwardsMod = cfg.pushUpwardsMod;
            }

            if (HitArea != null)
            {
                HitArea.radius = radius;
                HitArea.center = hitAreaOffset;
            }
        }

        public void SetDamageValue(float val)
        {
            damage = val;
        }

        public void SetPushValues(float force, float upwardsMod)
        {
            pushForce = force;
            pushUpwardsMod = upwardsMod;
        }

        public bool CheckNeedsDestroy()
        {
            return _lifeTimer >= lifetime;
        }

        public void Tick(float dt)
        {
            if (HitArea != null)
            {
                HitArea.enabled = _hitAreaLifeTimer < hitAreaLifetime;
            }

            _lifeTimer += dt;
            _hitAreaLifeTimer += dt;
        }

        private void CreateHitArea()
        {
            if (HitArea != null) return;
            if (hitAreaParent == null) return;

            var go = new GameObject("Hit Area")
            {
                layer = LayerMask.NameToLayer("Hitbox")
            };
            
            go.transform.SetParent(hitAreaParent);
            go.transform.localRotation = Quaternion.identity;
            go.transform.localPosition = Vector3.zero;
            
            HitArea = go.AddComponent<SphereCollider>();
            
            HitArea.isTrigger = true;
            HitArea.radius = hitAreaRadius;
            HitArea.center = hitAreaOffset;
        }
    }
}