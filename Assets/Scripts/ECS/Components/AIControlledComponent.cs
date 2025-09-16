using UnityEngine.AI;
using Actor;
using Data.AI;
using ECS.Bridges;

namespace ECS.Components
{
    public struct AIControlledComponent
    {
        public AIActorBridge Bridge;
        public AIConfig Config;
        public NavMeshAgent Agent;
    }
}