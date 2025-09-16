using UnityEngine;

namespace Data.AI
{
    [CreateAssetMenu(fileName = "AIConfig", menuName = "ScriptableObjects/AIConfig")]
    public class AIConfig : ScriptableObject
    {
        public float detectionRadius = 10f;
        public float fieldOfViewAngle = 60f;
        
        public float attackCooldown = 1f;
        public float attackRange = 5f;
    }
}