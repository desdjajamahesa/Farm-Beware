using UnityEngine;

namespace FarmBeware.Core.Runtime
{
    /// <summary>
    /// Contract defining camera occlusion state for foreground geometry.
    /// </summary>
    public interface IOccluder
    {
        bool IsOccluding { get; }
        Material[] OriginalMaterials { get; }
    }
}
