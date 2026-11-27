namespace FarmBeware.Core.Runtime
{
    /// <summary>
    /// Contract for HUD presenters (such as vehicle driving telemetry) that must snap hidden immediately during cutscenes.
    /// </summary>
    public interface ICutsceneSuppressibleHud
    {
        void SnapHidden();
    }
}
