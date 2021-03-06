#if UNITY_2019_1_OR_NEWER
using UnityEditor;
using UnityAtoms.Editor;

namespace UnityAtoms.WitchOS.Editor
{
    /// <summary>
    /// Event property drawer of type `Order`. Inherits from `AtomDrawer&lt;OrderEvent&gt;`. Only availble in `UNITY_2019_1_OR_NEWER`.
    /// </summary>
    [CustomPropertyDrawer(typeof(OrderEvent))]
    public class OrderEventDrawer : AtomDrawer<OrderEvent> { }
}
#endif
