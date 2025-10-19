using UnityEngine;
using Data.Explosion;

namespace Data.Weapon
{
    using NaughtyAttributes;
    
    public enum OriginCycleMode
    {
        None,
        Reset,
        PingPong
    }

    public enum ScatterType
    {
        None,
        Random,
        Cone
    }
    
    [CreateAssetMenu(fileName = "WeaponConfig", menuName = "SO/Weapon/WeaponConfig")]
    public class WeaponConfig : ScriptableObject
    {
        [BoxGroup("General")] public bool enableFriendlyFire;
        [BoxGroup("General")] public bool canHitOnCooldown;
        [BoxGroup("General")] public bool ignoreHitFx;
        
        [BoxGroup("Attack")] public float attackCooldown = 1f;
        [BoxGroup("Attack")] public int projectileCount = 1;
        [BoxGroup("Attack")] public float projectileCooldown = 1f;
        
        [BoxGroup("Attack")] public int burstCount = 1;
        [BoxGroup("Attack")] public float burstCooldown = 1f;
        [BoxGroup("Attack")] public bool holdBurstTransform;
        
        [BoxGroup("Scatter")] public float scatterAngle;
        [BoxGroup("Scatter")] public ScatterType scatterType;
        
        [BoxGroup("Origin Cycle Mode")] public OriginCycleMode originCycleMode = OriginCycleMode.None;
        [BoxGroup("Origin Cycle Mode")] public bool switchAfterEachShot;
        
        [BoxGroup("Projectile")] public GameObject projectilePrefab;
        [BoxGroup("Projectile")] public float tilt;
        [BoxGroup("Projectile")] public float scale = 1f;
        [BoxGroup("Projectile")] public float damage = 10f;
        [BoxGroup("Projectile")] public float speed = 10f;
        [BoxGroup("Projectile")] public float lifetime = 1f;
        
        [Space]
        [BoxGroup("Projectile")] public float knockbackForce;
        [BoxGroup("Projectile")] public float knockbackDuration;
        
        [Space]
        [BoxGroup("Projectile")] public int penetrationCount;
        [BoxGroup("Projectile")] public float hitboxEnableDelay;
        
        [BoxGroup("Aim")] public bool enableAim;
        [BoxGroup("Aim"), ShowIf("enableAim")] public bool canReuseTarget;
        [BoxGroup("Aim"), ShowIf("enableAim")] public float aimDot = 0.1f;
        [BoxGroup("Aim"), ShowIf("enableAim")] public float aimRange = 10f;
        [BoxGroup("Aim"), ShowIf("enableAim")] public float aimSpeed = 5f;
        [BoxGroup("Aim"), ShowIf("enableAim")] public float aimDelay = 0.2f;
        [BoxGroup("Aim"), ShowIf("enableAim")] public float aimMoveSpeedMult = 1f;
        
        [BoxGroup("Rigidbody")] public bool enableRigidbody;
        [BoxGroup("Rigidbody"), ShowIf("enableRigidbody")] public bool rbHitObstacles;
        [BoxGroup("Rigidbody"), ShowIf("enableRigidbody")] public float rbTorque;
        
        [BoxGroup("Ragdoll")] public float pushForce = 1f;
        [BoxGroup("Ragdoll")] public float pushUpwardsMod = 1f;
        
        [BoxGroup("Explosion"), Expandable] public ExplosionConfig explosionConfig;
    }
}