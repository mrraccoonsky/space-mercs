using Core.Camera;
using Data;
using DI.Services;
using ECS.Systems;
using Tools;

namespace ECS.Core
{
    using Leopotam.EcsLite;
    using Zenject;
    
    public class EcsBootstrap
    {
        private readonly EcsWorld _world;
        private readonly GlobalVarsConfig _config;
        private readonly IEventBusService _eventBus;
        private readonly IInputService _inputService;
        private readonly IProjectileService _projectileService;
        private readonly CameraController _cameraController;
        
        private EcsSystems _systems;

        public EcsBootstrap(DiContainer container)
        {
            _world = container.Resolve<EcsWorld>();
            
            _config = container.Resolve<GlobalVarsConfig>();
            _eventBus = container.Resolve<IEventBusService>();
            _inputService = container.Resolve<IInputService>();
            _projectileService = container.Resolve<IProjectileService>();
            _cameraController = container.Resolve<CameraController>();
        }
        
        public void Init()
        {
            _systems = new EcsSystems(_world);
            _systems.Add(new InputSystem(_world, _inputService))
                .Add(new ActorSystem(_world, _cameraController))
                .Add(new SpawnerSystem(_world))
                .Add(new AIPerceptionSystem(_world, _eventBus))
                .Add(new AIDecisionSystem(_world, _eventBus))
                .Add(new ProjectileSystem(_world, _projectileService))
                .Init();
        }

        public void Tick()
        {
            _systems?.Run();
        }
        
        public void Destroy()
        {
            DebCon.Warn("Destroying EcsBootstrap...");
            _systems?.Destroy();
        }
    }
}
