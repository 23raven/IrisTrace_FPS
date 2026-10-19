namespace IrisTrace.Runtime.Core
{
    /// <summary>Optional healing capability, separate from the read-only health contract.</summary>
    public interface IHealable : IHealthStatus
    {
        void Heal(float amount);
    }
}
