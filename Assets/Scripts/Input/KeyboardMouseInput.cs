using UnityEngine;
using UnityEngine.InputSystem;
using DI.Services;

namespace Input
{
    public class KeyboardMouseInput : IInputService
    {
        private Camera MainCamera { get; set; }
        
        public Vector2 Movement { get; private set; }
        
        public bool IsActionHit { get; private set; }
        public bool IsActionHeld { get; private set; }
        public bool IsActionReleased { get; private set; }

        public bool IsAimHit { get; private set; }
        public bool IsAimHeld { get; private set; }
        public bool IsAimReleased { get; private set; }

        public bool IsAttackHit { get; private set; }
        public bool IsAttackHeld { get; private set; }
        public bool IsAttackReleased { get; private set; }
        
        public Vector2 CursorPosition { get; private set; }
        public Vector2 CursorDelta { get; private set; }
        public bool IsRotatingCamera { get; private set; }
        
        public void Update()
        {
            if (MainCamera == null)
            {
                MainCamera = Camera.main;
            }
            
            // todo: utilize InputSystem actions instead of direct access to Keyboard and Mouse
            var movement = Vector2.zero;
            if (Keyboard.current.wKey.isPressed) movement.y += 1f;
            if (Keyboard.current.sKey.isPressed) movement.y -= 1f;
            if (Keyboard.current.aKey.isPressed) movement.x -= 1f;
            if (Keyboard.current.dKey.isPressed) movement.x += 1f;
            movement.Normalize();
            Movement = movement;

            IsActionHit = Keyboard.current.spaceKey.wasPressedThisFrame;
            IsActionHeld = Keyboard.current.spaceKey.isPressed;
            IsActionReleased = Keyboard.current.spaceKey.wasReleasedThisFrame;
            
            IsAimHit = Mouse.current.rightButton.wasPressedThisFrame;
            IsAimHeld = Mouse.current.rightButton.isPressed;
            IsAimReleased = Mouse.current.rightButton.wasReleasedThisFrame;
            // AimPosition value are controlled by InputSystem

            IsAttackHit = Mouse.current.leftButton.wasPressedThisFrame;
            IsAttackHeld = Mouse.current.leftButton.isPressed;
            IsAttackReleased = Mouse.current.leftButton.wasReleasedThisFrame;
            
            CursorPosition = Mouse.current.position.ReadValue();
            CursorDelta = Mouse.current.delta.ReadValue();
            IsRotatingCamera = Keyboard.current.leftShiftKey.isPressed;
        }
    }
}