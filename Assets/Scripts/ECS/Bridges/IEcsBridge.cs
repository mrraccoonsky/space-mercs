using Data;

namespace ECS.Bridges
{
    using Leopotam.EcsLite;
    
    public interface IEcsBridge
    {
        public int EntityId { get; }
        public EcsWorld World { get; }
        void Init(int entityId, EcsWorld world);
        void Tick(float dt);

        void FixedTick(float dt)
        {
            // optional method that can be implemented by bridges that need to know the fixed delta time
        }
        
        void SetTag(GlobalTag globalTag)
        {
            // optional method that can be implemented by bridges that need to know the global tag
        }
    }
}