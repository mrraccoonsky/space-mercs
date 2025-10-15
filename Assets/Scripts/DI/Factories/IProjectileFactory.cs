using UnityEngine;
using ECS.Bridges;

namespace DI.Factories
{
    public interface IProjectileFactory
    {
        ProjectileBridge CreateProjectile(GameObject prefab, Vector3 position, Quaternion rotation);
        ExplosionBridge CreateExplosion(GameObject prefab, Vector3 position);
    }
}