using Leopotam.EcsLite;

namespace ECS.Bridges
{
    public interface IEcsBridge
    {
        public int EntityId { get; }
        public EcsWorld World { get; }
        
        void Init(int entityId, EcsWorld world);
        void Tick(float dt);
    }
}