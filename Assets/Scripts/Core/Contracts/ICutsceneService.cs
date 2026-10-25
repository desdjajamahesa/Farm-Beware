using System;

namespace FarmBeware.Core.Runtime
{
    /// <summary>
    /// Contract defining cutscene and cinematic timeline orchestration in Core.Runtime.
    /// Provides lifecycle hooks and input locking governance for cinematic sequences.
    /// </summary>
    public interface ICutsceneService
    {
        /// <summary>Whether a cutscene is currently playing.</summary>
        bool IsCutscenePlaying { get; }

        /// <summary>Plays the initial New Game opening timeline cutscene.</summary>
        void PlayOpeningCutscene(Action onComplete = null);

        /// <summary>Skips the active cutscene immediately, restoring player control and camera framing.</summary>
        void SkipCurrentCutscene();

        /// <summary>Event raised when any cutscene begins playback.</summary>
        event Action OnCutsceneStarted;

        /// <summary>Event raised when any cutscene completes or is skipped.</summary>
        event Action OnCutsceneEnded;
    }
}
