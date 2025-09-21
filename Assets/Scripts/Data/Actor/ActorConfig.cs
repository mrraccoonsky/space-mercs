using Actor;
using ECS.Bridges;
using NaughtyAttributes;
using UnityEngine;

namespace Data.Actor
{
    [CreateAssetMenu(fileName = "ActorConfig", menuName = "ScriptableObjects/ActorConfig")]
    public class ActorConfig : ScriptableObject
    {
        // Mover
        [BoxGroup("Movement:")] public float speed = 5f;

        [BoxGroup("Jump:")] public float jumpHeight = 1.5f;
        [BoxGroup("Jump:")] public float jumpDelay = 0.1f;
        [BoxGroup("Jump:")] public float jumpCooldown = 0.1f;
        [BoxGroup("Jump:")] public float velocityDecrement = 10f;
    
        [BoxGroup("Gravity:")] public float gravityMultiplier = 1f;
        [BoxGroup("Gravity:")] public float fallMultiplier = 2.5f;
        [BoxGroup("Gravity:")] public float lowJumpMultiplier = 2f;
        
        // Aimer
        [BoxGroup("Target Origin:")] public float defaultTargetDistance = 1f;
        [BoxGroup("Target Origin:")] public float targetMoveSpeed = 10f;
        
        [BoxGroup("Aim:")] public float rotationSpeed = 500f;
        [BoxGroup("Aim:")] public bool aimTowardsAttackDirection = true;
        
        // Attacker
        // todo: partially move into separate weapon module
        [BoxGroup("Attack:")] public float attackCooldown = 0.8f;
        
        [BoxGroup("Attack:")] public bool holdBurstTransform;
        [BoxGroup("Attack:")] public int burstCount = 2;
        [BoxGroup("Attack:")] public float burstCooldown = 0.2f;

        [BoxGroup("Scatter: ")] public float scatterAngle = 0f;
        [BoxGroup("Scatter: ")] public ScatterType scatterType = ScatterType.None;
        
        [BoxGroup("Projectile:")] public GameObject projectilePrefab;
        [BoxGroup("Projectile:")] public int projectileCount = 3;
        [BoxGroup("Projectile:")] public float projectileCooldown = 0.05f;
        [BoxGroup("Projectile:")] public float projectileLifetime = 0.5f;
        [BoxGroup("Projectile:")] public float projectileSpeed = 15f;
        [BoxGroup("Projectile:")] public int projectilePenetrationCount = 1;
        [BoxGroup("Projectile:")] public bool canHitOnCooldown = false;
        
        // Health
        [BoxGroup("Health:")] public float maxHealth = 100f;
        [BoxGroup("Health:")] public float hitCooldown = 0.25f;
    }
}