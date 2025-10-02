using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using EventSystem;

namespace DI.Services.Editor
{
    [CustomEditor(typeof(EventBusService))]
    public class EventBusServiceEditor : UnityEditor.Editor
    {
        private readonly Dictionary<string, Color> _colorCache = new();
        
        private Vector2 _subscribersScrollPosition;
        private Vector2 _historyScrollPosition;
        
        private bool _showSubscribers = true;
        private bool _showHistory = true;
        
        private void OnEnable()
        {
            EventBusService.OnEventHistoryChanged += HandleEventHistoryChanged;
        }
        
        private void OnDisable()
        {
            EventBusService.OnEventHistoryChanged -= HandleEventHistoryChanged;
        }
        
        private void HandleEventHistoryChanged()
        {
            Repaint();
        }

        public override void OnInspectorGUI()
        {
            var eventBus = (EventBusService)target;
            
            // subscribers section
            _showSubscribers = EditorGUILayout.Foldout(_showSubscribers, "Subscribers");
            if (_showSubscribers)
            {
                var subs = eventBus.GetSubscriptions();
                if (subs.Count == 0)
                {
                    EditorGUILayout.LabelField("No active subscriptions");
                }
                else
                {
                    _subscribersScrollPosition = EditorGUILayout.BeginScrollView(_subscribersScrollPosition, GUILayout.Height(300));
                    
                    foreach (var sub in subs)
                    {
                        // different background color for each event type
                        var eventTypeColor = GetColor(sub.Key.Name, 0.2f);
                        var boxStyle = new GUIStyle("box") { margin = new RectOffset(0, 0, 1, 1) };
                        var originalColor = GUI.backgroundColor;
                        GUI.backgroundColor = eventTypeColor;
                        
                        EditorGUILayout.BeginVertical(boxStyle);
                        GUI.backgroundColor = originalColor;
                        
                        // event type header
                        var headerStyle = new GUIStyle(EditorStyles.boldLabel) { normal = { textColor = Color.white } };
                        EditorGUILayout.LabelField($"Event: {sub.Key.Name}", headerStyle);
                        
                        // separator
                        EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
                        
                        foreach (var subscriber in sub.Value)
                        {
                            EditorGUILayout.BeginHorizontal();
                            
                            // method name
                            var methodColor = GetColor(subscriber.methodName);
                            var methodStyle = new GUIStyle(EditorStyles.boldLabel) { normal = { textColor = methodColor }, fontSize = 11 };
                            EditorGUILayout.LabelField($"•", GUILayout.Width(10));
                            EditorGUILayout.LabelField(subscriber.methodName, methodStyle, GUILayout.Width(140));
                            
                            // target type
                            var typeColor = GetColor(subscriber.targetType);
                            var typeStyle = new GUIStyle(EditorStyles.miniLabel) { normal = { textColor = typeColor } };
                            EditorGUILayout.LabelField($"[{subscriber.targetType}]", typeStyle, GUILayout.Width(140));
                            
                            // target name
                            var nameStyle = new GUIStyle(EditorStyles.label) { fontSize = 11 };
                            EditorGUILayout.LabelField(subscriber.targetName, nameStyle);
                            
                            EditorGUILayout.EndHorizontal();
                        }
                        
                        EditorGUILayout.EndVertical();
                    }
                    
                    EditorGUILayout.EndScrollView();
                }
            }
            
            EditorGUILayout.Space();
            
            // history section
            _showHistory = EditorGUILayout.Foldout(_showHistory, $"Event History ({eventBus.GetEventHistory().Count})");
            if (_showHistory)
            {
                var history = eventBus.GetEventHistory();
                if (history.Count == 0)
                {
                    EditorGUILayout.LabelField("No events in history");
                }
                else
                {
                    _historyScrollPosition = EditorGUILayout.BeginScrollView(_historyScrollPosition, GUILayout.Height(200));
                    
                    foreach (var e in history.AsEnumerable().Reverse())
                    {
                        // different background color for each event type
                        var eventTypeColor = GetColor(e.EventType, 0.2f);
                        var boxStyle = new GUIStyle("box") { margin = new RectOffset(0, 0, 1, 1) };
                        var originalColor = GUI.backgroundColor;
                        GUI.backgroundColor = eventTypeColor;
                        
                        EditorGUILayout.BeginVertical(boxStyle);
                        GUI.backgroundColor = originalColor;
                        
                        EditorGUILayout.BeginHorizontal();
                        
                        // event type
                        var typeColor = GetColor(e.EventType);
                        var typeStyle = new GUIStyle(EditorStyles.boldLabel) { normal = { textColor = typeColor } };
                        EditorGUILayout.LabelField(e.EventType, typeStyle, GUILayout.Width(150));
                        
                        // caller info
                        var callerColor = GetColor(e.CallerInfo);
                        var callerStyle = new GUIStyle(EditorStyles.miniLabel) { normal = { textColor = callerColor } };
                        EditorGUILayout.LabelField(e.CallerInfo, callerStyle);
                        
                        EditorGUILayout.EndHorizontal();
                        
                        // event details
                        var detailStyle = new GUIStyle(EditorStyles.label) { fontSize = 11 };
                        if (e is ActorSpawnedEvent actorSpawned)
                        {
                            EditorGUILayout.LabelField($"Entity {actorSpawned.EntityId} at {actorSpawned.Position}", detailStyle);
                        }
                        else if (e is ActorDestroyedEvent actorDestroyed)
                        {
                            EditorGUILayout.LabelField($"Entity {actorDestroyed.EntityId} ({actorDestroyed.Reason})", detailStyle);
                        }
                        
                        EditorGUILayout.EndVertical();
                    }
                    
                    EditorGUILayout.EndScrollView();
                }
            }
            
            EditorGUILayout.Space();
            
            // buttons
            EditorGUILayout.BeginHorizontal();
            
            if (GUILayout.Button("Clear Event History", GUILayout.Height(30)))
            {
                eventBus.ClearHistory();
            }
            
            if (GUILayout.Button("Refresh Subscriptions", GUILayout.Height(30)))
            {
                Repaint();
            }
            
            EditorGUILayout.EndHorizontal();
        }
        
        private Color GetColor(string text, float alpha = 1.0f)
        {
            var cacheKey = $"{text}_{alpha}";
            if (_colorCache.TryGetValue(cacheKey, out var cachedColor))
            {
                return cachedColor;
            }

            if (string.IsNullOrEmpty(text))
            {
                return Color.gray;
            }
                
            var hash = text.GetHashCode();
            var r = Math.Max(0.5f, Math.Abs(hash % 1000) / 1000f);
            var g = Math.Max(0.5f, Math.Abs((hash >> 8) % 1000) / 1000f);
            var b = Math.Max(0.5f, Math.Abs((hash >> 16) % 1000) / 1000f);
            
            var color = new Color(r, g, b, alpha);
            _colorCache[cacheKey] = color;
            return color;
        }
    }
}