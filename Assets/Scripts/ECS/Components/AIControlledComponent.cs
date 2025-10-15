using UnityEngine.AI;
using Data.AI;

namespace ECS.Components
{
    using Bridges;
    
    public struct AIControlledComponent
    {
        public AIActorBridge Bridge;
        public AIConfig Config;
        public NavMeshAgent Agent;
        
        public float LastVisibleTime;
    }
}