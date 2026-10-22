namespace IrisTrace.Runtime.Core
{
    /// <summary>Optional input contract for abilities which remain active while their binding is held.</summary>
    public interface IAbilityHoldInput
    {
        bool Ability1Held { get; }
        bool Ability2Held { get; }
    }
}
