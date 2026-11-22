using System;
using System.Collections.Generic;
using UnityEngine;

namespace FarmBeware.Core.Runtime
{
    [Serializable]
    public readonly struct BuffSnapshotData
    {
        public readonly string BuffName;
        public readonly int BuffType;
        public readonly float Value;
        public readonly float Duration;
        public readonly float RemainingDuration;
        public readonly string Description;

        public BuffSnapshotData(string buffName, int buffType, float value, float duration, float remainingDuration, string description)
        {
            BuffName = buffName;
            BuffType = buffType;
            Value = value;
            Duration = duration;
            RemainingDuration = remainingDuration;
            Description = description;
        }
    }

    /// <summary>
    /// Decoupled contract for querying, clearing, and restoring runtime player food buffs across save/load boundaries.
    /// </summary>
    public interface IBuffStateService
    {
        void ClearAllBuffs();
        void ExportBuffSnapshots(List<BuffSnapshotData> output);
        void RestoreFromBuffSnapshots(IReadOnlyList<BuffSnapshotData> snapshots);
    }

    /// <summary>
    /// Centralized contract exposing player capabilities and state without coupling to concrete PlayerControl.
    /// </summary>
    public interface IPlayerContext
    {
        Transform Transform { get; }
        bool IsInputLocked { get; set; }
        void PlayAnimation(string triggerName);
        void TriggerPlantAnimation();
        void TriggerWaterAnimation();
        void TriggerHarvestAnimation();
        void CancelFarmAction();
        void ApplyKnockback(Vector3 direction, float force);
        void ApplyStun(float duration);
        void SetControlLock(bool locked);
        void AddInputLock(string source);
        void RemoveInputLock(string source);
        void ClearAllInputLocks();
        bool HasInputLock(string source);
        bool HasLockOtherThan(string excludedSource);
        bool IsGodMode { get; }
        void StopMovement();
        void Teleport(Vector3 position, Quaternion rotation);
        int Health { get; }
        float Stamina { get; }
        float Hunger { get; }
        float Thirst { get; }
        void RestoreStats(int health, float stamina, float hunger, float thirst);
        void UpdateEquipmentVisual(int slotIndex);
        T GetPlayerComponent<T>() where T : class;
    }
}
