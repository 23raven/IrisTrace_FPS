namespace IrisTrace.Runtime.Core
{
    /// <summary>Read-only health telemetry exposed to UI and other modules.</summary>
    public interface IHealthStatus
    {
        float CurrentHealth { get; }
        float MaxHealth { get; }
        bool IsDead { get; }
    }
}
