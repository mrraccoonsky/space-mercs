using UnityEngine;
using Data;
using DI.Services;
using Input;

namespace DI.Installers
{
    using Zenject;
    
    public class ProjectInstaller : MonoInstaller
    {
        [SerializeField] private GlobalVariablesConfig globalVariablesConfig;
        
        public override void InstallBindings()
        {
            // configs
            Container.BindInstance(globalVariablesConfig).AsSingle();
            
            // services
            // todo: make it changeable in runtime to be able to switch input methods in settings menu
            Container.Bind<IInputService>()
                .To<KeyboardMouseInput>()
                .AsSingle();
        }
    }
}