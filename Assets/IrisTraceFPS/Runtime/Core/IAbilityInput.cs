namespace IrisTrace.Runtime.Core
{
    /// <summary>Input contract for the two configurable character ability slots.</summary>
    public interface IAbilityInput
    {
        bool Ability1Pressed { get; }
        bool Ability2Pressed { get; }
    }
}
