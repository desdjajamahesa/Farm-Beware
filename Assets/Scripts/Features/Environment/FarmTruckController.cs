using UnityEngine;
using FeaturesInteraction;
using FeaturesTime;

namespace FeaturesEnvironment
{
    /// <summary>
    /// Thin adapter controlling the interactive farm pickup truck located outside the homestead fence.
    /// Manages headlight state toggling, emissive materials, audio clicks, and narrative interaction.
    /// </summary>
    [DisallowMultipleComponent]
    public class FarmTruckController : MonoBehaviour
    {
        [Header("Vehicle Lighting")]
        [Tooltip("Spot or point lights representing the forward halogen headlight beams.")]
        [SerializeField] private Light[] headlightBeams;

        [Tooltip("Renderers for headlight glass elements displaying HDR emission.")]
        [SerializeField] private Renderer[] headlightRenderers;

        [Tooltip("Renderers for taillight glass elements displaying red HDR emission.")]
        [SerializeField] private Renderer[] taillightRenderers;

        [Header("Emissive Colors")]
        [ColorUsage(true, true)]
        [SerializeField] private Color headlightEmissionOn = new Color(1.0f, 0.92f, 0.75f, 1.0f) * 2.5f;
        [SerializeField] private Color headlightEmissionOff = new Color(0.1f, 0.1f, 0.1f, 1.0f);

        [ColorUsage(true, true)]
        [SerializeField] private Color taillightEmissionOn = new Color(1.0f, 0.08f, 0.08f, 1.0f) * 2.2f;
        [SerializeField] private Color taillightEmissionOff = new Color(0.15f, 0.02f, 0.02f, 1.0f);

        [Header("State & Settings")]
        [Tooltip("Current status of the vehicle headlights.")]
        [SerializeField] private bool areHeadlightsOn = false;

        [Tooltip("Optional audio source for dashboard switch relay click.")]
        [SerializeField] private AudioSource switchAudioSource;

        [Tooltip("Custom narrative examine message displayed on interaction.")]
        [TextArea(2, 4)]
        [SerializeField] private string inspectionMessage = 
            "An old farm pickup truck. The engine is silent and the battery is weak, but the alternator still holds enough charge to toggle the headlights.";

        private MaterialPropertyBlock _headlightPropBlock;
        private MaterialPropertyBlock _taillightPropBlock;
        private static readonly int EmissionColorId = Shader.PropertyToID("_EmissionColor");

        public bool AreHeadlightsOn => areHeadlightsOn;

        private void Awake()
        {
            _headlightPropBlock = new MaterialPropertyBlock();
            _taillightPropBlock = new MaterialPropertyBlock();

            ApplyLightingState(areHeadlightsOn);
        }

        private void Start()
        {
            // Subscribe to TimeManager day/night transitions if desired
            if (TimeManager.Instance != null)
            {
                TimeManager.Instance.OnPhaseChanged += HandlePhaseChanged;
                // If spawned during night, default headlights to on for atmospheric effect
                if (TimeManager.Instance.currentPhase == TimeManager.DayPhase.Night)
                {
                    areHeadlightsOn = true;
                    ApplyLightingState(true);
                }
            }
        }

        private void OnDestroy()
        {
            if (TimeManager.Instance != null)
            {
                TimeManager.Instance.OnPhaseChanged -= HandlePhaseChanged;
            }
        }

        private void HandlePhaseChanged(TimeManager.DayPhase newPhase)
        {
            // Automatically switch headlights on at Night
            if (newPhase == TimeManager.DayPhase.Night)
            {
                areHeadlightsOn = true;
                ApplyLightingState(true);
            }
        }

        public void ToggleHeadlights()
        {
            areHeadlightsOn = !areHeadlightsOn;
            ApplyLightingState(areHeadlightsOn);

            if (switchAudioSource != null && switchAudioSource.clip != null)
            {
                switchAudioSource.Play();
            }

            string stateText = areHeadlightsOn ? "illuminated" : "switched off";
            Debug.Log($"[FarmTruckController] {inspectionMessage} (Headlights {stateText})");
        }

        public void SetHeadlights(bool enable)
        {
            areHeadlightsOn = enable;
            ApplyLightingState(enable);
        }

        public void SetupReferences(Light[] beams, Renderer[] headlights, Renderer[] taillights, AudioSource audioSource = null)
        {
            headlightBeams = beams;
            headlightRenderers = headlights;
            taillightRenderers = taillights;
            switchAudioSource = audioSource;

            if (_headlightPropBlock == null) _headlightPropBlock = new MaterialPropertyBlock();
            if (_taillightPropBlock == null) _taillightPropBlock = new MaterialPropertyBlock();

            ApplyLightingState(areHeadlightsOn);
        }

        private void ApplyLightingState(bool enabled)
        {
            if (headlightBeams != null)
            {
                for (int i = 0; i < headlightBeams.Length; i++)
                {
                    if (headlightBeams[i] != null)
                    {
                        headlightBeams[i].enabled = enabled;
                    }
                }
            }

            Color hColor = enabled ? headlightEmissionOn : headlightEmissionOff;
            if (headlightRenderers != null && _headlightPropBlock != null)
            {
                for (int i = 0; i < headlightRenderers.Length; i++)
                {
                    if (headlightRenderers[i] != null)
                    {
                        headlightRenderers[i].GetPropertyBlock(_headlightPropBlock);
                        _headlightPropBlock.SetColor(EmissionColorId, hColor);
                        headlightRenderers[i].SetPropertyBlock(_headlightPropBlock);
                    }
                }
            }

            Color tColor = enabled ? taillightEmissionOn : taillightEmissionOff;
            if (taillightRenderers != null && _taillightPropBlock != null)
            {
                for (int i = 0; i < taillightRenderers.Length; i++)
                {
                    if (taillightRenderers[i] != null)
                    {
                        taillightRenderers[i].GetPropertyBlock(_taillightPropBlock);
                        _taillightPropBlock.SetColor(EmissionColorId, tColor);
                        taillightRenderers[i].SetPropertyBlock(_taillightPropBlock);
                    }
                }
            }
        }
    }
}
