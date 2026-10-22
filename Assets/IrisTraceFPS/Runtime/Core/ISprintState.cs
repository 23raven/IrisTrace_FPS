namespace IrisTrace.Runtime.Core
{
    /// <summary>Optional movement modifier contract; implemented by the equipped Sprint ability.</summary>
    public interface ISprintState
    {
        bool IsSprinting { get; }
    }
}
