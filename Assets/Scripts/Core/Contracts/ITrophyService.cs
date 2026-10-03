namespace FarmBeware.Core.Runtime
{
    /// <summary>
    /// Contract defining access to trophy interaction state.
    /// </summary>
    public interface ITrophyService
    {
        bool IsInTrophyMode { get; }
    }
}
