using System.Collections.Generic;
using UnityEngine;

namespace FeaturesCamera
{
    /// <summary>
    /// Groups multiple modular wall segments into a single cohesive wall side (e.g. Kitchen_South).
    /// When any segment in this group occludes the camera, all segments fade transparent together.
    /// </summary>
    [DisallowMultipleComponent]
    public class WallOcclusionGroup : MonoBehaviour
    {
        [Header("Group Identity")]
        [Tooltip("Human-readable wall side name (e.g. 'Kitchen_South', 'Bedroom_West').")]
        public string groupName = "WallGroup";

        [Header("Group Members")]
        [Tooltip("All modular wall segments and corners belonging to this wall side.")]
        public List<WallOccluder> occluders = new List<WallOccluder>();

        private void Awake()
        {
            RegisterMembers();
        }

        private void OnEnable()
        {
            RegisterMembers();
        }

        public void RegisterMembers()
        {
            if (occluders == null) return;
            for (int i = 0; i < occluders.Count; i++)
            {
                if (occluders[i] != null)
                {
                    occluders[i].Group = this;
                }
            }
        }

        /// <summary>
        /// Manually adds an occluder to this group.
        /// </summary>
        public void AddOccluder(WallOccluder occluder)
        {
            if (occluder == null) return;
            if (!occluders.Contains(occluder))
            {
                occluders.Add(occluder);
            }
            occluder.Group = this;
        }

        /// <summary>
        /// Clears all registered occluders.
        /// </summary>
        public void ClearOccluders()
        {
            for (int i = 0; i < occluders.Count; i++)
            {
                if (occluders[i] != null && occluders[i].Group == this)
                {
                    occluders[i].Group = null;
                }
            }
            occluders.Clear();
        }
    }
}
