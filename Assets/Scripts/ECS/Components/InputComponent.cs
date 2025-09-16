namespace ECS.Components
{
    using UnityEngine;
    
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

        public Vector2 CursorPosition;
        public Vector2 CursorDelta;
        
        // Player-only
        public bool IsRotatingCamera;

        // NOT PRESENT IN THE INPUT SERVICE
        public Camera MainCamera;
        public Vector3 AimPosition;
    }
}