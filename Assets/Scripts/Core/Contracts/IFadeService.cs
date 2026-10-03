using System;

namespace FarmBeware.Core.Runtime
{
    /// <summary>
    /// Contract defining screen fade transition commands.
    /// </summary>
    public interface IFadeService
    {
        void FadeOut(float duration, Action onComplete = null);
        void FadeIn(float duration, Action onComplete = null);
    }
}
