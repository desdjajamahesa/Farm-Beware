using UnityEngine;

namespace FeaturesInteraction
{
    /// <summary>
    /// Name label for interactive objects in the world.
    /// Pure data (displayName); empty = fallback to GameObject name.
    /// Read by HoverLabelController to display a hotbar-style tooltip.
    /// </summary>
    public class WorldLabel : MonoBehaviour
    {
        [Tooltip("Nama ramah yang ditampilkan saat hover (kosong = nama GameObject).")]
        public string displayName;

        public string GetDisplayName()
        {
            if (!string.IsNullOrEmpty(displayName))
                return displayName;
            return gameObject != null ? gameObject.name : "";
        }
    }
}