using DI.Services;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.InputSystem;
using Zenject;

namespace Core.Camera
{
    public class CameraController : MonoBehaviour
    {
        [SerializeField] private CinemachineCamera playerCamera;

        [Header("Settings:")]
        [SerializeField] private float xSensitivity = 0.5f;
        [SerializeField] private float ySensitivity = 0.5f;
        [SerializeField] private float smoothTime = 0.2f;

        [SerializeField] private bool invertX;
        [SerializeField] private bool invertY;
        
        private CinemachineOrbitalFollow _follow;
        private Vector2 _lastCursorPosition;
        private bool _wasControllingLastFrame;

        [Inject] private IInputService _inputService;
    
        private void Awake()
        {
            _follow = playerCamera.GetComponent<CinemachineOrbitalFollow>();
            _lastCursorPosition = _inputService.CursorPosition;
        }

        private void Update()
        {
            if (_inputService.IsRotatingCamera)
            {
                if (!_wasControllingLastFrame)
                {
                    _lastCursorPosition = _inputService.CursorPosition;
                }
                
                var mouseDelta = _inputService.CursorDelta;

                // horizontal axis
                var horAxis = _follow.HorizontalAxis.Value;
                var xMult = invertX ? -1 : 1;
                
                mouseDelta.x *= xSensitivity * xMult;
                var lerpX = Mathf.Lerp(horAxis, horAxis + mouseDelta.x, smoothTime * Time.deltaTime);
                _follow.HorizontalAxis.Value = lerpX;

                // vertical axis
                var verAxis = _follow.VerticalAxis.Value;
                var yMult = invertY ? -1 : 1;
                
                mouseDelta.y *= ySensitivity * yMult;
                var lerpY = Mathf.Lerp(verAxis, verAxis - mouseDelta.y, smoothTime * Time.deltaTime);
                _follow.VerticalAxis.Value = Mathf.Clamp(lerpY, _follow.VerticalAxis.Range.x, _follow.VerticalAxis.Range.y);

                // lock and hide cursor while controlling camera
                Mouse.current.WarpCursorPosition(_lastCursorPosition);
                Cursor.visible = false;
            }
            
            else
            {
                if (_wasControllingLastFrame)
                {
                    Cursor.lockState = CursorLockMode.None;
                    Mouse.current.WarpCursorPosition(_lastCursorPosition);
                    Cursor.lockState = CursorLockMode.Confined;
                }

                Cursor.visible = true;
            }

            _wasControllingLastFrame = _inputService.IsRotatingCamera;
        }
    }
}