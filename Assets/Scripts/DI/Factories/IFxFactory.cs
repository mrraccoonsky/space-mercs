using UnityEngine;

namespace DI.Factories
{
    public interface IFxFactory
    {
        ParticleSystem Create(GameObject prefab, Vector3 position, Quaternion rotation, Vector3 scale);
    }
}