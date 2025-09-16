using UnityEngine;
using System;
using System.Collections.Generic;
using NaughtyAttributes;

namespace Data
{
    [CreateAssetMenu(fileName = "Default Tag Config", menuName = "ScriptableObjects/Global Variables/Tag Config")]
    public class TagConfig : ScriptableObject
    {
        [Serializable]
        public class TagEntry
        {
            [Tag]
            public string tag;
            public string displayName;
        }

        [SerializeField, ReorderableList] private List<TagEntry> availableTags = new();
        
        public bool TryGetTag(string displayName, out string tag)
        {
            var entry = availableTags.Find(x => x.displayName == displayName);
            if (entry != null)
            {
                tag = entry.tag;
                return true;
            }
            tag = "Default";
            return false;
        }
    }
}