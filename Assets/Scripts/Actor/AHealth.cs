using UnityEngine;
using Data.Actor;
using ECS.Components;
using ECS.Utils;
using Tools;

namespace Actor
{
    using Leopotam.EcsLite;
    
    public class AHealth : MonoBehaviour, IActorModule
    {
        [Header("Health:")]
        [SerializeField] private float maxHealth = 100f;
        [SerializeField] private float hitCooldown = 0.25f;
        
        [Header("Hitbox:")]
        [SerializeField] private Transform hitboxParent;
        [SerializeField] private Vector3 hitboxOffset;
        [SerializeField] private Vector3 hitboxRotation;
        [SerializeField] private Vector3 hitboxSize = Vector3.one;
        
        [Header("Visuals:")]
        
        [SerializeField] private ParticleSystem hitFxPrefab;
        
        private BoxCollider _hitbox;
        private ParticleSystem _hitFx;
        
        private float _currentHealth;
        private float _accumHealthChange;

        private bool _isHit;
        private Vector3 _lastHitPosition;
        private Vector3 _lastHitDirection;
        private float _hitTimer = -1f;
        
        private bool _isDead;

        public bool IsEnabled { get; private set; }
        public int EntityId { get; private set; }
        public EcsWorld World { get; private set; }
        
        private void OnDrawGizmosSelected()
        {
            if (hitboxParent == null) return;
            
            Gizmos.color = Color.yellow;
            
            var hitboxPosition = hitboxParent.position;
            var hitboxRotationQuat = hitboxParent.rotation * Quaternion.Euler(hitboxRotation);
            
            Gizmos.matrix = Matrix4x4.TRS(hitboxPosition, hitboxRotationQuat, Vector3.one);
            Gizmos.DrawWireCube(hitboxOffset, hitboxSize);
        }
        
        public void Init(ActorConfig cfg, int entityId, EcsWorld world)
        {
            IsEnabled = enabled;
            if (!IsEnabled) return;
            
            EntityId = entityId;
            World = world;

            // init config (
            if (cfg)
            {
                maxHealth = cfg.maxHealth;
                hitCooldown = cfg.hitCooldown;
            }
            
            _currentHealth = maxHealth;
            
            CreateHitbox();
            CreateHitFx();
            
            var healthPool = world.GetPool<HealthComponent>();
            healthPool.Add(entityId);
            
            SyncEcsState();
        }

        public void SyncEcsState()
        {
            if (EcsUtils.HasCompInPool<HealthComponent>(World, EntityId, out var healthPool))
            {
                ref var aHealth = ref healthPool.Get(EntityId);

                aHealth.Module = this;
                aHealth.Tag = gameObject.tag;
                aHealth.HitBox = _hitbox;
                
                aHealth.CurrentHealth = _currentHealth;
                aHealth.MaxHealth = maxHealth;
                
                aHealth.IsOnCooldown = _hitTimer > 0f;
                aHealth.IsHit = _isHit;
                aHealth.LastHitPosition = _lastHitPosition;
                aHealth.LastHitDirection = _lastHitDirection;
                
                aHealth.IsDead = _isDead;
            }
        }

        public void Tick(float dt)
        {
            if (_isHit)
            {
                _isHit = false;
            }
            
            if (_hitTimer > 0f)
            {
                _hitTimer -= dt;
            }

            if (_accumHealthChange != 0f)
            {
                _currentHealth += _accumHealthChange;
                _currentHealth = Mathf.Clamp(_currentHealth, 0f, maxHealth);
                
                if (_accumHealthChange < 0f)
                {
                    PlayHitFx();
                    DebCon.Info(gameObject.name + " got HIT for " + _accumHealthChange + ", current hp: " + _currentHealth, "AHealth", gameObject);
                }
                else
                {
                    DebCon.Info(gameObject.name + " got HEALED for " + _accumHealthChange + ", current hp: " + _currentHealth, "AHealth", gameObject);
                }
            }
            
            _isDead = _currentHealth <= 0f;
            _accumHealthChange = 0f;

            if (_hitbox)
            {
                _hitbox.enabled = !_isDead;
            }
        }
        
        public void RegisterHitData(Vector3 hitPosition, Vector3 hitDirection)
        {
            _lastHitPosition = hitPosition;
            _lastHitDirection = hitDirection;
        }
        
        public void ChangeHealth(float value)
        {
            if (value < 0f)
            {
                if (_hitTimer <= 0f)
                {
                    _isHit = true;
                    _hitTimer = hitCooldown;
                }
                else
                {
                    return;
                }
            }
            
            _accumHealthChange += value;
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
        
        private void CreateHitFx()
        {
            if (hitFxPrefab == null) return;
            _hitFx = Instantiate(hitFxPrefab, transform);
        }
        
        private void PlayHitFx()
        {
            if (_hitFx == null) return;
            
            _hitFx.transform.localPosition = transform.InverseTransformPoint(_lastHitPosition);
            _hitFx.transform.rotation = Quaternion.LookRotation(_lastHitDirection);
            
            _hitFx.Play();
        }
    }
}