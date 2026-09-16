using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Detects walls blocking the view between Camera and Player Capsule,
/// and smoothly reduces wall opacity (fades transparency) so the full player is visible behind walls.
/// </summary>
public class WallOcclusionFader : MonoBehaviour
{
    [Header("Target Settings")]
    [Tooltip("Target player capsule to keep visible")]
    public Transform target;

    [Header("Fade Settings")]
    [Tooltip("Target opacity when wall is blocking player (0 = invisible, 0.25 = semi-transparent)")]
    [Range(0.05f, 0.8f)]
    public float fadeAlpha = 0.25f;

    [Tooltip("Speed of fading transition")]
    public float fadeSpeed = 10.0f;

    [Tooltip("SphereCast radius for obstacle detection")]
    public float sphereRadius = 0.5f;

    [Tooltip("Layer mask for walls")]
    public LayerMask wallLayerMask = ~0;

    private Dictionary<Renderer, float> currentAlphas = new Dictionary<Renderer, float>();
    private HashSet<Renderer> occludingWalls = new HashSet<Renderer>();

    void Start()
    {
        FindTarget();
    }

    void LateUpdate()
    {
        if (target == null)
        {
            FindTarget();
            if (target == null) return;
        }

        // Raycast directly to capsule center position
        Vector3 targetCenterPos = target.position;
        Vector3 dir = targetCenterPos - transform.position;
        float dist = dir.magnitude;

        occludingWalls.Clear();

        RaycastHit[] hits = Physics.SphereCastAll(transform.position, sphereRadius, dir.normalized, dist, wallLayerMask);
        foreach (RaycastHit hit in hits)
        {
            if (hit.transform == target || hit.transform.IsChildOf(target)) continue;

            Renderer r = hit.transform.GetComponent<Renderer>();
            if (r != null && (hit.transform.name.StartsWith("Wall") || hit.transform.name.Contains("Pillar")))
            {
                occludingWalls.Add(r);
            }
        }

        foreach (Renderer r in occludingWalls)
        {
            if (!currentAlphas.ContainsKey(r))
            {
                currentAlphas[r] = 1.0f;
            }
        }

        List<Renderer> keys = new List<Renderer>(currentAlphas.Keys);
        foreach (Renderer r in keys)
        {
            if (r == null)
            {
                currentAlphas.Remove(r);
                continue;
            }

            bool isOccluding = occludingWalls.Contains(r);
            float targetAlpha = isOccluding ? fadeAlpha : 1.0f;
            float currentAlpha = Mathf.Lerp(currentAlphas[r], targetAlpha, Time.deltaTime * fadeSpeed);
            currentAlphas[r] = currentAlpha;

            SetRendererAlpha(r, currentAlpha);

            if (!isOccluding && Mathf.Abs(currentAlpha - 1.0f) < 0.01f)
            {
                SetRendererAlpha(r, 1.0f);
                currentAlphas.Remove(r);
            }
        }
    }

    private void FindTarget()
    {
        if (target == null)
        {
            GameObject player = GameObject.Find("PlayerCapsule");
            if (player != null) target = player.transform;
        }
    }

    private void SetRendererAlpha(Renderer r, float alpha)
    {
        if (r == null) return;
        Material mat = r.material;

        Color c = mat.color;
        if (mat.HasProperty("_BaseColor")) c = mat.GetColor("_BaseColor");
        else if (mat.HasProperty("_Color")) c = mat.GetColor("_Color");

        c.a = alpha;

        if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", c);
        if (mat.HasProperty("_Color")) mat.SetColor("_Color", c);
        mat.color = c;

        if (alpha < 0.98f)
        {
            mat.SetFloat("_Surface", 1); // 1 = Transparent in URP
            mat.SetOverrideTag("RenderType", "Transparent");
            mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            mat.SetInt("_ZWrite", 0);
            mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            mat.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
        }
        else
        {
            mat.SetFloat("_Surface", 0); // 0 = Opaque in URP
            mat.SetOverrideTag("RenderType", "Opaque");
            mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.One);
            mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.Zero);
            mat.SetInt("_ZWrite", 1);
            mat.DisableKeyword("_SURFACE_TYPE_TRANSPARENT");
            mat.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Geometry;
        }
    }
}
