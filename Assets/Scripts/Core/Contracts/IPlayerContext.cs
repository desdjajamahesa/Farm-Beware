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
        void TriggerPlantAnimation();
        void TriggerHarvestAnimation();
        void ApplyKnockback(Vector3 direction, float force);
        void ApplyStun(float duration);
        void SetControlLock(bool locked);
        bool IsGodMode { get; }
        T GetPlayerComponent<T>() where T : class;
    }
}
