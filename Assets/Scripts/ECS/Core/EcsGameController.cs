using UnityEngine;
using DI.Services;
using ECS.Systems;

namespace ECS.Core
{
    using Leopotam.EcsLite;
    using Zenject;
    
    public class EcsGameController : MonoBehaviour
    {
        private EcsWorld _world;
        private EcsSystems _systems;
        private int _playerEntity;
        
        [Inject] private IInputService _inputService;
        
        private void Start()
        {
            _world = new EcsWorld();
            _systems = new EcsSystems(_world);
            
            _systems.Add(new InputSystem(_world, _inputService));
            _systems.Add(new ActorSystem(_world));
            _systems.Add(new AIPerceptionSystem(_world));
            _systems.Add(new AIDecisionSystem(_world));
            _systems.Add(new ProjectileSystem(_world));
            
            _systems.Init();
        }

        private void Update()
        {
            _systems?.Run();
        }

        private void OnDestroy()
        {
            _systems?.Destroy();
            _systems = null;
        
            _world?.Destroy();
            _world = null;
        }
    }
}
