using UnityEngine;

namespace FarmBeware.Core.Runtime
{
    public enum CameraMode
    {
        Gameplay,
        TrophyMode,
        WardrobeMode
    }

    /// <summary>
    /// Contract defining camera state machine management and framing commands.
    /// </summary>
    public interface ICameraService
    {
        CameraMode CurrentMode { get; }
        Camera MainCamera { get; }
        Camera TrophyCamera { get; }
        Camera WardrobeCamera { get; }
        void SetMode(CameraMode mode, Transform contextRoot = null);
        void PositionPlayerBehindTrophyCamera();
    }
}
