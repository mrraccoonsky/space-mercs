using UnityEngine;

namespace Data.AI
{
    [CreateAssetMenu(fileName = "AIConfig", menuName = "SO/Actor/AIConfig")]
    public class AIConfig : ScriptableObject
    {
        public float detectionRadius = 10f;
        public float attackCooldown = 1f;
        public float attackRange = 5f;
        
        public float randomChaseAttackMinTime = 1f;
        public float randomChaseAttackMaxTime = 3f;
    }
}