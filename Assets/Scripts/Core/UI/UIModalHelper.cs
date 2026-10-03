namespace FarmBeware.Core.Runtime
{
    /// <summary>
    /// Global modal tracking helper to synchronize modal closing and prevent input overlap.
    /// </summary>
    public static class UIModalHelper
    {
        public static int LastFrameUIPanelClosed = -1;
        public static bool IsSaveUIOpen = false;
    }
}
