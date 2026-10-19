namespace IrisTrace.Runtime.Core
{
    /// <summary>Contract for any runtime object that can receive damage.</summary>
    public interface IDamageable
    {
        void TakeDamage(float amount);
    }
}
