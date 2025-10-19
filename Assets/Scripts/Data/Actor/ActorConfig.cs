using UnityEngine;
using Data.Weapon;

namespace Data.Actor
{
    using NaughtyAttributes;
    
    [CreateAssetMenu(fileName = "ActorConfig", menuName = "SO/Actor/ActorConfig")]
    public class ActorConfig : ScriptableObject
    {
        // aHealth
        [BoxGroup("Health")] public float maxHealth = 100f;
        [BoxGroup("Health")] public float hitCooldown = 0.25f;
        
        // aMover
        [BoxGroup("Movement")] public float speed = 5f;
        [BoxGroup("Movement")] public bool canBeKnockedBack = true;

        [BoxGroup("Jump")] public float jumpHeight = 1.5f;
        [BoxGroup("Jump")] public float jumpDelay = 0.1f;
        [BoxGroup("Jump")] public float jumpCooldown = 0.1f;
        [BoxGroup("Jump")] public float velocityDecrement = 10f;
    
        [BoxGroup("Gravity")] public float gravityMultiplier = 1f;
        [BoxGroup("Gravity")] public float fallMultiplier = 2.5f;
        [BoxGroup("Gravity")] public float lowJumpMultiplier = 2f;
        
        // aAimer
        [BoxGroup("Target Origin")] public float defaultTargetDistance = 1f;
        [BoxGroup("Target Origin")] public float targetMoveSpeed = 10f;
        
        [BoxGroup("Aim")] public float rotationSpeed = 500f;
        [BoxGroup("Aim")] public bool aimTowardsAttackDirection = true;
        
        // aAttacker
        [BoxGroup("Attack"), Expandable] public WeaponConfig weaponCfg;
    }
}