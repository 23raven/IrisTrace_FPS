using UnityEngine;

namespace IrisTrace.Runtime.Core
{
    /// <summary>Marks an object that simple AI can locate and aim at without depending on a concrete hero class.</summary>
    public interface ICombatTarget
    {
        Transform AimPoint { get; }
    }
}
