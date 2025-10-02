using System;
using System.Collections.Generic;
using EventSystem;

namespace DI.Services
{
    public interface IEventBusService
    {
        Dictionary<Type, List<EventBusService.SubscriberInfo>> GetSubscriptions();
        List<SerializableEvent> GetEventHistory();
        void ClearHistory();
        
        void Subscribe<T>(Action<T> handler) where T : SerializableEvent;
        void Unsubscribe<T>(Action<T> handler) where T : SerializableEvent;
        void Publish<T>(T evnt) where T : SerializableEvent;
    }
}