using UnityEngine;
using ECS.Bridges;
using ECS.Components;

namespace ECS.Systems
{
    using Leopotam.EcsLite;
    
    public class ActorSystem : IEcsRunSystem
    {
        private readonly EcsWorld _world;
        private readonly EcsFilter _actorsFilter;
        private readonly EcsFilter _aiControlledFilter;
        
        public ActorSystem(EcsWorld world)
        {
            _world = world;
            
            _actorsFilter = _world.Filter<ActorComponent>().End();
            _aiControlledFilter = _world.Filter<AIControlledComponent>().End();
            
            // todo: it shouldn't be done here
            var bridges = Object.FindObjectsByType<ActorBridge>(FindObjectsSortMode.None);
            foreach (var bridge in bridges)
            {
                var entity = _world.NewEntity();
                bridge.Init(entity, _world);
            }
        }
        
        public void Run(IEcsSystems systems)
        {
            var actorPool = _world.GetPool<ActorComponent>();
            var aiControlledPool = _world.GetPool<AIControlledComponent>();
            var dt = Time.deltaTime;

            foreach (var e in _actorsFilter)
            {
                ref var aActor = ref actorPool.Get(e);
                aActor.Bridge?.Tick(dt);
            }

            foreach (var e in _aiControlledFilter)
            {
                ref var aAIControlled = ref aiControlledPool.Get(e);
                aAIControlled.Bridge?.Tick(dt);
            }
        }
    }
}