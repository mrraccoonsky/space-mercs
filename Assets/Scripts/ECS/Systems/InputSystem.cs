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
        private static LayerMask _raycastLayerMask;

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

            // todo: it shouldn't be here
            _raycastLayerMask = LayerMask.GetMask("Ground");
            
            var actorPool = _world.GetPool<ActorComponent>();
            var inputPool = _world.GetPool<InputComponent>();
            
            // add input component to player characters only
            foreach (var e in _world.Filter<ActorComponent>().Exc<AIControlledComponent>().End())
            {
                ref var aActor = ref actorPool.Get(e);
                var entityId = aActor.Bridge.EntityId;
                if (!inputPool.Has(entityId))
                {
                    inputPool.Add(entityId);
                    DebCon.Log($"Added input component to entity {entityId}", "InputSystem");
                }
            }
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
            
            // todo: it shouldn't be done in system
            if (input.MainCamera)
            {
                var ray = input.MainCamera.ScreenPointToRay(inputService.CursorPosition);
                Physics.Raycast(ray, out var hitInfo, 100f, _raycastLayerMask);
                input.AimPosition = hitInfo.point;
            }
        }
    }
}