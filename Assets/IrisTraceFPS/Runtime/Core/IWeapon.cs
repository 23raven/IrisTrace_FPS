namespace IrisTrace.Runtime.Core
{
    /// <summary>Contract consumed by input and UI modules without depending on a concrete weapon.</summary>
    public interface IWeapon
    {
        int CurrentAmmoInMagazine { get; }
        int ReserveAmmo { get; }
        bool IsReloading { get; }
        void BeginFire();
        void EndFire();
        void Reload();
    }
}
