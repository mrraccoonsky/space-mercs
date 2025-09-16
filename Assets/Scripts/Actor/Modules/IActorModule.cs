using Data.Actor;

namespace Actor.Modules
{
    using Leopotam.EcsLite;
    
    public interface IActorModule
    {
        int EntityId { get; }
        EcsWorld World { get; }
        bool IsEnabled { get; }
        
        void Init(ActorConfig cfg, int entityId, EcsWorld world);
        void SyncEcsState();
        void Tick(float dt);
    }
}