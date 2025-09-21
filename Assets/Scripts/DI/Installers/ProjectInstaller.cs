using UnityEngine;
using Data;
using DI.Factories;
using DI.Services;
using ECS.Core;

namespace DI.Installers
{
    using Leopotam.EcsLite;
    using Zenject;
    
    public class ProjectInstaller : MonoInstaller
    {
        [SerializeField] private GlobalVariablesConfig globalVariablesConfig;
        
        private EcsWorld _world; // is it proper to store it here?
        
        public override void InstallBindings()
        {
            // ecs
            _world = new EcsWorld();
            Container.BindInstance(_world).AsSingle();
            Container.Bind<EcsBootstrap>().AsSingle().NonLazy();
            
            // configs
            Container.BindInstance(globalVariablesConfig).AsSingle();
            
            // factories
            Container.Bind<IProjectileFactory>().To<ProjectileFactory>().AsSingle();
            Container.Bind<IFxFactory>().To<FxFactory>().AsSingle();
            
            // services
            Container.Bind<IPoolService>().To<PoolService>().AsSingle();
            Container.Bind<IProjectileService>().To<ProjectileService>().AsSingle();
            Container.Bind<IFXService>().To<FxService>().AsSingle();
            
            // todo: make it changeable in runtime
            Container.Bind<IInputService>().To<KeyboardMouseInputService>().AsSingle(); 
        }

        private void OnDestroy()
        {
            Container.UnbindAll();
            
            _world?.Destroy();
        }
    }
}