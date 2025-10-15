using System.Collections.Generic;
using Data;

namespace ECS.Components
{
    using Bridges;
    
    public struct ExplosionComponent
    {
        public ExplosionBridge Bridge;
        public GlobalTag Tag;
        
        public HashSet<int> HitEntities;
        
        public bool CanHitOnCooldown;
        public bool IgnoreHitFx;
        
        public float Damage;
        public float PushForce;
        public float PushUpwardsMod;
        
        public float Radius;
        public float DistanceMult;
    }
}