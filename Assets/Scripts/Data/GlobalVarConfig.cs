using UnityEngine;
using NaughtyAttributes;

namespace Data
{
    [CreateAssetMenu(fileName = "GlobalVarsConfig", menuName = "SO/Game/GlobalVarsConfig")]
    public class GlobalVarsConfig : ScriptableObject
    {
        [BoxGroup("Tags:")]
        [SerializeField, Required] private TagConfig tagConfig;
        
        public TagConfig TagConfig => tagConfig;
    }
}