using ECS.Components;
using Leopotam.EcsLite;

namespace ECS.AI.States
{
    public interface IAIState
    {
        void Enter();
        void Update(float dt);
        void Exit();
        void GenerateInput(ref InputComponent input, float dt);
    }
    
    public class AIContext
    {
        public int EntityId { get; }
        public EcsWorld World { get; }
        public AIStateMachine StateMachine { get; set; }
        
        public AIContext(int entityId, EcsWorld world)
        {
            EntityId = entityId;
            World = world;
        }
    }
}