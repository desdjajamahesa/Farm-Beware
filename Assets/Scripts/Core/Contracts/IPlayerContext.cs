using UnityEngine;

namespace FarmBeware.Core.Runtime
{
    /// <summary>
    /// Centralized contract exposing player capabilities and state without coupling to concrete PlayerControl.
    /// </summary>
    public interface IPlayerContext
    {
        Transform Transform { get; }
        bool IsInputLocked { get; set; }
        void PlayAnimation(string triggerName);
        T GetPlayerComponent<T>() where T : class;
    }
}
