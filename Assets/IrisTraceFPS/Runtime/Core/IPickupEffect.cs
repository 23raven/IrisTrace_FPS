using UnityEngine;

namespace IrisTrace.Runtime.Core
{
    /// <summary>Implement this interface on a pickup component to add a custom pickup effect.</summary>
    public interface IPickupEffect
    {
        bool TryApply(GameObject receiver, float amount);
    }
}
