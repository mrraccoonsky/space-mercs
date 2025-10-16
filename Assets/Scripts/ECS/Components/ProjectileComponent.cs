using System.Collections.Generic;
using Data;

namespace ECS.Components
{
    using Bridges;
    
    public struct ProjectileComponent
    {
        public ProjectileBridge Bridge;
        public GlobalTag Tag;
        
        public HashSet<int> HitEntities;
        
        public bool CanHitOnCooldown;
        public bool IgnoreHitFx;

        public float Scale;
        public float Damage;
        
        public float PushForce;
        public float PushUpwardsMod;
    }
}