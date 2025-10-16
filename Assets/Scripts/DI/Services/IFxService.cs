using UnityEngine;

namespace DI.Services
{
    public interface IFxService
    {
        void Spawn(GameObject prefab, Vector3 position, Quaternion rotation, Vector3 scale);
    }
}
