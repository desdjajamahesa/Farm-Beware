namespace FarmBeware.Core.Runtime
{
    /// <summary>
    /// Explicit discrete states for Place -> Refill -> Collect / Cancel station lifecycles.
    /// </summary>
    public enum RefillStationState
    {
        Empty = 0,
        Refilling = 1,
        ReadyToCollect = 2
    }

    /// <summary>
    /// Pure C# Zero-GC state machine governing Place -> Refill -> Collect / Cancel station lifecycles.
    /// Eliminates impossible 3-boolean combinations across KitchenSinkInteractable and WaterWellInteractable.
    /// </summary>
    public struct RefillStationStateMachine
    {
        public RefillStationState State { get; private set; }
        public float Progress01 { get; private set; }

        public bool HasItem => State != RefillStationState.Empty;
        public bool IsRefilling => State == RefillStationState.Refilling;
        public bool IsReadyToCollect => State == RefillStationState.ReadyToCollect;

        public bool PlaceItem()
        {
            if (State != RefillStationState.Empty)
            {
                return false;
            }

            State = RefillStationState.Refilling;
            Progress01 = 0f;
            return true;
        }

        /// <summary>
        /// Ticks refill progress and returns true on the exact step it transitions from Refilling to ReadyToCollect.
        /// </summary>
        public bool Tick(float deltaTime, float duration)
        {
            if (State != RefillStationState.Refilling)
            {
                return false;
            }

            float safeDuration = duration > 0.01f ? duration : 0.01f;
            Progress01 += deltaTime / safeDuration;
            if (Progress01 >= 1f)
            {
                Progress01 = 1f;
                State = RefillStationState.ReadyToCollect;
                return true;
            }

            return false;
        }

        public void CompleteImmediate()
        {
            Progress01 = 1f;
            State = RefillStationState.ReadyToCollect;
        }

        public void ResetToEmpty()
        {
            State = RefillStationState.Empty;
            Progress01 = 0f;
        }

        /// <summary>
        /// Restores station state from persisted save data and normalizes contradictory flag combinations.
        /// </summary>
        public void Restore(bool hasItem, bool ready, float progress01)
        {
            if (!hasItem)
            {
                ResetToEmpty();
                return;
            }

            float clamped = progress01 < 0f ? 0f : (progress01 > 1f ? 1f : progress01);
            if (ready || clamped >= 1f)
            {
                Progress01 = 1f;
                State = RefillStationState.ReadyToCollect;
            }
            else
            {
                Progress01 = clamped;
                State = RefillStationState.Refilling;
            }
        }
    }
}
