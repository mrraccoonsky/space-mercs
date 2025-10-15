using UnityEngine;
using Actor;
using Data;

namespace ECS.Components
{
    public struct HealthComponent
    {
        public AHealth Module;
        public GlobalTag Tag;
        public BoxCollider HitBox;
        
        public float CurrentHealth;
        public float MaxHealth;

        public bool IsOnCooldown;
        public bool IsHit;
        public bool LastHitIgnoreFx;
        
        public Vector3 LastHitPos;
        public Vector3 LastHitDir;
        
        public bool IsDead;
        public float DeadTimer;
    }
}