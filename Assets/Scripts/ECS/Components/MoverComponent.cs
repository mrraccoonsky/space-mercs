using UnityEngine;

namespace ECS.Components
{
    public struct MoverComponent
    {
        public bool IsGrounded;
        public bool HasJumped;
        
        public Vector3 Velocity;
    }
}