using UnityEngine;
using NaughtyAttributes;

namespace Data
{
    [CreateAssetMenu(fileName = "Default Global Variables Config", menuName = "ScriptableObjects/Global Variables/Global Variable Config")]
    public class GlobalVariablesConfig : ScriptableObject
    {
        [BoxGroup("Tags:")]
        [SerializeField, Required] private TagConfig tagConfig;
        
        public TagConfig TagConfig => tagConfig;
    }
}