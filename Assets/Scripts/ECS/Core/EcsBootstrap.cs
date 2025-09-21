using Data;
using DI.Services;
using ECS.Systems;

namespace ECS.Core
{
    using Leopotam.EcsLite;
    using Zenject;
    
    public class EcsBootstrap
    {
        private readonly EcsWorld _world;
        private readonly GlobalVariablesConfig _config;
        private readonly IInputService _inputService;
        private readonly IProjectileService _projectileService;
        
        private EcsSystems _systems;

        public EcsBootstrap(DiContainer container)
        {
            _world = container.Resolve<EcsWorld>();
            
            _config = container.Resolve<GlobalVariablesConfig>();
            _inputService = container.Resolve<IInputService>();
            _projectileService = container.Resolve<IProjectileService>();
        }
        
        public void Init()
        {
            _systems = new EcsSystems(_world);
            _systems.Add(new InputSystem(_world, _inputService))
                .Add(new ActorSystem(_world))
                .Add(new AIPerceptionSystem(_world))
                .Add(new AIDecisionSystem(_world))
                .Add(new ProjectileSystem(_world, _projectileService))
                .Init();
        }

        public void Tick()
        {
            _systems?.Run();
        }
        
        private void OnDestroy()
        {
            _systems?.Destroy();
        }
    }
}
