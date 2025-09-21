using UnityEngine;

namespace DI.Services
{
    public interface IFXService
    {
        void Spawn(GameObject prefab, Vector3 position, Quaternion rotation);
    }
}
