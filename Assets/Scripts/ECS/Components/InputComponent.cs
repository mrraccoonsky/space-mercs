using UnityEngine;

namespace ECS.Components
{
    public struct InputComponent
    {
        public Vector2 Movement;
        
        public bool IsJumpHit;
        public bool IsJumpHeld;
        public bool IsJumpReleased;
        
        public bool IsAimHit;
        public bool IsAimHeld;
        public bool IsAimReleased;

        public bool IsAttackHit;
        public bool IsAttackHeld;
        public bool IsAttackReleased;

        // player-only, not present in the input service
        public Camera MainCamera;
        public Vector3 AimPosition;
    }
}