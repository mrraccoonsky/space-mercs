using System;
using System.Collections.Generic;
using UnityEngine;

namespace Data
{
    using NaughtyAttributes;
    
    public enum GlobalTag
    {
        Default = 0,
        Player = 1,
        Enemy = 2,
    }
    
    [CreateAssetMenu(fileName = "TagConfig", menuName = "SO/Game/TagConfig")]
    public class TagConfig : ScriptableObject
    {
        [Serializable]
        public class TagEntry
        {
            public GlobalTag globalTag;
            [Tag] public string editorTag;
            public string displayTag;
        }

        [SerializeField, ReorderableList] private List<TagEntry> availableTags = new();

        public bool TryGetEditorTag(GlobalTag globalGlobalTag, out string editorTag)
        {
            var entry = availableTags.Find(x => x.globalTag == globalGlobalTag);
            if (entry != null)
            {
                editorTag = entry.editorTag;
                return true;
            }
            editorTag = "Default";
            return false;
        }
        
        public string GetDisplayTag(GlobalTag globalGlobalTag)
        {
            var entry = availableTags.Find(x => x.globalTag == globalGlobalTag);
            
            return entry != null 
                ? entry.displayTag
                : string.Empty;
        }
    }
}