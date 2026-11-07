using UnityEngine;

namespace FarmBeware.Core.Runtime
{
    /// <summary>
    /// Represents 2D world spatial bounds for orthographic map projection.
    /// Pure value type with zero heap allocations.
    /// </summary>
    public readonly struct MapBounds2D
    {
        public readonly float MinX;
        public readonly float MaxX;
        public readonly float MinZ;
        public readonly float MaxZ;

        public MapBounds2D(float minX, float maxX, float minZ, float maxZ)
        {
            MinX = minX;
            MaxX = maxX;
            MinZ = minZ;
            MaxZ = maxZ;
        }

        public float SpanX => Mathf.Max(0.0001f, MaxX - MinX);
        public float SpanZ => Mathf.Max(0.0001f, MaxZ - MinZ);

        /// <summary>
        /// Exact coordinate bounding box for the Style C in-engine orthographic render:
        /// Camera Center = (-70, 35), OrthoSize = 65, Aspect = 16:9.
        /// </summary>
        public static MapBounds2D DefaultStyleCBounds => new MapBounds2D(
            minX: -185.55556f,
            maxX: 45.55556f,
            minZ: -30.0f,
            maxZ: 100.0f
        );
    }

    /// <summary>
    /// Pure POCO math service for projecting 3D world coordinates onto 2D UI map canvases.
    /// Guaranteed Zero-GC across all frame calculations.
    /// </summary>
    public static class MapProjectionMath
    {
        /// <summary>
        /// Converts a 3D world position (X, Z) into a normalized UV coordinate in [0, 1].
        /// </summary>
        public static Vector2 WorldToNormalizedUV(Vector3 worldPos, MapBounds2D bounds, bool clamp = true)
        {
            float u = (worldPos.x - bounds.MinX) / bounds.SpanX;
            float v = (worldPos.z - bounds.MinZ) / bounds.SpanZ;

            if (clamp)
            {
                u = Mathf.Clamp01(u);
                v = Mathf.Clamp01(v);
            }

            return new Vector2(u, v);
        }

        /// <summary>
        /// Converts normalized UV coordinates [0, 1] to centered UI anchored coordinates.
        /// Assumes the RectTransform has pivot at (0.5, 0.5).
        /// </summary>
        public static Vector2 UVToAnchoredPosition(Vector2 uv, Vector2 containerSize)
        {
            float anchoredX = (uv.x - 0.5f) * containerSize.x;
            float anchoredY = (uv.y - 0.5f) * containerSize.y;
            return new Vector2(anchoredX, anchoredY);
        }

        /// <summary>
        /// Directly calculates the anchored UI position of a 3D world position in a single zero-allocation call.
        /// </summary>
        public static Vector2 WorldToMapAnchoredPosition(Vector3 worldPos, MapBounds2D bounds, Vector2 containerSize, bool clamp = true)
        {
            Vector2 uv = WorldToNormalizedUV(worldPos, bounds, clamp);
            return UVToAnchoredPosition(uv, containerSize);
        }

        /// <summary>
        /// Converts 3D world yaw rotation (clockwise from North/+Z around Y)
        /// to 2D UI Z-axis rotation angle (counter-clockwise in screen space).
        /// </summary>
        public static float WorldYawToUIRotationZ(float worldYawDegrees)
        {
            return -worldYawDegrees;
        }

        /// <summary>
        /// Returns true if the world position lies within the specified map bounds.
        /// </summary>
        public static bool IsWithinBounds(Vector3 worldPos, MapBounds2D bounds)
        {
            return worldPos.x >= bounds.MinX && worldPos.x <= bounds.MaxX &&
                   worldPos.z >= bounds.MinZ && worldPos.z <= bounds.MaxZ;
        }
    }
}

