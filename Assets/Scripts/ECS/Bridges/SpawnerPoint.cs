using UnityEngine;
using Tools;

namespace ECS.Bridges
{
    public class SpawnerPoint : ActorSpawnerBridge
    {
        [Header("Point:")]
        [SerializeField] private float pointMinDistance = 10f;
        [SerializeField] private float pointMaxDistance = 15f;
        
        public override void Tick(float dt)
        {
            PlayerPos = GetPlayerPos();

            if (PlayerPos != Vector3.zero)
            {
                var distance = (PlayerPos - transform.position).magnitude;
                    
                if (distance <= pointMinDistance) return;
                if (distance >= pointMaxDistance) return;
            }
            
            base.Tick(dt);
        }
        
        protected override void Spawn()
        {
            var prefabIdx = Random.Range(0, prefabs.Length);
            var prefab = prefabs[prefabIdx];
            var position = transform.position;
            var rotation = transform.rotation;
            
            var actor = SpawnService.Spawn(prefab, position, rotation);
            if (actor == null) return;
            
            SpawnedIds.Add(actor.EntityId);
            TotalSpawned++;
            DebCon.Info($"{actor.gameObject.name} spawned at position {position}", "ActorSpawnerBridge", actor.gameObject);
        }
    }
}
