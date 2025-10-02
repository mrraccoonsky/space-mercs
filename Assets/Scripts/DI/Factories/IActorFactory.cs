using UnityEngine;
using ECS.Bridges;

namespace DI.Factories
{
    public interface IActorFactory
    {
        ActorBridge Create(GameObject prefab, Vector3 position, Quaternion rotation);
    }
}