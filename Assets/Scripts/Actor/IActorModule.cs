using Data;
using Data.Actor;

namespace Actor
{
    using Leopotam.EcsLite;
    
    public interface IActorModule
    {
        int EntityId { get; }
        EcsWorld World { get; }
        bool IsEnabled { get; }
        
        void Init(ActorConfig cfg, int entityId, EcsWorld world);
        void Reset();
        void SyncEcsState();
        void Tick(float dt);
        
        void SetTag(GlobalTag globalTag)
        {
            // optional method that can be implemented by modules that need to know the global tag
        }
    }
}