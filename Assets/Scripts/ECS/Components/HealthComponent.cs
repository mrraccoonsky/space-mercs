using UnityEngine;
using Actor;
using Data;

namespace ECS.Components
{
    public struct HitData
    {
        public Vector3 Pos;
        public Vector3 Dir;
        
        public bool IgnoreFx;
        public float KnockbackForce;
        public float KnockbackDuration;
        public float PushForce;
        public float PushUpwardsMod;
    }
    
    public struct HealthComponent
    {
        public AHealth Module;
        public GlobalTag Tag;
        
        public BoxCollider HitBox;
        
        public float CurrentHealth;
        public float MaxHealth;
        
        public bool IsOnCooldown;
        public bool IsHit;
        
        public HitData LastHit;
        
        public bool IsDead;
        public float DeadTimer;
    }
}