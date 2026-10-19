namespace IrisTrace.Runtime.Core
{
    /// <summary>Contract for built-in and custom character abilities.</summary>
    public interface ICharacterAbility
    {
        string AbilityName { get; }
        float CooldownRemaining { get; }
        bool TryActivate();
    }
}
