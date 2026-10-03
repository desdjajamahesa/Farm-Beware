namespace FarmBeware.Core.Runtime
{
    /// <summary>
    /// Contract for bed interactions that need to update their visual cues or labels based on night brawl state.
    /// </summary>
    public interface IBedInteractable
    {
        void UpdateLabel();
    }
}
