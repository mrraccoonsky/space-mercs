using Core.Camera;
using UnityEngine;
using Data;
using DI.Factories;
using DI.Services;
using ECS.Core;
using Tools;

namespace DI.Installers
{
    using Leopotam.EcsLite;
    using Zenject;
    
    public class ProjectInstaller : MonoInstaller
    {
        [SerializeField] private GlobalVarsConfig globalVars;
        [SerializeField] private CameraController cameraController;
        
        private EcsWorld _world; // is it proper to store it here?
        
        public override void InstallBindings()
        {
            // ecs
            _world = new EcsWorld();
            Container.BindInstance(_world).AsSingle();
            Container.Bind<EcsBootstrap>().AsSingle().NonLazy();
            
            // configs
            Container.BindInstance(globalVars).AsSingle();
            
            // factories
            Container.Bind<IActorFactory>().To<ActorFactory>().AsSingle();
            Container.Bind<IProjectileFactory>().To<ProjectileFactory>().AsSingle();
            Container.Bind<IFxFactory>().To<FxFactory>().AsSingle();
            
            // services
            Container.Bind<IPoolService>().To<PoolService>().AsSingle();
            Container.Bind<IActorSpawnService>().To<ActorSpawnService>().AsSingle();
            Container.Bind<IProjectileService>().To<ProjectileService>().AsSingle();
            Container.Bind<IFxService>().To<FxService>().AsSingle();
            
            // todo: make it changeable in runtime
            Container.Bind<IInputService>().To<KeyboardMouseInputService>().AsSingle();
            
            // singletons
            Container.BindInstance(cameraController).AsSingle();
            
            // event bus as SO resource
            Container.Bind<IEventBusService>().To<EventBusService>().FromScriptableObjectResource("EventBusService")
                .AsSingle()
                .NonLazy();
        }

        private void OnDestroy()
        {
            DebCon.Warn("Destroying ProjectInstaller...");
            Container.UnbindAll();
            _world?.Destroy();
        }
    }
}