using UnityEngine;
using Data;
using Data.Actor;
using ECS.Components;
using ECS.Utils;
using Tools;

namespace Actor
{
    using Leopotam.EcsLite;
    using NaughtyAttributes;
    
    public class AHealth : MonoBehaviour, IActorModule
    {
        [Header("Hitbox")]
        [SerializeField] private Transform hitboxParent;
        [SerializeField] private Vector3 hitboxOffset;
        [SerializeField] private Vector3 hitboxRotation;
        [SerializeField] private Vector3 hitboxSize = Vector3.one;

        [ReadOnly, SerializeField] private GlobalTag globalTag;
        
        [Space]
        [ReadOnly, SerializeField] private float currentHealth;
        [ReadOnly, SerializeField] private float maxHealth;
        [ReadOnly, SerializeField] private float hitCooldown;

        [Space]
        [ReadOnly, SerializeField] private bool isHit;
        [ReadOnly, SerializeField] private float hitTimer = -1f;
        
        [Space]
        [ReadOnly, SerializeField] private bool isDead;
        [ReadOnly, SerializeField] private float deadTimer = -1f;
        
        [Space]
        [ReadOnly, SerializeField] private Vector3 lastHitPos;
        [ReadOnly, SerializeField] private Vector3 lastHitDir;
        [ReadOnly, SerializeField] private float lastHitPushForce;
        [ReadOnly, SerializeField] private float lastHitPushUpwardsMod;
        [ReadOnly, SerializeField] private bool lastHitIgnoreFx;
        
        private BoxCollider _hitbox;
        
        private float _accumHealthChange;
        
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
            if (cfg == null)
            {
                DebCon.Err($"Actor config is null on {gameObject.name}!", "AHealth", gameObject);
                return;
            }
            
            maxHealth = cfg.maxHealth;
            hitCooldown = cfg.hitCooldown;
            
            CreateHitbox();
            
            // add component to pool
            var healthPool = world.GetPool<HealthComponent>();
            healthPool.Add(entityId);
            
            SyncEcsState();
        }

        public void Reset()
        {
            if (!enabled) return;
            
            currentHealth = maxHealth;
            _accumHealthChange = 0f;
            
            isHit = false;
            isDead = false;
            
            hitTimer = -1f;
            deadTimer = -1f;

            lastHitPos = Vector3.zero;
        }

        public void SetTag(GlobalTag globalTag)
        {
            this.globalTag = globalTag;
        }

        public void SyncEcsState()
        {
            if (EcsUtils.HasCompInPool<HealthComponent>(World, EntityId, out var healthPool))
            {
                ref var aHealth = ref healthPool.Get(EntityId);

                aHealth.Module = this;
                aHealth.Tag = globalTag;
                aHealth.HitBox = _hitbox;
                
                aHealth.CurrentHealth = currentHealth;
                aHealth.MaxHealth = maxHealth;
                
                aHealth.IsOnCooldown = hitTimer > 0f;
                aHealth.IsHit = isHit;
                aHealth.LastHitPos = lastHitPos;
                aHealth.LastHitDir = lastHitDir;
                
                aHealth.IsDead = isDead;
                aHealth.DeadTimer = isDead ? deadTimer : -1f;
            }
            
            // reset hit flag after sync
            if (isHit)
            {
                isHit = false;
            }
        }

        public void Tick(float dt)
        {
            if (hitTimer > 0f)
            {
                hitTimer -= dt;
            }

            if (deadTimer > 0f)
            {
                deadTimer -= dt;
            }

            if (_accumHealthChange != 0f)
            {
                currentHealth += _accumHealthChange;
                currentHealth = Mathf.Clamp(currentHealth, 0f, maxHealth);
                
                if (_accumHealthChange < 0f)
                {
                    DebCon.Info($"{gameObject.name} got HIT for {_accumHealthChange}, currentHealth = {currentHealth}", "AHealth", gameObject);
                }
                else
                {
                    DebCon.Info($"{gameObject.name} got HEALED for {_accumHealthChange}, currentHealth = {currentHealth}", "AHealth", gameObject);
                }
            }

            // single frame check if health is 0 and not dead yet - start dead timer
            if (currentHealth <= 0f && !isDead)
            {
                deadTimer = 2f;
                
                // check if entity has both ragdoll and animator components
                if (EcsUtils.HasCompInPool<RagdollComponent>(World, EntityId, out var ragdollPool) &&
                    EcsUtils.HasCompInPool<AnimatorComponent>(World, EntityId, out var animatorPool))
                {
                    ref var aAnimator = ref animatorPool.Get(EntityId);
                    aAnimator.Module?.SetAnimatorEnabled(false);
                    
                    ref var aRagdoll = ref ragdollPool.Get(EntityId);
                    aRagdoll.Module?.SetRagdollEnabled(true);

                    var forceDir = lastHitDir;
                    DebCon.Log($"Adding force to {gameObject.name}'s ragdoll at {forceDir}, force = {lastHitPushForce}, upwardsMod = {lastHitPushUpwardsMod}", "AHealth", gameObject);
                    
                    forceDir *= lastHitPushForce;
                    forceDir.y = lastHitPushUpwardsMod;
                    
                    var moveDir = Vector3.zero;
                    if (EcsUtils.HasCompInPool<MoverComponent>(World, EntityId, out var moverPool))
                    {
                        ref var aMover = ref moverPool.Get(EntityId);
                        moveDir = aMover.Velocity;
                        moveDir.y = 0f;
                    }
                    
                    aRagdoll.Module?.AddForce(forceDir, moveDir, lastHitPos);
                }
            }
            
            isDead = currentHealth <= 0f;

            if (_hitbox)
            {
                _hitbox.enabled = !isDead;
            }
            
            _accumHealthChange = 0f;
        }
        
        public void StoreHitData(Vector3 pos, Vector3 dir, float force, float upwardsMod, bool ignoreHitFx)
        {
            lastHitPos = pos;
            lastHitDir = dir;
            
            lastHitPushForce = force;
            lastHitPushUpwardsMod = upwardsMod;
            lastHitIgnoreFx = ignoreHitFx;
        }
        
        public void ChangeHealth(float value)
        {
            if (value < 0f)
            {
                if (hitTimer <= 0f)
                {
                    isHit = true;
                    hitTimer = hitCooldown;
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
    }
}