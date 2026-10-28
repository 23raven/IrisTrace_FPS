namespace IrisTrace.Runtime.Core
{
    /// <summary>Optional aim-button input contract. A weapon can ignore it when no aim profile is assigned.</summary>
    public interface IAimInput
    {
        bool AimHeld { get; }
    }
}
