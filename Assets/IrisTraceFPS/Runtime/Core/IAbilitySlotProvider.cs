namespace IrisTrace.Runtime.Core
{
    /// <summary>
    /// Exposes configured ability slots and their bindings without requiring UI or other systems
    /// to reference the concrete ability input handler.
    /// </summary>
    public interface IAbilitySlotProvider
    {
        ICharacterAbility AbilitySlot1 { get; }
        ICharacterAbility AbilitySlot2 { get; }
        string AbilitySlot1Binding { get; }
        string AbilitySlot2Binding { get; }
    }
}
