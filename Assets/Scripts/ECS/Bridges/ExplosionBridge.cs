using UnityEngine;
using Data;
using Data.Explosion;
using ECS.Components;
using ECS.Utils;
using Tools;

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
        [ReadOnly, SerializeField] private bool canHitOnCooldown;
        [ReadOnly, SerializeField] private bool ignoreHitFx;

        [Space]
        [ReadOnly, SerializeField] private float scale;
        [ReadOnly, SerializeField] private float damage;
        [ReadOnly, SerializeField] private float radius;
        [ReadOnly, SerializeField] private float lifetime;
        [ReadOnly, SerializeField] private float knockbackForce;
        [ReadOnly, SerializeField] private float knockbackDuration;
        [ReadOnly, SerializeField] private float hitAreaLifetime;
        [ReadOnly, SerializeField] private float distanceMult = 1f;
        
        [Space]
        [ReadOnly, SerializeField] private float pushForce;
        [ReadOnly, SerializeField] private float pushUpwardsMod;

        private Transform _t;
        private SphereCollider _hitArea;
        private float _lifeTimer;
        private float _hitAreaLifeTimer;
        
        public int EntityId { get; private set; }
        public EcsWorld World { get; private set; }

        private void OnDrawGizmosSelected()
        {
            if (Application.isPlaying) return;
            if (hitAreaParent == null) return;

            Gizmos.color = Color.orange;
            
            var hitAreaPosition = hitAreaParent.position;
            Gizmos.DrawWireSphere(hitAreaPosition + hitAreaOffset, hitAreaRadius);
        }

        public void Init(int entityId, EcsWorld world)
        {
            EntityId = entityId;
            World = world;

            _t = transform;

            CreateHitArea();
        }
        
        public void Reset()
        {
            UpdateScale();
            
            _lifeTimer = 0f;
            _hitAreaLifeTimer = 0f;
            
            SyncEcsState();
            gameObject.SetActive(true);
        }
        
        public void SetTag(GlobalTag globalTag)
        {
            this.globalTag = globalTag;
        }

        public void SetData(ExplosionConfig cfg)
        {
            canHitOnCooldown = cfg.canHitOnCooldown;
            ignoreHitFx = cfg.ignoreHitFx;
            
            scale = cfg.scale;
            damage = cfg.damage;
            radius = cfg.scaleAffectsRadius ? cfg.scale * cfg.radius : cfg.radius;
            lifetime = cfg.lifetime;
            hitAreaLifetime = cfg.hitAreaLifetime;
            distanceMult = cfg.distanceMult;
            
            knockbackForce = cfg.knockbackForce;
            knockbackDuration = cfg.knockbackDuration;
            
            pushForce = cfg.pushForce;
            pushUpwardsMod = cfg.pushUpwardsMod;
        }
        
        public void ForceUpdateFromComponent()
        {
            if (!EcsUtils.HasCompInPool<ExplosionComponent>(World, EntityId, out var explosionPool)) return;
            ref var aExplosion = ref explosionPool.Get(EntityId);
            
            scale = aExplosion.Scale;
            damage = aExplosion.Damage;
            radius = aExplosion.Radius;
            
            knockbackForce = aExplosion.KnockbackForce;
            knockbackDuration = aExplosion.KnockbackDuration;
            
            pushForce = aExplosion.PushForce;
            pushUpwardsMod = aExplosion.PushUpwardsMod;
            
            UpdateScale();
            DebCon.Log($"{gameObject.name} are using cloned data from parent projectile", "ExplosionBridge", this);
        }
        
        public bool CheckNeedsDestroy()
        {
            return _lifeTimer >= lifetime;
        }

        public void Tick(float dt)
        {
            if (_hitArea != null)
            {
                _hitArea.enabled = _hitAreaLifeTimer < hitAreaLifetime;
            }

            _lifeTimer += dt;
            _hitAreaLifeTimer += dt;
        }
        
        private void SyncEcsState()
        {
            if (EcsUtils.HasCompInPool<ExplosionComponent>(World, EntityId, out var explosionPool))
            {
                ref var aExplosion = ref explosionPool.Get(EntityId);

                aExplosion.Bridge = this;
                aExplosion.Tag = globalTag;
                aExplosion.HitArea = _hitArea;
                
                aExplosion.CanHitOnCooldown = canHitOnCooldown;
                aExplosion.IgnoreHitFx = ignoreHitFx;
                
                aExplosion.Scale = scale;
                aExplosion.Damage = damage;
                aExplosion.Radius = radius;
                aExplosion.DistanceMult = distanceMult;
                
                aExplosion.KnockbackDuration = knockbackDuration;
                aExplosion.KnockbackForce = knockbackForce;
                
                aExplosion.PushForce = pushForce;
                aExplosion.PushUpwardsMod = pushUpwardsMod;
            }
        }
        
        private void CreateHitArea()
        {
            if (_hitArea != null) return;
            if (hitAreaParent == null) return;

            var go = new GameObject("Hit Area")
            {
                layer = LayerMask.NameToLayer("Hitbox")
            };
            
            go.transform.SetParent(hitAreaParent);
            go.transform.localRotation = Quaternion.identity;
            go.transform.localPosition = Vector3.zero;
            
            _hitArea = go.AddComponent<SphereCollider>();
            
            _hitArea.isTrigger = true;
            _hitArea.radius = hitAreaRadius;
            _hitArea.center = hitAreaOffset;
        }
        
        private void UpdateScale()
        {
            _t.localScale = Vector3.one * scale;

            if (_hitArea != null)
            {
                _hitArea.radius = radius / Mathf.Max(scale, 0.1f);
                _hitArea.center = hitAreaOffset;
            }
        }
    }
}