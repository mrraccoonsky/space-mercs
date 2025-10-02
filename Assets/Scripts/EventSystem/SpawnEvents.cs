using System;
using UnityEngine;

namespace EventSystem
{
    [Serializable]
    public class ActorSpawnedEvent : SerializableEvent
    {
        public int EntityId { get; private set; }
        public string PrefabName { get; private set; }
        public Vector3 Position { get; private set; }
        
        public ActorSpawnedEvent(int entityId, string prefabName, Vector3 position)
        {
            EntityId = entityId;
            PrefabName = prefabName;
            Position = position;
        }
    }

    [Serializable]
    public class ActorDestroyedEvent : SerializableEvent
    {
        public int EntityId { get; private set; }
        public string Reason { get; private set; }
        
        public ActorDestroyedEvent(int entityId, string reason = "Unknown")
        {
            EntityId = entityId;
            Reason = reason;
        }
    }
}