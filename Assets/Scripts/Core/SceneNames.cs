using System;

namespace FarmBeware.Core.Runtime
{
    /// <summary>
    /// Canonical scene name constants and helpers to eliminate hardcoded scene string checks.
    /// </summary>
    public static class SceneNames
    {
        public const string MainMenuScene = "MainMenuScene";
        public const string MainScene = "MainScene";
        public const string StagingScene = "StagingScene";
        public const string StorylinePrototypeSandbox = "StorylinePrototypeSandbox";

        public static bool IsGameplayScene(string sceneName)
        {
            if (string.IsNullOrEmpty(sceneName)) return false;
            return string.Equals(sceneName, MainScene, StringComparison.Ordinal) ||
                   string.Equals(sceneName, StagingScene, StringComparison.Ordinal);
        }
    }
}
