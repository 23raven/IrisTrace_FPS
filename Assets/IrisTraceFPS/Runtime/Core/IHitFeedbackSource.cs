using System;

namespace IrisTrace.Runtime.Core
{
    /// <summary>Event contract for UI or effects that react to a confirmed damaging hit.</summary>
    public interface IHitFeedbackSource
    {
        event Action HitConfirmed;
    }
}
