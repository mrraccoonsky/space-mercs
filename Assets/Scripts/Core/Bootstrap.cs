using UnityEngine;
using Tools;

namespace Core
{
    public class Bootstrap : MonoBehaviour
    {
        [SerializeField] private ConsoleVerbosity consoleVerbosity = ConsoleVerbosity.Verbose;

        private void Awake()
        {
            DebCon.Verbosity = consoleVerbosity;
        }
    }
}