namespace IrisTrace.Runtime.Core
{
    /// <summary>Input contract consumed by weapons, independent from keyboard/gamepad implementation.</summary>
    public interface IWeaponInput
    {
        bool FirePressed { get; }
        bool FireHeld { get; }
        bool FireReleased { get; }
        bool ReloadPressed { get; }
    }
}
