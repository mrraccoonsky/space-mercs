namespace DI.Services
{
    using UnityEngine;
    
    public interface IInputService
    {
        Vector2 Movement { get; }

        bool IsActionHit { get; }
        bool IsActionHeld { get; }
        bool IsActionReleased { get; }
        
        bool IsAimHit { get; }
        bool IsAimHeld { get; }
        bool IsAimReleased { get; }
        
        bool IsAttackHit { get; }
        bool IsAttackHeld { get; }
        bool IsAttackReleased { get; }

        Vector2 CursorPosition { get; }
        Vector2 CursorDelta { get; }
        bool IsRotatingCamera { get; }

        void Update();
        Vector3 GetAimPosition();
    }
}