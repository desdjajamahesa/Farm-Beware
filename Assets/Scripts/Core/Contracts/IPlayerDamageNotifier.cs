using System;

namespace FarmBeware.Core.Runtime
{
    /// <summary>
    /// Contract exposing player damage notifications without coupling to concrete PlayerStats.
    /// </summary>
    public interface IPlayerDamageNotifier
    {
        event Action<int> OnDamageTaken;
    }
}
