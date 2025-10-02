using UnityEngine;
using ECS.Bridges;

namespace DI.Services
{
    public interface IActorSpawnService
    {
        ActorBridge Spawn (GameObject prefab, Vector3 position, Quaternion rotation);
    }
}