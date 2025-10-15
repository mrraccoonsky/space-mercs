using UnityEngine;
using Unity.Cinemachine;
using Tools;

namespace Cinemachine
{
    public class CinemachineForwardOnly : CinemachineExtension
    {
        private enum Axis { X, Y, Z }

        [SerializeField] private Axis axis = Axis.Z;
        [SerializeField] private bool positiveDirection = true;
        [SerializeField] private bool debugMode;

        private float _initDelay = 1f;
        private float _extremeValue;
        
        public bool IsInit { get; private set; }

        protected override void PostPipelineStageCallback(CinemachineVirtualCameraBase vcam, CinemachineCore.Stage stage, ref CameraState state, float deltaTime)
        {
            // only modify the camera position after the Body stage (where positioning happens)
            if (stage != CinemachineCore.Stage.Body) return;

            // initialize on first update
            if (!IsInit)
            {
                if (_initDelay > 0f)
                {
                    _initDelay -= deltaTime;
                    return;
                }
                
                _extremeValue = GetAxisValue(state.RawPosition, axis);
                
                IsInit = true;
                _initDelay = -1f;
                
                if (debugMode)
                {
                    DebCon.Info($"Initialized with extreme value = {_extremeValue}", "CinemachineForwardOnly", gameObject);
                }
            }

            var currentPos = state.RawPosition;
            var currentValue = GetAxisValue(currentPos, axis);

            if (positiveDirection)
            {
                if (currentValue > _extremeValue)
                {
                    _extremeValue = currentValue;
                }
                else if (currentValue < _extremeValue)
                {
                    currentPos = SetAxisValue(currentPos, axis, _extremeValue);
                    state.PositionCorrection += currentPos - state.RawPosition;
                }
            }
            else
            {
                if (currentValue < _extremeValue)
                {
                    _extremeValue = currentValue;
                }
                else if (currentValue > _extremeValue)
                {
                    currentPos = SetAxisValue(currentPos, axis, _extremeValue);
                    state.PositionCorrection += currentPos - state.RawPosition;
                }
            }
        }

        public void ResetExtremeValue(float delay = 0f)
        {
            IsInit = false;
            _initDelay = delay;
            
            if (debugMode)
            {
                DebCon.Info($"Resetting extreme value - will reinitialize on next update with delay = {delay}", "CinemachineForwardOnly", gameObject);
            }
        }
        
        private float GetAxisValue(Vector3 position, Axis axis)
        {
            return axis switch
            {
                Axis.X => position.x,
                Axis.Y => position.y,
                Axis.Z => position.z,
                _ => position.x
            };
        }

        private Vector3 SetAxisValue(Vector3 position, Axis axis, float value)
        {
            switch (axis)
            {
                case Axis.X:
                    position.x = value;
                    break;
                case Axis.Y:
                    position.y = value;
                    break;
                case Axis.Z:
                    position.z = value;
                    break;
            }
            
            return position;
        }
    }
}
