namespace FarmBeware.Core.Runtime
{
    /// <summary>
    /// Global session state flags shared across decoupled modules (SaveSystem, Time, Cutscenes).
    /// Preserves acyclic .asmdef compilation boundaries without circular dependencies.
    /// </summary>
    public static class GameSessionState
    {
        public static bool IsPendingNewGameIntroCutscene { get; set; } = false;
    }
}
