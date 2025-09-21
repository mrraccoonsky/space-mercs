using UnityEngine;
using Data.Projectile;
using ECS.Bridges;

namespace DI.Factories
{
    public interface IProjectileFactory
    {
        ProjectileBridge Create(ProjectileData data, Vector3 position, Quaternion rotation);
    }
}