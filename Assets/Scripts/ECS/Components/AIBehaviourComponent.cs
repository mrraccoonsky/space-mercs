namespace ECS.Components
{
    public enum AIBehaviorState
    {
        Idle,
        Patrol,
        Chase,
        Attack,
        Dead
    }
    
    public struct AIBehaviorComponent
    {
        public AIBehaviorState CurrentState;
        public float StateTimer;                // time spent in current state
        
        public float DetectionRadius;           // how far the AI can detect targets
        public float FieldOfViewAngle;          // field of view angle in degrees
        
        public float AttackRange;               // maximum distance for attacks
        public float AttackCooldown;            // time between attacks
    }
}