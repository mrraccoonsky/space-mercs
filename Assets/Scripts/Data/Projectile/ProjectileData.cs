using UnityEngine;

namespace Data.Projectile
{
    public struct ProjectileData
    {
        public readonly GameObject prefab;
        public readonly string tag;
        public readonly float speed;
        public readonly float lifetime;
        public readonly int penetrationCount;
        public readonly bool canHitOnCooldown;

        public ProjectileData(GameObject prefab, string tag, float speed, float lifetime, int penetrationCount, bool canHitOnCooldown)
        {
            this.prefab = prefab;
            this.tag = tag;
            this.speed = speed;
            this.lifetime = lifetime;
            this.penetrationCount = penetrationCount;
            this.canHitOnCooldown = canHitOnCooldown;
        }
    }
}