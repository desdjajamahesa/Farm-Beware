namespace FarmBeware.Core.Runtime
{
    /// <summary>
    /// Decoupled contract for restoring player vitals (HP and hydration) without SendMessage.
    /// </summary>
    public interface IPlayerVitalsAction
    {
        void Heal(int amount);
        void Eat(float amount);
        void Drink(float amount);
    }

    /// <summary>
    /// Decoupled contract for ensuring the player starter kit is populated for a given day.
    /// </summary>
    public interface IStarterKitProvider
    {
        void EnsureStarterKit(int currentDay);
    }

    /// <summary>
    /// Decoupled contract for refreshing character stats UI displays.
    /// </summary>
    public interface IStatsDisplayRefreshable
    {
        void UpdateAllStats();
    }

    /// <summary>
    /// Decoupled contract for dismissing active narrative dialogue overlays.
    /// </summary>
    public interface IDialogueDismissible
    {
        void DismissDialogue();
    }
}
