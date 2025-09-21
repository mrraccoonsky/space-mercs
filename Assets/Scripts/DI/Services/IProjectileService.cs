using UnityEngine;
using Data.Projectile;

namespace DI.Services
{
    public interface IProjectileService
    { 
        void Spawn(ProjectileData data, Vector3 position, Quaternion rotation);
        void Destroy(int entityId);
    }
}