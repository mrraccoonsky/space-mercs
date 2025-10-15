using UnityEngine;

namespace ECS.Components
{
    public struct TransformComponent
    {
        public Transform Transform;
        public Vector3 Position;
        public Quaternion Rotation;
    }
}