using UnityEngine;

// Simple marker/identity component for Trophy objects.
// Attached to 3D trophy GameObjects; collider is guaranteed via RequireComponent.
[RequireComponent(typeof(Collider))]
public class TrophyItem : MonoBehaviour
{
    // Trophy name for future tooltip/UI use.
    public string trophyName = "Unnamed Trophy";
}