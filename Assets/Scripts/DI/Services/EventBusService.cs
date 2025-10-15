using System;
using System.Collections.Generic;
using System.Linq;
using EventSystem;
using UnityEngine;

namespace DI.Services
{
    [CreateAssetMenu(fileName = "EventBusService", menuName = "SO/Game/EventBusService")]
    public class EventBusService : ScriptableObject, IEventBusService
    {
        [Serializable]
        public class SubscriberInfo
        {
            public string methodName;
            public string targetType;
            public string targetName;
        }
        
        private class SubscriptionInfo
        {
            public string EventType;
            public string MethodName;
            public int SubscriberCount;
        }
        
        [SerializeField] private int maxHistorySize = 100;
        
        private readonly Dictionary<Type, List<Delegate>> _subscribers = new();
        private readonly List<SubscriptionInfo> _subscriptionInfos = new();
        private readonly List<SerializableEvent> _eventHistory = new();
        
        public static event Action OnEventHistoryChanged;
        
        public Dictionary<Type, List<SubscriberInfo>> GetSubscriptions()
        {
            return _subscribers.ToDictionary(
                kvp => kvp.Key,
                kvp => kvp.Value.Select(d => new SubscriberInfo
                {
                    methodName = d.Method.Name,
                    targetType = d.Target?.GetType().Name ?? "Static",
                    targetName = GetTargetName(d.Target)
                }).ToList()
            );
        }
        
        public List<SerializableEvent> GetEventHistory() => new(_eventHistory);
        
        public void ClearHistory()
        {
            _eventHistory.Clear();
        }
        
        public void Subscribe<T>(Action<T> handler) where T : SerializableEvent
        {
            var eventType = typeof(T);
            
            if (!_subscribers.ContainsKey(eventType))
            {
                _subscribers[eventType] = new List<Delegate>();
            }
            
            if (!_subscribers[eventType].Contains(handler))
            {
                _subscribers[eventType].Add(handler);
                UpdateSubscriptionInfo(eventType, handler, true);
            }
        }

        public void Unsubscribe<T>(Action<T> handler) where T : SerializableEvent
        {
            var eventType = typeof(T);
            if (!_subscribers.TryGetValue(eventType, out var subscriber)) return;
            
            subscriber.Remove(handler);
            UpdateSubscriptionInfo(eventType, handler, false);
        }

        public void Publish<T>(T evnt) where T : SerializableEvent
        {
            // add to history
            _eventHistory.Add(evnt);
            if (_eventHistory.Count > maxHistorySize)
            {
                _eventHistory.RemoveAt(0);
            }

            OnEventHistoryChanged?.Invoke();
            
            // notify subscribers
            var eventType = typeof(T);
            if (!_subscribers.TryGetValue(eventType, out var subscriber)) return;
            
            foreach (var handler in subscriber.ToList())
            {
                try
                {
                    (handler as Action<T>)?.Invoke(evnt);
                }
                catch (Exception e)
                {
                    Debug.LogError($"Error invoking event handler: {e.Message}");
                }
            }
        }

        private string GetTargetName(object target)
        {
            return target switch
            {
                null => "Static",
                MonoBehaviour mono => $"{mono.name}",
                ScriptableObject so => $"{so.name}",
                _ => target.GetType().Name
            };
            
            /* return target switch
            {
                null => "Static",
                MonoBehaviour mono => $"{mono.GetType().Name} ({mono.name})",
                ScriptableObject so => $"{so.GetType().Name} ({so.name})",
                _ => target.GetType().Name
            }; */
        }
        
        private void UpdateSubscriptionInfo(Type eventType, Delegate handler, bool isSubscribing)
        {
            var methodName = handler.Method.Name;
            var subscriptionInfo = _subscriptionInfos.FirstOrDefault(s => 
                s.EventType == eventType.Name && s.MethodName == methodName);
            
            if (isSubscribing)
            {
                if (subscriptionInfo == null)
                {
                    _subscriptionInfos.Add(new SubscriptionInfo
                    {
                        EventType = eventType.Name,
                        MethodName = methodName,
                        SubscriberCount = 1
                    });
                }
                else
                {
                    subscriptionInfo.SubscriberCount++;
                }
            }
            else
            {
                if (subscriptionInfo == null) return;
                
                subscriptionInfo.SubscriberCount--;
                if (subscriptionInfo.SubscriberCount <= 0)
                {
                    _subscriptionInfos.Remove(subscriptionInfo);
                }
            }
        }
    }
}