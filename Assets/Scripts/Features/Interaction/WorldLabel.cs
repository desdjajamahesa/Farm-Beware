using UnityEngine;

namespace FeaturesInteraction
{
    /// <summary>
    /// Label nama untuk objek interaktif di dunia.
    /// Murni data (displayName); kosong = fallback ke nama GameObject.
    /// Dibaca oleh HoverLabelController untuk menampilkan tooltip ala hotbar.
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