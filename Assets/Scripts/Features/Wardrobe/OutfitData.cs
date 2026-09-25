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

            // Cache all transforms once — recursive search supports any
            // hierarchy depth (the previous transform.Find only checked
            // direct children, which broke after the Player prefab swap).
            Transform[] allChildren = character.GetComponentsInChildren<Transform>(true);

            // Always-on parts: body, hair.
            TogglePart(allChildren, "body", true);
            TogglePart(allChildren, "hair1", true);
            TogglePart(allChildren, "hair2", true);

            // Top — exclusive pair.
            TogglePart(allChildren, "cloth1", topVariant == 0);
            TogglePart(allChildren, "cloth2", topVariant == 1);

            // Bottom — exclusive pair.
            TogglePart(allChildren, "pants1", bottomVariant == 0);
            TogglePart(allChildren, "pants2", bottomVariant == 1);

            // Shoes — exclusive pair (left + right). The "shoes2_rigth"
            // (missing 'h') spelling on the prefab is handled by an
            // extra fallback lookup below.
            TogglePart(allChildren, "shoes1_left",  shoesVariant == 0);
            TogglePart(allChildren, "shoes1_right", shoesVariant == 0);
            TogglePart(allChildren, "shoes2_left",  shoesVariant == 1);
            if (!TogglePart(allChildren, "shoes2_right", shoesVariant == 1))
                TogglePart(allChildren, "shoes2_rigth", shoesVariant == 1);

            // Hat — driven by isHatEquipped, not the asset's hatVariant, to keep
            // the Topi button authoritative over the standalone toggle.
            // The asset's hatVariant is still persisted in save data.
        }

        // Recursive scan — finds the first Transform with matching name at any depth.
        // Returns true if a part was found and toggled, false otherwise.
        private static bool TogglePart(Transform[] allChildren, string partName, bool isActive)
        {
            for (int i = 0; i < allChildren.Length; i++)
            {
                if (allChildren[i] != null && allChildren[i].name == partName)
                {
                    allChildren[i].gameObject.SetActive(isActive);
                    return true;
                }
            }
            return false;
        }
    }
}