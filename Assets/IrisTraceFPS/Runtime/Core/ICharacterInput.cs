using UnityEngine;

namespace IrisTrace.Runtime.Core
{
    /// <summary>Input contract consumed by character movement, not tied to a specific input package.</summary>
    public interface ICharacterInput
    {
        Vector2 Move { get; }
        Vector2 LookDelta { get; }
        bool JumpPressed { get; }
        bool CrouchHeld { get; }
        bool CancelPressed { get; }
    }
}
