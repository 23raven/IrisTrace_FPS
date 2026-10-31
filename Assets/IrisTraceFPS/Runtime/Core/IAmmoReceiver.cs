namespace IrisTrace.Runtime.Core
{
    /// <summary>Optional capability for components that can receive reserve ammunition.</summary>
    public interface IAmmoReceiver
    {
        /// <summary>Returns the amount of reserve ammunition actually added.</summary>
        int AddReserveAmmo(int amount);
    }
}
