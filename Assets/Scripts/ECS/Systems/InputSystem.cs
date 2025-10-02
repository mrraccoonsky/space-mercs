using UnityEngine;
using DI.Services;
using ECS.Components;
using Tools;

namespace ECS.Systems
{
    using Leopotam.EcsLite;
    
    public class InputSystem : IEcsRunSystem, IEcsInitSystem
    {
        private readonly EcsWorld _world;
        private readonly IInputService _inputService;
        
        private EcsFilter _playerFilter;

        public InputSystem(EcsWorld world, IInputService inputService)
        {
            _world = world;
            _inputService = inputService;
        }
        
        public void Init(IEcsSystems systems)
        {
            _playerFilter = _world.Filter<InputComponent>()
                .Exc<AIControlledComponent>()
                .End();
        }

        public void Run(IEcsSystems systems)
        {
            _inputService.Update();
            
            var inputPool = _world.GetPool<InputComponent>();
            
            foreach (var entity in _playerFilter)
            {
                ref var aInput = ref inputPool.Get(entity);
                UpdateInputComponent(ref aInput, _inputService);
            }
        }
        
        private static void UpdateInputComponent(ref InputComponent input, IInputService inputService)
        {
            if (input.MainCamera == null)
            {
                input.MainCamera = Camera.main;
            }
            
            input.Movement = inputService.Movement;

            input.IsJumpHit = inputService.IsActionHit;
            input.IsJumpHeld = inputService.IsActionHeld;
            input.IsJumpReleased = inputService.IsActionReleased;

            input.IsAimHit = inputService.IsAimHit;
            input.IsAimHeld = inputService.IsAimHeld;
            input.IsAimReleased = inputService.IsAimReleased;

            input.IsAttackHit = inputService.IsAttackHit;
            input.IsAttackHeld = inputService.IsAttackHeld;
            input.IsAttackReleased = inputService.IsAttackReleased;
            
            input.AimPosition = inputService.GetAimPosition();
        }
    }
}