using UnityEngine;

namespace ECS.Components
{
    public struct AimerComponent
    {
        public Transform TargetOrigin;
        
        public bool IsAiming;
        public Vector3 AimPosition;
        public Vector3 AimDirection;
    }
}