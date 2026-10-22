namespace IrisTrace.Runtime.Core
{
    /// <summary>Contract for hold-to-activate abilities such as sprint.</summary>
    public interface IHoldCharacterAbility : ICharacterAbility
    {
        bool IsActive { get; }
        void SetInputHeld(bool held);
    }
}
