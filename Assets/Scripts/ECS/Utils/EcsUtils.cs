using Leopotam.EcsLite;

namespace ECS.Utils
{
    static class EcsUtils
    {
        public static bool HasCompInPool<T>(EcsWorld world, int entityId, out EcsPool<T> pool) where T : struct
        {
            pool = world.GetPool<T>();
            return pool.Has(entityId);
        }
    }
}