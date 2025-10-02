using System;
using System.Diagnostics;
using UnityEngine;

namespace EventSystem
{
    [Serializable]
    public abstract class SerializableEvent
    {
        [SerializeField] private string eventType;
        [SerializeField] private string callerInfo;
        
        public string EventType => string.IsNullOrEmpty(eventType) ? GetType().Name : eventType;
        public string CallerInfo => callerInfo;
        
        protected SerializableEvent()
        {
            eventType = GetType().Name;
            callerInfo = GetCallerInfo();
        }

        protected SerializableEvent(string customTypeName)
        {
            eventType = customTypeName;
            callerInfo = GetCallerInfo();
        }
        
        private string GetCallerInfo()
        {
            try
            {
                var stackTrace = new StackTrace(3, true); // skip 3 frames to get past the event constructor and publisher
                var frame = stackTrace.GetFrame(0);
                if (frame != null)
                {
                    var method = frame.GetMethod();
                    var className = method?.DeclaringType?.Name ?? "Unknown";
                    var methodName = method?.Name ?? "Unknown";
                    return $"{className}.{methodName}";
                }
            }
            catch (Exception)
            {
                // ...
            }
            
            return "Unknown";
        }
    }
}