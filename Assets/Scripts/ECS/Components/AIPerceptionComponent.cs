using UnityEngine;

namespace ECS.Components
{
    public struct AIPerceptionComponent
    {
        public int targetEntityId;              // entity id of the current target
        
        public float DistanceToTarget;          // distance to the current target
        public Vector3 DirectionToTarget;       // direction vector to the current target
        public float TimeSinceLastSawTarget;    // time since AI last had line of sight to target
        public Vector3 LastKnownTargetPosition; // last position where target was seen
        
        public bool DetectionRadiusPass;        // can see the target within detection radius
        public bool HealthCheckPass;            // can see the target is alive
        public bool LineOfSightPass;            // can see the target with line of sight
        
        public bool HasTarget => targetEntityId >= 0;
    }
}