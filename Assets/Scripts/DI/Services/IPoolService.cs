using UnityEngine;

namespace DI.Services
{
    public interface IPoolService
    {
        void Init(GameObject prefab, int count);
        T Get<T>(GameObject prefab) where T : Component;
        void Return(GameObject prefab, GameObject item);
        void Clear();
    }
}