using UnityEngine;
using UnityEngine.InputSystem;
using FeaturesCombat;
using FeaturesCombat.Core.PureLogic;

namespace Player
{
    /// <summary>
    /// Lightweight toggleable PlayMode combat telemetry overlay and gizmo drawer.
    /// Toggle with F8. Zero heap allocations when disabled. Strictly read-only observer.
    /// </summary>
    public class CombatDebugTelemetryOverlay : MonoBehaviour
    {
        [Header("Debug Telemetry")]
        [Tooltip("Toggle PlayMode combat telemetry HUD and phase gizmos (Hotkey: F8).")]
        [SerializeField] private bool telemetryEnabled = false;

        private PlayerControl _playerControl;
        private PlayerEquipment _playerEquipment;
        private PlayerStats _playerStats;

        private readonly EnemyBase[] _nearestEnemies = new EnemyBase[4];

        private void Awake()
        {
            _playerControl = GetComponent<PlayerControl>();
            _playerEquipment = GetComponent<PlayerEquipment>();
            _playerStats = GetComponent<PlayerStats>();
        }

        private void Update()
        {
            if (Keyboard.current != null && Keyboard.current.f8Key.wasPressedThisFrame)
            {
                telemetryEnabled = !telemetryEnabled;
            }
        }

        private void OnGUI()
        {
            if (!telemetryEnabled || !Application.isPlaying) return;

            if (_playerControl == null) _playerControl = GetComponent<PlayerControl>();
            if (_playerEquipment == null) _playerEquipment = GetComponent<PlayerEquipment>();
            if (_playerStats == null) _playerStats = GetComponent<PlayerStats>();

            var actionId = _playerEquipment != null ? _playerEquipment.CurrentCombatActionID : CombatActionID.Idle;
            var phase = _playerEquipment != null ? _playerEquipment.CurrentSwingPhase : CombatPhase.Idle;
            var buf = _playerControl != null ? _playerControl.CurrentBufferedAction : PlayerCancelAction.None;
            var def = _playerStats != null ? _playerStats.DefenseEvaluator : null;

            int meleeTokens = 0;
            int rangedTokens = 0;
            if (NightBrawlManager.Instance != null && NightBrawlManager.Instance.TokenDispatcher != null)
            {
                meleeTokens = NightBrawlManager.Instance.TokenDispatcher.CountActiveTokens(false, Time.time);
                rangedTokens = NightBrawlManager.Instance.TokenDispatcher.CountActiveTokens(true, Time.time);
            }

            GUILayout.BeginArea(new Rect(14f, 14f, 410f, 255f), GUI.skin.box);
            GUILayout.Label("<b>[F8] COMBAT TELEMETRY (60 FPS AUDIT)</b>");
            GUILayout.Label($"State: {(_playerControl != null ? _playerControl.CurrentActionState.ToString() : "N/A")} | Move: {actionId} [{phase}]");
            GUILayout.Label($"Buffer: {buf} | Lunge: {(_playerControl != null ? _playerControl.ActiveLungeSpeed : 0f):F1} m/s | Dodge%: {(_playerControl != null ? _playerControl.DodgeProgress * 100f : 0f):F0}%");
            if (def != null)
            {
                GUILayout.Label($"Defense: iFrame={def.IsDodgeInvulnerable} | Parry={def.IsParryActive} | Whiff={def.RemainingWhiffTime:F2}s | Riposte={def.RemainingRiposteTime:F2}s");
            }
            GUILayout.Label($"Tokens Active: Melee={meleeTokens}/3 | Ranged={rangedTokens}/2");

            PopulateNearestEnemies();
            for (int i = 0; i < _nearestEnemies.Length; i++)
            {
                var e = _nearestEnemies[i];
                if (e == null) break;
                var p = e.PoiseTracker;
                string lockTag = e.IsTrackingLocked ? "LOCKED" : (e.IsInRecoveryPunishWindow ? "PUNISH" : e.CurrentTacticalState.ToString());
                GUILayout.Label($"  [{e.enemyType}] HP:{e.currentHealth}/{e.maxHealth} Poise:{p.CurrentPoise:F0}/{p.MaxPoise:F0} SA:{p.IsSuperArmorActive} ({lockTag})");
            }
            GUILayout.EndArea();
        }

        private void PopulateNearestEnemies()
        {
            for (int i = 0; i < _nearestEnemies.Length; i++) _nearestEnemies[i] = null;
            if (NightBrawlManager.Instance == null) return;

            var list = NightBrawlManager.Instance.ActiveEnemies;
            if (list == null || list.Count == 0) return;

            Vector3 myPos = transform.position;
            int filled = 0;
            for (int i = 0; i < list.Count && filled < _nearestEnemies.Length; i++)
            {
                var e = list[i];
                if (e != null && !e.IsDead)
                {
                    _nearestEnemies[filled++] = e;
                }
            }
        }

        private void OnDrawGizmos()
        {
            if (!telemetryEnabled) return;
            if (_playerEquipment == null) _playerEquipment = GetComponent<PlayerEquipment>();
            if (_playerEquipment == null) return;

            var phase = _playerEquipment.CurrentSwingPhase;
            Color phaseColor = phase switch
            {
                CombatPhase.Startup => new Color(1f, 0.85f, 0.15f, 0.85f),
                CombatPhase.Active => new Color(1f, 0.2f, 0.1f, 0.9f),
                CombatPhase.Recovery => new Color(0.2f, 0.9f, 0.35f, 0.85f),
                _ => new Color(0.3f, 0.7f, 1f, 0.45f)
            };

            Gizmos.color = phaseColor;
            float range = _playerEquipment.AttackHitRange;
            Vector3 center = transform.position + Vector3.up * 0.8f + transform.forward * (range * 0.5f);
            Gizmos.DrawWireSphere(center, range * 0.65f);

            if (NightBrawlManager.Instance != null && NightBrawlManager.Instance.ActiveEnemies != null)
            {
                var enemies = NightBrawlManager.Instance.ActiveEnemies;
                for (int i = 0; i < enemies.Count; i++)
                {
                    var e = enemies[i];
                    if (e == null || e.IsDead) continue;

                    Gizmos.color = e.IsTrackingLocked ? Color.red : (e.IsInRecoveryPunishWindow ? Color.green : Color.yellow);
                    Gizmos.DrawRay(e.transform.position + Vector3.up * 0.6f, e.LockedStrikeDirection * (e.attackRange + 0.55f));
                }
            }
        }
    }
}
