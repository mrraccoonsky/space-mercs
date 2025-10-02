using System;
using System.Linq;
using UnityEngine;
using Core.Camera;
using Tools;

namespace ECS.Bridges
{
    using Leopotam.EcsLite;
    using NaughtyAttributes;
    
    using Random = UnityEngine.Random;
    
    public class SpawnerArea : ActorSpawnerBridge
    {
        [Serializable] private struct AreaConfig
        {
            public Vector3 offset;
            public Vector3 rotation;
            public Vector3 size;
        }
        
        [Header("Area:")]
        [SerializeField] private Vector3 areaOffset;
        [SerializeField] private Vector3 areaRotation;
        [SerializeField] private Vector3 areaSize = Vector3.one;
        
        [SerializeField] private bool offscreenMode = true;
        [SerializeField] private bool withinBoundsMode = true;
        [SerializeField, EnumFlags] private ScreenSide screenSides;
        
        private BoxCollider _hitbox;
        
        private void OnDrawGizmos()
        {
            Gizmos.color = Color.softBlue;
            
            var areaPosition = transform.position;
            var areaRotationQuat = transform.rotation * Quaternion.Euler(areaRotation);
            
            Gizmos.matrix = Matrix4x4.TRS(areaPosition, areaRotationQuat, Vector3.one);
            Gizmos.DrawWireCube(areaOffset, areaSize);
        }
        
        public override void Init(int entityId, EcsWorld world)
        {
            base.Init(entityId, world);
            
            CreateArea();
        }
        
        public override void Tick(float dt)
        {
            if (offscreenMode)
            {
                var playerPos = GetPlayerPos();
                if (!IsInsideArea(playerPos, 0.5f))
                {
                    PlayerPos = Vector3.zero;
                    return;
                }
                
                PlayerPos = playerPos;
            }
            
            base.Tick(dt);
        }

        protected override void Spawn()
        {
            var prefabIdx = Random.Range(0, prefabs.Length);
            var prefab = prefabs[prefabIdx];
            var position = Vector3.zero;

            // _playerPos != Vector3.zero means the player is inside the area
            if (offscreenMode && PlayerPos != Vector3.zero)
            {
                position = GetOffscreenSpawnPosition();
            }
            else
            {
                var bounds = _hitbox.bounds;
                position.x = Random.Range(bounds.min.x, bounds.max.x);
                position.z = Random.Range(bounds.min.z, bounds.max.z);
            }
            
            var rotation = PlayerPos != Vector3.zero
                ? Quaternion.LookRotation(PlayerPos - position)
                : Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);
            
            var actor = SpawnService.Spawn(prefab, position, rotation);
            if (actor == null) return;
            
            SpawnedIds.Add(actor.EntityId);
            TotalSpawned++;
            DebCon.Info($"{actor.gameObject.name} spawned at position {position}", "ActorSpawnerBridge", actor.gameObject);
        }
        
        private void CreateArea()
        {
            if (_hitbox != null) return;
            
            var go = new GameObject("Area")
            {
                layer = LayerMask.NameToLayer("Area")
            };
            
            go.transform.SetParent(transform);
            go.transform.localRotation = Quaternion.Euler(areaRotation);
            go.transform.localPosition = Vector3.zero;
            
            _hitbox = go.AddComponent<BoxCollider>();
            _hitbox.isTrigger = true;
            _hitbox.size = areaSize;
            _hitbox.center = areaOffset;
        }
        
        private bool IsInsideArea(Vector3 point, float yOffset = 0f)
        {
            if (_hitbox == null)
            {
                DebCon.Warn("Hitbox is null", "ActorSpawnerBridge", gameObject);
                return false;
            }
            var bounds = _hitbox.bounds;
            return bounds.Contains(point + Vector3.up * yOffset);
        }
        
        private Vector3 GetOffscreenSpawnPosition()
        {
            var cam = Camera.main;
            if (cam == null)
            {
                DebCon.Warn("Main camera not found for offscreen spawning", "ActorSpawnerBridge");
                return Vector3.zero;
            }
            
            var selected = Enum.GetValues(typeof(ScreenSide))
                .Cast<ScreenSide>()
                .Where(s => screenSides.HasFlag(s))
                .ToList();

            if (selected.Count == 0)
            {
                DebCon.Warn("No valid screen positions found for offscreen spawning", "ActorSpawnerBridge");
                return Vector3.zero;
            }
            
            var attempts = 0;
            var maxAttempts = 25;
            Vector3 pos;

            do
            {
                var r = Random.Range(0, selected.Count);
                pos = CameraController.GetRandomOffscreenPosition(selected[r], 2f);
                pos.y = areaOffset.y - areaSize.y * 0.5f;
                attempts++;
                
                var offscreenPass = !CameraController.CheckIfPointIsVisible(pos);
                var boundsPass = !withinBoundsMode || IsInsideArea(pos, 0.5f);
                var groundPass = HasValidGround(pos, 5f);

                if (offscreenPass && boundsPass && groundPass)
                {
                    DebCon.Info($"Valid spawn position found after {attempts} attempts: {pos}", "ActorSpawnerBridge");
                    Debug.DrawLine(pos, pos + Vector3.up * 5f, Color.green, 1f);
                    return pos;
                }
                
                // invalid spawn position debug
                Debug.DrawLine(pos, pos + Vector3.up * 5f, Color.red, 0.5f);
            }
            while (attempts < maxAttempts);
            
            DebCon.Warn($"Failed to find valid spawn position after {attempts} attempts", "ActorSpawnerBridge");
            return pos;
        }

        private bool HasValidGround(Vector3 position, float yOffset = 0f, float maxDistance = 10f)
        {
            var ray = new Ray(position + Vector3.up * yOffset, Vector3.down);
            var hit = Physics.Raycast(ray, out var hitInfo, maxDistance, LayerMask.GetMask("Ground", "Obstacle"));
            if (!hit) return false;
            
            var layer = hitInfo.collider.gameObject.layer;
            return layer == LayerMask.NameToLayer("Ground");
        }
    }
}
