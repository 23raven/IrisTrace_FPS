namespace IrisTrace.Runtime.Core
{
    /// <summary>Optional aim state exposed to systems such as mouse-look sensitivity.</summary>
    public interface IAimState
    {
        bool IsAiming { get; }
        float LookSensitivityMultiplier { get; }
    }
}
