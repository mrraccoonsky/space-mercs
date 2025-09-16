namespace ECS.Components
{
    using UnityEngine;
    
    public struct AimComponent
    {
        public Transform TargetOrigin;
        
        public bool IsAiming;
        public Vector3 AimPosition;
        public Vector3 AimDirection;
    }
}