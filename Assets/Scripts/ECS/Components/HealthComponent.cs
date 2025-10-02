using UnityEngine;
using Actor;

namespace ECS.Components
{
    public struct HealthComponent
    {
        public AHealth Module;
        public string Tag;
        public BoxCollider HitBox;
        
        public float CurrentHealth;
        public float MaxHealth;

        public bool IsOnCooldown;
        public bool IsHit;
        public Vector3 LastHitPosition;
        public Vector3 LastHitDirection;
        
        public bool IsDead;
        public float DeadTimer;
    }
}