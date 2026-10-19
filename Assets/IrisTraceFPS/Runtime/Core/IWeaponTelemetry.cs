namespace IrisTrace.Runtime.Core
{
    /// <summary>Read-only weapon state for HUD and feedback systems.</summary>
    public interface IWeaponTelemetry
    {
        int CurrentAmmoInMagazine { get; }
        int ReserveAmmo { get; }
        bool IsReloading { get; }
        string WeaponDisplayName { get; }
    }
}
