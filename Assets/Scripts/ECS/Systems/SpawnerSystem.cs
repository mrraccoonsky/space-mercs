using UnityEngine;
using ECS.Components;

namespace ECS.Systems
{
    using Leopotam.EcsLite;
    
    public class SpawnerSystem : IEcsRunSystem
    {
        private readonly EcsWorld _world;
        private readonly EcsFilter _spawnFilter;
        
        public SpawnerSystem(EcsWorld world)
        {
            _world = world;
            _spawnFilter = _world.Filter<SpawnerComponent>().End();
            
            // bridges init on awake
        }
        
        public void Run(IEcsSystems systems)
        {
            var spawnerPool = _world.GetPool<SpawnerComponent>();
            foreach (var entity in _spawnFilter)
            {
                ref var spawner = ref spawnerPool.Get(entity);
                spawner.Bridge?.Tick(Time.deltaTime);
            }
        }
    }
}
