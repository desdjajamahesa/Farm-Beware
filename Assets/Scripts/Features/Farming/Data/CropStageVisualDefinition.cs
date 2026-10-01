using System;
using UnityEngine;

namespace FeaturesFarming.Data
{
    /// <summary>
    /// Definition of a visual stage for a crop based on watered growth days threshold.
    /// </summary>
    [Serializable]
    public struct CropStageVisualDefinition
    {
        [Tooltip("Minimum number of watered days required to reach this stage.")]
        public int dayThreshold;

        [Tooltip("Prefab representing the crop at this growth stage.")]
        public GameObject stagePrefab;
    }
}
