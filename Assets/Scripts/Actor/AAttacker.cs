using UnityEngine;
using Data;
using Data.Actor;
using Data.Weapon;
using DI.Services;
using ECS.Components;
using ECS.Utils;
using Tools;

namespace Actor
{
    using Leopotam.EcsLite;
    using NaughtyAttributes;
    using Zenject;
    
    public class AAttacker : MonoBehaviour, IActorModule
    {
        [SerializeField] private WeaponConfig weapon;
        [SerializeField] private Transform originsRoot; // todo: make it changeable

        [ReadOnly, SerializeField] private GlobalTag globalTag;
        
        [Space]
        [ReadOnly, SerializeField] private Transform[] origins;
        [ReadOnly, SerializeField] private int currentOriginIndex;
        [ReadOnly, SerializeField] private int currentCycleDirection = 1;

        [Space]
        [ReadOnly, SerializeField] private bool holdTransform;
        [ReadOnly, SerializeField] private Vector3 holdSpawnPos;
        [ReadOnly, SerializeField] private Quaternion holdSpawnRot;
        
        [Space]
        [ReadOnly, SerializeField] private float scatterAngle;
        [ReadOnly, SerializeField] private float attackCooldownTimer;
        [ReadOnly, SerializeField] private float projectileCooldownTimer;
        [ReadOnly, SerializeField] private float burstCooldownTimer;
        [ReadOnly, SerializeField] private int projectileCount;
        [ReadOnly, SerializeField] private int burstCount;
        
        [ReadOnly, SerializeField] private bool isAttacking;
        [ReadOnly, SerializeField] private bool isAttackTriggered;
        
        public bool IsEnabled { get; private set; }
        public int EntityId { get; private set; }
        public EcsWorld World { get; private set; }

        [Inject] private IPoolService _poolService;
        [Inject] private IProjectileService _projectileService;
        
        public void Init(ActorConfig cfg, int entityId, EcsWorld world)
        {
            IsEnabled = enabled;
            if (!IsEnabled) return;
            
            EntityId = entityId;
            World = world;
            
            // init config
            if (cfg == null)
            {
                DebCon.Err($"Actor config is null on {gameObject.name}!", "AAttacker", gameObject);
                return;
            }
            
            weapon = cfg.weaponCfg;
            if (weapon == null)
            {
                DebCon.Err($"Weapon config is null on {gameObject.name}!", "AAttacker", gameObject);
                return;
            }
            
            // todo: make it switchable
            origins = new Transform[originsRoot.childCount];
            for (var i = 0; i < originsRoot.childCount; i++)
            {
                var child = originsRoot.GetChild(i);
                origins[i] = child;
            }
            
            // add component to pool
            var attackerPool = world.GetPool<AttackerComponent>();
            attackerPool.Add(entityId);
            
            SyncEcsState();
        }

        public void Reset()
        {
            if (!enabled) return;

            holdSpawnPos = Vector3.zero;
            holdSpawnRot = Quaternion.identity;
            holdTransform = false;
            
            attackCooldownTimer = 0f;
         
            burstCount = 0;
            burstCooldownTimer = 0f;
            scatterAngle = 0f;
        
            projectileCount = 0;
            projectileCooldownTimer = 0f;

            isAttacking = false;
            isAttackTriggered = false;
        }
        
        public void SetTag(GlobalTag globalTag)
        {
            this.globalTag = globalTag;
        }
        
        public void SyncEcsState()
        {
            if (EcsUtils.HasCompInPool<AttackerComponent>(World, EntityId, out var attackerPool))
            {
                ref var aAttack = ref attackerPool.Get(EntityId);
                aAttack.IsAttacking = isAttacking;
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
            HandleAttack(aInput, dt);
        }

        private void HandleAttack(InputComponent input, float dt)
        {
            // main attack cooldown timer
            if (attackCooldownTimer > 0f)
            {
                attackCooldownTimer -= dt;
                
                if (attackCooldownTimer <= 0f)
                {
                    attackCooldownTimer = 0f;
                    burstCount = 0;
                }
            }
            
            // single burst (with multiple projectiles) cooldown timer
            burstCooldownTimer -= dt;

            if (burstCooldownTimer <= 0f)
            {
                burstCooldownTimer = 0f;
            }
            
            // update origins root positions and rotation (hold logics)
            if (holdTransform)
            {
                originsRoot.SetPositionAndRotation(holdSpawnPos, holdSpawnRot);
            }
            else
            {
                originsRoot.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
            }

            if (!isAttackTriggered)
            {
                if (weapon == null)
                {
                    DebCon.Warn("Weapon is null", "AAttacker", gameObject);
                    return;
                }
                
                // attack input check
                var shotCountPass = burstCount < weapon.burstCount;
                var shotCooldownPass = burstCooldownTimer <= 0f;
                
                if (input.IsAttackHeld && shotCountPass && shotCooldownPass)
                {
                    isAttackTriggered = true;
                }
            }
            else
            {
                // interval between shots in single burst timer
                projectileCooldownTimer -= dt;

                if (projectileCooldownTimer <= 0f)
                {
                    projectileCooldownTimer = 0f;
                }

                // end of single attack (burst)
                if (projectileCount >= weapon.projectileCount)
                {
                    ResetBurst();
                    
                    // set attack cooldown timer if it's not set
                    if (attackCooldownTimer == 0f)
                    {
                        attackCooldownTimer = weapon.attackCooldown;
                    }
                    // add calculated attack cooldown based on performed bursts
                    // todo: check if calculated correctly. have some issues on multi-shots ???
                    else
                    {
                        var shotsLeft = burstCount;
                        attackCooldownTimer = weapon.attackCooldown / shotsLeft;
                    }
                }
                
                // projectile spawn
                var countPass = burstCount < weapon.burstCount;
                var burstCooldownPass = burstCooldownTimer <= 0f;
                var projectileCooldownPass = projectileCooldownTimer <= 0f;
                
                // wait until the conditions are met
                if (!countPass || !burstCooldownPass || !projectileCooldownPass) return;

                // keep first shot transform for transform holding
                if (projectileCount == 0 && weapon.holdBurstTransform)
                {
                    holdSpawnPos = originsRoot.transform.position;
                    holdSpawnRot = originsRoot.transform.rotation;
                    holdTransform = true;
                }

                // handle single- or multi-shot based on projectile cooldown
                var singleStep = GetScatterSingleStep();
                var initAngle = GetScatterInitAngle(singleStep);
                var isSingleShot = weapon.projectileCooldown > 0f;
                
                if (isSingleShot)
                {
                    HandleSingleShot(initAngle, singleStep);
                }
                else
                {
                    HandleMultiShot(initAngle, singleStep);
                }
            }
        }
        
        private void ResetBurst()
        {
            if (weapon == null) return;
            
            isAttackTriggered = false;
            holdTransform = false;
            burstCooldownTimer = weapon.burstCooldown;
            projectileCooldownTimer = 0f;
            burstCount++;
            scatterAngle = 0f;
            projectileCount = 0;
            
            // switch direction for single origin cone scatter with ping pong cycle mode (who needs that anyways?)
            if (origins.Length == 1 && weapon.scatterType == ScatterType.Cone && weapon.originCycleMode == OriginCycleMode.PingPong)
            {
                currentCycleDirection *= -1;
            }
        }
        
        private void HandleSingleShot(float initAngle, float singleStep)
        {
            if (projectileCount == 0)
            {
                scatterAngle = initAngle;
            }
            
            projectileCount++;
            projectileCooldownTimer = weapon.projectileCooldown;
            
            if (weapon.originCycleMode == OriginCycleMode.None || origins.Length == 1 || weapon.burstCooldown <= 0f)
            {
                foreach (var o in origins)
                {
                    SpawnProjectile(o);

                    if (weapon.scatterType == ScatterType.Random)
                    {
                        scatterAngle = GetScatterInitAngle(singleStep);
                    }
                }
                
                UpdateCurrentScatterAngle(singleStep);
            }
            else
            {
                SpawnProjectile(origins[currentOriginIndex]);
                UpdateCurrentScatterAngle(singleStep);

                if (weapon.switchAfterEachShot || projectileCount >= weapon.projectileCount)
                {
                    CycleOrigins(weapon.originCycleMode);
                }
            }
        }

        private void HandleMultiShot(float initAngle, float singleStep)
        {
            projectileCount = weapon.projectileCount;
            projectileCooldownTimer = 0f;

            if (weapon.originCycleMode == OriginCycleMode.None || origins.Length == 1)
            {
                foreach (var o in origins)
                {
                    scatterAngle = initAngle;
                    
                    for (var i = 0; i < weapon.projectileCount; i++)
                    {
                        SpawnProjectile(o);
                        UpdateCurrentScatterAngle(singleStep);
                    }
                }
            }
            else
            {
                scatterAngle = initAngle;
                    
                for (var i = 0; i < weapon.projectileCount; i++)
                {
                    SpawnProjectile(origins[currentOriginIndex]);
                    UpdateCurrentScatterAngle(singleStep);
                }
                
                CycleOrigins(weapon.originCycleMode);
            }
        }
        
        private float GetScatterSingleStep()
        {
            if (weapon == null) return 0f;
            
            return weapon.scatterAngle / Mathf.Max(1, weapon.projectileCount - 1);
        }
        
        private float GetScatterInitAngle(float signleStep)
        {
            if (weapon == null) return 0f;
            
            return weapon.scatterType switch
            {
                ScatterType.Cone => -signleStep * 0.5f * (weapon.projectileCount - 1) * currentCycleDirection,
                ScatterType.Random => Random.Range(-weapon.scatterAngle * 0.5f, weapon.scatterAngle * 0.5f),
                _ => 0f
            };
        }

        private void UpdateCurrentScatterAngle(float singleStep)
        {
            if (weapon == null) return;
            
            if (weapon.scatterType == ScatterType.Cone)
            {
                scatterAngle += singleStep * currentCycleDirection;
            }
            else
            {
                scatterAngle = GetScatterInitAngle(singleStep);
            }
        }
        
        private void CycleOrigins(OriginCycleMode cycleMode)
        {
            if (origins.Length <= 1) return;
            
            if (cycleMode == OriginCycleMode.Reset)
            {
                currentOriginIndex++;

                if (currentOriginIndex >= origins.Length)
                {
                    currentOriginIndex = 0;
                }
            }
            else if (cycleMode == OriginCycleMode.PingPong)
            {
                currentOriginIndex += currentCycleDirection;

                if (currentOriginIndex >= origins.Length)
                {
                    currentOriginIndex = origins.Length - 1;
                    currentCycleDirection *= -1;
                }
                else if (currentOriginIndex < 0)
                {
                    currentOriginIndex = 0;
                    currentCycleDirection *= -1;
                }
            }
        }
        
        private void SpawnProjectile(Transform origin)
        {
            if (weapon == null)
            {
                DebCon.Warn("Weapon is null", "AAttacker", gameObject);
                return;
            }
            
            var rot = Quaternion.Euler(origin.eulerAngles + Vector3.up * scatterAngle);
            var pos = origins.Length > 1 && weapon.scatterType == ScatterType.Cone && weapon.originCycleMode == OriginCycleMode.None
                ? transform.position + rot * origin.localPosition
                : origin.position;
            
            _projectileService.SpawnProjectile(weapon, globalTag, pos, rot);
        }
    }
}