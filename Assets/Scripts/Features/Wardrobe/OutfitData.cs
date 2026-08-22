using UnityEngine;

namespace FeaturesWardrobe
{
    [CreateAssetMenu(fileName = "NewOutfit", menuName = "Wardrobe/Outfit Data")]
    public class OutfitData : ScriptableObject
    {
        [Header("Identity")]
        public string outfitName;
        public Sprite icon;
        
        [Header("Visual")]
        [Tooltip("Full-body prefab (body + clothes combined). Di-spawn ke PlayerOutfit.outfitRoot.")]
        public GameObject fullBodyPrefab;
        
        [Header("Optional Metadata")]
        [TextArea] public string description;
    }
}