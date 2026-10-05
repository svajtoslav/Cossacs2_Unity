// C2UnitInteractionAndCursorV238.cs
// V243: disabled cleanly.
// V238 was a minimal bridge and conflicted with the full unit port restored in V239.
// Keep a real MonoBehaviour with the original filename/class name so Unity's MonoScript importer
// does not complain about a non-MonoBehaviour/static "disabled" class.

using UnityEngine;

namespace Cossacks2Bridge.UnityAdapters.Maps
{
    [DisallowMultipleComponent]
    [AddComponentMenu("")]
    public sealed class C2UnitInteractionAndCursorV238 : MonoBehaviour
    {
        public const string Contract = "V243_DISABLED_NOOP_REPLACED_BY_C2GameplayInteractionV1_AND_C2UnitSelectionBoxV239";

        private void Awake()
        {
            enabled = false;
        }
    }
}
