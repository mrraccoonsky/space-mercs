using System.Collections.Generic;
using UnityEngine;
using Core.Camera;
using DI.Services;
using ECS.Components;
using ECS.Utils;
using EventSystem;
using Tools;

namespace ECS.Bridges
{
    using Leopotam.EcsLite;
    using Zenject;
    
    using Random = Random;
    
    public class ActorSpawnerBridge : MonoBehaviour, IEcsBridge
    {
        [Header("General:")]
        [SerializeField, Min(1)] protected int prefabsPerSpawn = 1;
        [SerializeField] [Min(-1)] protected int totalSpawnedCount = -1;  // -1 means infinite
        [SerializeField] [Min(-1)] protected int spawnLimit = -1;         // -1 means no limit
        [SerializeField] protected float spawnInterval = 1f;

        [SerializeField] protected GameObject[] prefabs; // todo: use configs instead

        [Inject] protected CameraController CameraController;
        [Inject] protected IActorSpawnService SpawnService;
        [Inject] private IEventBusService _eventBus;
        
        protected Vector3 PlayerPos;
        protected readonly HashSet<int> SpawnedIds = new();
        protected int TotalSpawned;
        
        private EcsFilter _playerFilter;
        private float _spawnTimer;
        
        public int EntityId { get; private set;  }
        [Inject] public EcsWorld World { get; private set; }
        
        private void OnDestroy() 
        {
            DebCon.Warn($"Destroying ActorSpawnerBridge {EntityId}...");
            _eventBus.Unsubscribe<ActorDestroyedEvent>(HandleActorDestroyed);
            SpawnedIds.Clear();
            World.DelEntity(EntityId);
        }

        private void Awake()
        {
            var entity = World.NewEntity();
            Init(entity, World);
        }
        
        public virtual void Init(int entityId, EcsWorld world)
        {
            EntityId = entityId;
            World = world;
            
            var spawnerPool = World.GetPool<SpawnerComponent>();
            ref var aSpawner = ref spawnerPool.Add(EntityId);
            aSpawner.Bridge = this;

            _playerFilter = World.Filter<TransformComponent>()
                .Inc<ActorComponent>()
                .Exc<AIControlledComponent>()
                .End();
            
            _spawnTimer = spawnInterval;
            _eventBus.Subscribe<ActorDestroyedEvent>(HandleActorDestroyed);
        }

        public virtual void Tick(float dt)
        {
            // the overriden methods are usually updated before this base method...
            
            if (totalSpawnedCount > 0 && TotalSpawned >= totalSpawnedCount) return;
            if (spawnLimit > 0 && SpawnedIds.Count >= spawnLimit) return;

            if (_spawnTimer > 0f)
            {
                _spawnTimer -= dt;
            }

            if (_spawnTimer <= 0f)
            {
                _spawnTimer = spawnInterval;
                
                var spawnCount = spawnLimit > 0 && SpawnedIds.Count + prefabsPerSpawn > spawnLimit
                    ? spawnLimit - SpawnedIds.Count
                    : prefabsPerSpawn;
                
                spawnCount = totalSpawnedCount > 0 && TotalSpawned + spawnCount > totalSpawnedCount
                    ? totalSpawnedCount - TotalSpawned
                    : spawnCount;
                
                for (var i = spawnCount; i > 0; i--)
                {
                    Spawn();
                }
            }
        }
        
        protected virtual void Spawn()
        {
            // base realisation, use overriden methods instead! /!\
            
            var prefabIdx = Random.Range(0, prefabs.Length);
            var prefab = prefabs[prefabIdx];
            var position = Vector3.zero;
            var rotation = Quaternion.identity;
            
            var actor = SpawnService.Spawn(prefab, position, rotation);
            if (actor == null) return;
            
            SpawnedIds.Add(actor.EntityId);
            TotalSpawned++;
            DebCon.Info($"{actor.gameObject.name} spawned at position {position}", "ActorSpawnerBridge", actor.gameObject);
        }

        protected Vector3 GetPlayerPos()
        {
            foreach (var entity in _playerFilter)
            {
                if (!EcsUtils.HasCompInPool<TransformComponent>(World, entity, out var transformPool)) continue;
                
                ref var aTransform = ref transformPool.Get(entity);
                return aTransform.Position;
            }

            return Vector3.zero;
        }

        private void HandleActorDestroyed(ActorDestroyedEvent e)
        {
            var entityId = e.EntityId;
            if (!SpawnedIds.Contains(entityId)) return;
            SpawnedIds.Remove(entityId);
        }
    }
}
