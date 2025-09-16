using UnityEngine;

namespace ECS.Components
{
    public struct MovementComponent
    {
        public bool IsGrounded;
        public bool HasJumped;
        
        public Vector3 Velocity;
    }
}