using UnityEngine;
using ECS.Core;
using Tools;

namespace Core
{
    using Zenject;
    
    public class Bootstrap : MonoBehaviour
    {
        [SerializeField] private ConsoleVerbosity consoleVerbosity = ConsoleVerbosity.Verbose;
        
        private EcsBootstrap _ecsBootstrap;
        
        [Inject] private DiContainer _container;
        
        private void Awake()
        {
            DebCon.Verbosity = consoleVerbosity;

            InitEcs(); 
        }
        
        private void InitEcs()
        {
            _ecsBootstrap = new EcsBootstrap(_container);
            _ecsBootstrap.Init();
        }

        private void Update()
        {
            _ecsBootstrap?.Tick();
        }
    }
}