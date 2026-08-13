using UnityEngine;

// Penanda/identitas sederhana untuk objek Piala (Trophy).
// Dipasang pada GameObject 3D piala; collider dijamin ada berkat RequireComponent.
[RequireComponent(typeof(Collider))]
public class TrophyItem : MonoBehaviour
{
    // Nama piala untuk keperluan tooltip/UI di masa depan.
    public string trophyName = "Unnamed Trophy";
}