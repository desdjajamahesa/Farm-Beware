using UnityEngine;

namespace FeaturesWardrobe
{
    [DisallowMultipleComponent]
    public class OutfitMeshSwapper : MonoBehaviour
    {
        [Header("Character Root")]
        [SerializeField] private GameObject characterRoot;

        [Header("Hat")]
        [SerializeField] private SkinnedMeshRenderer hatMesh;

        private SkinnedMeshRenderer[] cachedRenderers;

        private void Awake()
        {
            if (characterRoot == null)
                characterRoot = GameObject.Find("Player/character");

            if (characterRoot != null)
                cachedRenderers = characterRoot.GetComponentsInChildren<SkinnedMeshRenderer>(true);

            if (hatMesh == null && characterRoot != null)
            {
                foreach (var r in cachedRenderers)
                {
                    if (r != null && r.gameObject.name == "hat")
                    {
                        hatMesh = r;
                        break;
                    }
                }
            }
        }

        public void ApplyOutfit(OutfitData outfit)
        {
            if (outfit == null || characterRoot == null) return;
            if (cachedRenderers == null)
                cachedRenderers = characterRoot.GetComponentsInChildren<SkinnedMeshRenderer>(true);
            outfit.ApplyToCharacter(characterRoot);
        }

        public void SetHatState(bool isEquipped)
        {
            if (hatMesh != null)
            {
                hatMesh.gameObject.SetActive(isEquipped);
                hatMesh.enabled = isEquipped;
            }
        }
    }
}
