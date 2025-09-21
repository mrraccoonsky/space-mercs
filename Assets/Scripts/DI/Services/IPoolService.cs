using UnityEngine;

namespace DI.Services
{
    public interface IPoolService
    {
        T Get<T>(GameObject prefab) where T : Component;
        void Return(GameObject prefab, GameObject item);
        void Clear();
    }
}