using UnityEngine;

namespace Data.Explosion
{
    using NaughtyAttributes;
    
    [CreateAssetMenu(fileName = "ExplosionConfig", menuName = "SO/Weapon/ExplosionConfig")]
    public class ExplosionConfig : ScriptableObject
    {
        [BoxGroup("General")] public bool enableFriendlyFire;
        [BoxGroup("General")] public bool canHitOnCooldown;
        [BoxGroup("General")] public bool ignoreHitFx;
        
        // true = damage value will be cloned from parent projectile
        [BoxGroup("Damage")] public bool cloneDamage;
        [BoxGroup("Damage"), HideIf("cloneDamage")] public float damage = 10f;

        [BoxGroup("Explosion")] public GameObject explosionPrefab;
        [BoxGroup("Explosion")] public float radius = 1f;
        [BoxGroup("Explosion")] public float lifetime = 1f;
        [BoxGroup("Explosion")] public float hitAreaLifetime = 0.5f;
        [BoxGroup("Explosion")] public float distanceMult = 1f;
        
        // true = push values will be cloned from parent projectile
        [BoxGroup("Ragdoll")] public bool clonePush;
        [BoxGroup("Ragdoll"), HideIf("clonePush")] public float pushForce = 1f;
        [BoxGroup("Ragdoll"), HideIf("clonePush")] public float pushUpwardsMod = 1f;
    }
}