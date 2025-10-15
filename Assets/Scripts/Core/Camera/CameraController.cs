using UnityEngine;
using UnityEngine.InputSystem;
using Unity.Cinemachine;
using DI.Services;
using Tools;

namespace Core.Camera
{
    using Cinemachine;
    using Zenject;
    
    using Camera = UnityEngine.Camera;

    public enum ScreenSide
    {
        Bottom = 1 << 0,
        Right = 1 << 1,
        Top = 1 << 2,
        Left = 1 << 3,
    }
    
    public class CameraController : MonoBehaviour
    {
        [SerializeField] private Camera cam;
        [SerializeField] private CinemachineCamera playerCamera;

        [Header("Settings")]
        [SerializeField] private LayerMask raycastLayerMask;
        [SerializeField] private float cornerUpdateInterval = 0.2f;
        [SerializeField] private float xSensitivity = 0.5f;
        [SerializeField] private float ySensitivity = 0.5f;
        [SerializeField] private float smoothTime = 0.2f;

        [SerializeField] private bool invertX;
        [SerializeField] private bool invertY;

        private Transform _t;
        private CinemachineOrbitalFollow _follow;
        private CinemachinePanTilt _panTilt;
        private CinemachineForwardOnly _forwardOnly;
        
        private readonly RaycastHit[] _hits = new RaycastHit[1];
        private Vector3[] _corners;
        private float _lastCornersUpdateTime;
        
        private Vector2 _lastCursorPosition;
        private bool _wasControllingLastFrame;

        [Inject] private IInputService _inputService;

        public Transform CurrentTarget => _follow?.FollowTarget;
    
        private void Awake()
        {
            cam = Camera.main;
            _t = cam?.transform;
            
            _follow = playerCamera.GetComponent<CinemachineOrbitalFollow>();
            _panTilt = playerCamera.GetComponent<CinemachinePanTilt>();
            _forwardOnly = playerCamera.GetComponent<CinemachineForwardOnly>();
            
            _lastCursorPosition = _inputService.CursorPosition;
        }

        private void OnDrawGizmosSelected()
        {
            if (cam == null) return;
            
            _corners = cam.orthographic
                ? GetOrthoViewportCorners()
                : GetPerspectiveViewportCorners();
                
            if (_corners == null || _corners.Length < 4) return;
                
            Gizmos.color = Color.yellow;
            foreach (var corner in _corners)
            {
                Gizmos.DrawWireSphere(corner, 0.2f);
            }
                
            Gizmos.color = Color.darkGreen;
            Gizmos.DrawLine(_corners[0], _corners[1]);
            Gizmos.DrawLine(_corners[1], _corners[2]);
            Gizmos.DrawLine(_corners[2], _corners[3]);
            Gizmos.DrawLine(_corners[3], _corners[0]);
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
                var horAxis = _panTilt.PanAxis.Value;
                var xMult = invertX ? -1 : 1;
                
                mouseDelta.x *= xSensitivity * xMult;
                
                var lerpFollowX = Mathf.Lerp(horAxis, horAxis + mouseDelta.x, smoothTime * Time.deltaTime);
                _follow.HorizontalAxis.Value = Mathf.Clamp(lerpFollowX, _follow.HorizontalAxis.Range.x, _follow.HorizontalAxis.Range.y);
                
                var lerpPanX = Mathf.Lerp(horAxis, horAxis + mouseDelta.x, smoothTime * Time.deltaTime);
                _panTilt.PanAxis.Value = Mathf.Clamp(lerpPanX, _panTilt.PanAxis.Range.x, _panTilt.PanAxis.Range.y);
                
                // vertical axis
                // var verAxis = _follow.VerticalAxis.Value;
                var yMult = invertY ? -1 : 1;
                
                mouseDelta.y *= ySensitivity * yMult;
                // var lerpFollowY = Mathf.Lerp(verAxis, verAxis - mouseDelta.y, smoothTime * Time.deltaTime);
                // _follow.VerticalAxis.Value = Mathf.Clamp(lerpFollowY, _follow.VerticalAxis.Range.x, _follow.VerticalAxis.Range.y);
                
                var lerpTiltY = Mathf.Lerp(_panTilt.TiltAxis.Value, _panTilt.TiltAxis.Value + mouseDelta.y, smoothTime * Time.deltaTime);
                _panTilt.TiltAxis.Value = Mathf.Clamp(lerpTiltY, _panTilt.TiltAxis.Range.x, _panTilt.TiltAxis.Range.y);

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
        
        public void SetCameraTarget(Transform target)
        {
            if (playerCamera == null)
            {
                DebCon.Warn("Player camera is null", "CameraController", gameObject);
                return;
            }
            
            playerCamera.Follow = target;
        }
        
        public void ResetForwardOnly(float delay = 0f)
        {
            if (_forwardOnly == null) return;
            
            _forwardOnly.ResetExtremeValue(delay);
        }

        public Vector3 GetClampedViewportPosition(Vector3 position, float buffer = 0f)
        {
            if (cam == null) return position;
            if (_inputService.IsRotatingCamera) return position;
    
            _corners = cam.orthographic
                ? GetOrthoViewportCorners()
                : GetPerspectiveViewportCorners();
    
            if (_corners == null || _corners.Length < 4)
            {
                DebCon.Warn("Failed to get viewport corners", "CameraController", gameObject);
                return position;
            }
    
            // transform position and corners to camera's local space (XZ plane aligned with camera forward)
            var localPos = _t.InverseTransformPoint(position);
            var localCorners = new Vector3[4];
            for (var i = 0; i < 4; i++)
            {
                localCorners[i] = _t.InverseTransformPoint(_corners[i]);
            }
    
            // clamp in local space
            var x = Mathf.Clamp(localPos.x, localCorners[0].x + buffer, localCorners[1].x - buffer);
            var z = Mathf.Clamp(localPos.z, localCorners[0].z + buffer, localCorners[2].z - buffer);
    
            // Transform back to world space
            var clampedLocal = new Vector3(x, localPos.y, z);
            return _t.TransformPoint(clampedLocal);
        }
        
        public Vector3 GetRandomOffscreenPosition(ScreenSide side, float buffer = 0f)
        {
            if (cam == null)
            {
                DebCon.Warn("Camera is null", "CameraController", gameObject);
                return Vector3.zero;
            }
            
            _corners = cam.orthographic
                ? GetOrthoViewportCorners()
                : GetPerspectiveViewportCorners();
            
            if (_corners == null || _corners.Length < 4)
            {
                DebCon.Warn("Failed to get viewport corners", "CameraController", gameObject);
                return Vector3.zero;
            }
            
            var pos = Vector3.zero;
            switch (side)
            {
                case ScreenSide.Bottom:
                    pos.x = Random.Range(_corners[0].x - buffer, _corners[1].x + buffer);
                    pos.z = (_corners[0].z + _corners[1].z) * 0.5f - buffer;
                    break;
                
                case ScreenSide.Right:
                    pos.x = (_corners[1].x + _corners[2].x) * 0.5f + buffer;
                    pos.z = Random.Range(_corners[1].z - buffer, _corners[2].z + buffer);
                    break;
                
                case ScreenSide.Top:
                    pos.x = Random.Range(_corners[2].x - buffer, _corners[3].x + buffer);
                    pos.z = (_corners[2].z + _corners[3].z) * 0.5f + buffer;
                    break;
                
                case ScreenSide.Left:
                    pos.x = (_corners[0].x + _corners[3].x) * 0.5f - buffer;
                    pos.z = Random.Range(_corners[0].z - buffer, _corners[3].z + buffer);
                    break;
            }
            
            return pos;
        }
        
        public bool CheckIfPointIsVisible(Vector3 point, float buffer = 0f)
        {
            if (cam == null)
            {
                DebCon.Warn("Camera is null", "CameraController", gameObject);
                return false;
            }
            
            var viewportPoint = cam.WorldToViewportPoint(point);
            if (viewportPoint.z <= 0f - buffer) return false;
            
            var xPass = viewportPoint.x >= 0f - buffer && viewportPoint.x <= 1f + buffer;
            var yPass = viewportPoint.y >= 0f - buffer && viewportPoint.y <= 1f + buffer;
            return xPass && yPass;
        }
        
        private Vector3[] GetPerspectiveViewportCorners()
        {
            if (cam == null) return null;
            if (_corners != null && Time.time - _lastCornersUpdateTime < cornerUpdateInterval)
            {
                return _corners;
            }
            
            var corners = new Vector3[4];
            var origin = cam.transform.position;
            var targets = new[] {
                cam.ViewportToWorldPoint(new Vector3(0f, 0f, cam.farClipPlane)),    // bottom left
                cam.ViewportToWorldPoint(new Vector3(1f, 0f, cam.farClipPlane)),    // bottom right
                cam.ViewportToWorldPoint(new Vector3(1f, 1f, cam.farClipPlane)),    // top right
                cam.ViewportToWorldPoint(new Vector3(0f, 1f, cam.farClipPlane))     // top left
            };

            var pos = origin;
            for (var i = 0; i < targets.Length; i++)
            {
                var dir = targets[i] - origin;
                var ray = new Ray(pos, dir);
                if (Physics.RaycastNonAlloc(ray, _hits, cam.farClipPlane, raycastLayerMask) > 0)
                {
                    corners[i] = _hits[0].point;
                }
                else
                {
                    DebCon.Err("Raycast failed", "CameraController", gameObject);
                    return null;
                }
            }

            _lastCornersUpdateTime = Time.time;
            return corners;
        }

        private Vector3[] GetOrthoViewportCorners()
        {
            if (cam == null) return null;
            if (_corners != null && Time.time - _lastCornersUpdateTime < cornerUpdateInterval)
            {
                return _corners;
            }
            
            var corners = new Vector3[4];
            var origins = new[] {
                cam.ViewportToWorldPoint(new Vector3(0f, 0f, cam.nearClipPlane)),   // bottom left
                cam.ViewportToWorldPoint(new Vector3(1f, 0f, cam.nearClipPlane)),   // bottom right
                cam.ViewportToWorldPoint(new Vector3(1f, 1f, cam.nearClipPlane)),   // top right
                cam.ViewportToWorldPoint(new Vector3(0f, 1f, cam.nearClipPlane))    // top left
            };
                
            var targets = new[] {
                cam.ViewportToWorldPoint(new Vector3(0f, 0f, cam.farClipPlane)),    // bottom left
                cam.ViewportToWorldPoint(new Vector3(1f, 0f, cam.farClipPlane)),    // bottom right
                cam.ViewportToWorldPoint(new Vector3(1f, 1f, cam.farClipPlane)),    // top right
                cam.ViewportToWorldPoint(new Vector3(0f, 1f, cam.farClipPlane))     // top left
            };
                
            for (var i = 0; i < targets.Length; i++)
            {
                var pos = origins[i];
                var dir = targets[i] - origins[i];
                var ray = new Ray(pos, dir);
                
                if (Physics.RaycastNonAlloc(ray, _hits, cam.farClipPlane, raycastLayerMask) > 0)
                {
                    corners[i] = _hits[0].point;
                }
                else
                {
                    DebCon.Err("Raycast failed", "CameraController", gameObject);
                    return null;
                }
            }
            
            _lastCornersUpdateTime = Time.time;
            return corners;
        }
    }
}