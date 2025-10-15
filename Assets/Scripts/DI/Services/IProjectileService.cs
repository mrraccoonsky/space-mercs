using UnityEngine;
using Data;
using Data.Explosion;
using Data.Weapon;
using ECS.Bridges;

namespace DI.Services
{
    public interface IProjectileService
    { 
        ProjectileBridge SpawnProjectile(WeaponConfig cfg, GlobalTag tag, Vector3 position, Quaternion rotation);
        void DestroyProjectile(int entityId);
        
        ExplosionBridge SpawnExplosion(ExplosionConfig cfg, GlobalTag tag, Vector3 position);
        void DestroyExplosion(int entityId);
    }
}