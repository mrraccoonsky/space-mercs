using System.Collections.Generic;

namespace ECS.Components
{
    using Bridges;
    
    public struct ProjectileComponent
    {
        public ProjectileBridge Bridge;
        public string Tag;
        
        public HashSet<int> HitEntities;
        public bool CanHitOnCooldown;
    }
}