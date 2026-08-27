using UnityEngine;

namespace FeaturesWardrobe
{
    [CreateAssetMenu(fileName = "NewOutfit", menuName = "Wardrobe/Outfit Data")]
    public class OutfitData : ScriptableObject
    {
        [Header("Identity")]
        public string outfitName;
        public Sprite icon;

        [Header("Per-Part Variants (mutually exclusive pairs)")]
        [Tooltip("Top: cloth1 (variant 0) / cloth2 (variant 1)")]
        public int topVariant = 0;

        [Tooltip("Bottom: pants1 (variant 0) / pants2 (variant 1)")]
        public int bottomVariant = 0;

        [Tooltip("Shoes: shoes1_left+right (variant 0) / shoes2_left+right (variant 1)")]
        public int shoesVariant = 0;

        [Tooltip("Hat: 0 = Unequipped (off), 1 = Equipped (hat on)")]
        public int hatVariant = 0;

        [Header("Legacy (optional, for backward compat)")]
        [Tooltip("Full-body prefab — jika diisi, dipakai sebagai fallback preset.")]
        public GameObject fullBodyPrefab;

        [Header("Optional Metadata")]
        [TextArea] public string description;

        /// <summary>Applies this outfit's part selections to the character renderers.</summary>
        public void ApplyToCharacter(GameObject character)
        {
            if (character == null) return;

            var renderers = character.GetComponentsInChildren<SkinnedMeshRenderer>(true);
            foreach (var r in renderers)
                r.enabled = false; // disable all first

            // Enable body + hair always
            foreach (var r in renderers)
            {
                if (r.name == "body" || r.name == "hair1" || r.name == "hair2")
                    r.enabled = true;
            }

            // Top
            string topName = topVariant == 0 ? "cloth1" : "cloth2";
            EnableRendererByName(renderers, topName);

            // Bottom
            string bottomName = bottomVariant == 0 ? "pants1" : "pants2";
            EnableRendererByName(renderers, bottomName);

            // Shoes (left + right pair)
            if (shoesVariant == 0)
            {
                EnableRendererByName(renderers, "shoes1_left");
                EnableRendererByName(renderers, "shoes1_right");
            }
            else
            {
                EnableRendererByName(renderers, "shoes2_left");
                EnableRendererByName(renderers, "shoes2_right");
            }

            // Hat
            if (hatVariant == 1)
                EnableRendererByName(renderers, "hat");
        }

        private void EnableRendererByName(SkinnedMeshRenderer[] renderers, string name)
        {
            foreach (var r in renderers)
                if (r.name == name)
                {
                    r.enabled = true;
                    return;
                }
        }
    }
}