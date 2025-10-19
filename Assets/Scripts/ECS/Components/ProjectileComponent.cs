using System.Collections.Generic;
using UnityEngine;
using Data;

namespace ECS.Components
{
    using Bridges;
    
    public struct ProjectileComponent
    {
        public ProjectileBridge Bridge;
        public GlobalTag Tag;

        public BoxCollider HitBox;
        public HashSet<int> HitEntities;
        
        public bool CanHitOnCooldown;
        public bool IgnoreHitFx;

        public float Scale;
        public float Damage;
        
        public float KnockbackForce;
        public float KnockbackDuration;

        public bool EnableAim;
        public bool CanReuseTarget;
        public float AimDot;
        public float AimRange;
        public Vector3 AimTarget;
        
        public float PushForce;
        public float PushUpwardsMod;
    }
}