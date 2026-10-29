using UnityEngine;
using UnityEngine.UI;
using TMPro;
using FarmBeware.Core.Runtime;
using FarmBeware.Logic.Quests;
using FeaturesTime.UI;

namespace FarmBeware.Features.Quest.UI
{
    /// <summary>
    /// Displays the active Side Quest tracker card in the HUD directly underneath the Activity card.
    /// Follows the visual aesthetic of CombatPhaseTrackerUI with rich text progress indicators.
    /// </summary>
    public class QuestHUDUI : MonoBehaviour
    {
        [Header("Procedural References")]
        [SerializeField] private GameObject questCardRoot;
        [SerializeField] private TextMeshProUGUI questHeaderBadgeText;
        [SerializeField] private TextMeshProUGUI questDetailText;

        private bool _isSubscribed = false;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void AutoAttachToHUD()
        {
            UnityEngine.SceneManagement.SceneManager.sceneLoaded -= HandleSceneLoaded;
            UnityEngine.SceneManagement.SceneManager.sceneLoaded += HandleSceneLoaded;
            EnsureInstance();
        }

        private static void HandleSceneLoaded(UnityEngine.SceneManagement.Scene scene, UnityEngine.SceneManagement.LoadSceneMode mode)
        {
            EnsureInstance();
        }

        public static QuestHUDUI EnsureInstance()
        {
            var tracker = CombatPhaseTrackerUI.Instance;
            if (tracker == null)
            {
                var found = UnityEngine.Object.FindObjectsByType<CombatPhaseTrackerUI>(UnityEngine.FindObjectsInactive.Include, UnityEngine.FindObjectsSortMode.None);
                if (found != null && found.Length > 0)
                {
                    tracker = found[0];
                }
            }

            if (tracker != null)
            {
                var existing = tracker.GetComponentInChildren<QuestHUDUI>(true);
                if (existing != null) return existing;

                var hud = tracker.gameObject.AddComponent<QuestHUDUI>();
                return hud;
            }
            return null;
        }

        private void Awake()
        {
            EnsureUIHierarchy();
        }

        private void Start()
        {
            EnsureUIHierarchy();
            TrySubscribe();
            RefreshDisplay();
        }

        private void OnEnable()
        {
            TrySubscribe();
            RefreshDisplay();
        }

        private void OnDisable()
        {
            Unsubscribe();
        }

        private void Update()
        {
            if (!_isSubscribed)
            {
                TrySubscribe();
            }

            if (questCardRoot == null || questHeaderBadgeText == null || questDetailText == null)
            {
                EnsureUIHierarchy();
            }

            if (QuestManager.Instance != null && QuestManager.Instance.Model != null)
            {
                UpdateDisplayFromModel(QuestManager.Instance.Model);
            }
        }

        private void TrySubscribe()
        {
            if (_isSubscribed) return;

            if (QuestManager.Instance != null)
            {
                QuestManager.Instance.OnQuestUpdated += HandleQuestUpdated;
                _isSubscribed = true;
                RefreshDisplay();
            }
        }

        private void Unsubscribe()
        {
            if (_isSubscribed)
            {
                if (QuestManager.HasInstance && QuestManager.Instance != null)
                {
                    QuestManager.Instance.OnQuestUpdated -= HandleQuestUpdated;
                }
                _isSubscribed = false;
            }
        }

        private void HandleQuestUpdated(QuestProgressModel model)
        {
            UpdateDisplayFromModel(model);
        }

        public void RefreshDisplay()
        {
            if (QuestManager.Instance != null && QuestManager.Instance.Model != null)
            {
                UpdateDisplayFromModel(QuestManager.Instance.Model);
            }
        }

        public void UpdateDisplayFromModel(QuestProgressModel model)
        {
            if (model == null) return;

            // Suppress Side Quest card on Day 0 per narrative progression (player only sleeps on Day 0)
            int currentDay = 1;
            if (ServiceLocator.TryResolve<ITimeService>(out var timeService) && timeService != null)
            {
                currentDay = timeService.CurrentDay;
            }
            else if (FeaturesTime.DayNightTimeManager.Instance != null)
            {
                currentDay = FeaturesTime.DayNightTimeManager.Instance.CurrentDay;
            }

            if (currentDay == 0)
            {
                if (questCardRoot != null && questCardRoot.activeSelf)
                {
                    questCardRoot.SetActive(false);
                }
                return;
            }
            else
            {
                if (questCardRoot != null && !questCardRoot.activeSelf)
                {
                    questCardRoot.SetActive(true);
                }
            }

            if (questHeaderBadgeText != null)
            {
                string statusTag;
                if (model.CurrentStage >= QuestStage.EnterKitchen)
                {
                    statusTag = model.CurrentStage == QuestStage.CookingQuestCompleted ? "• COMPLETED" : "• HOME COOKING";
                }
                else
                {
                    statusTag = model.IsCompleted ? "• COMPLETED" : "• FIRST STEPS";
                }
                questHeaderBadgeText.text = $"• SIDE QUEST {statusTag}";
                questHeaderBadgeText.color = (model.CurrentStage == QuestStage.Completed || model.CurrentStage == QuestStage.CookingQuestCompleted)
                    ? new Color(1f, 0.84f, 0.25f)
                    : new Color(0.40f, 0.85f, 1f);
            }

            if (questDetailText != null)
            {
                questDetailText.text = model.GetFormattedObjectiveDetails();
            }
        }

        public void EnsureUIHierarchy()
        {
            Transform existingCard = transform.Find("QuestCard");
            if (existingCard != null)
            {
                questCardRoot = existingCard.gameObject;
                var tmps = existingCard.GetComponentsInChildren<TextMeshProUGUI>(true);
                foreach (var t in tmps)
                {
                    if (t.gameObject.name.Equals("QuestHeader", System.StringComparison.OrdinalIgnoreCase)) questHeaderBadgeText = t;
                    else if (t.gameObject.name.Equals("QuestDetail", System.StringComparison.OrdinalIgnoreCase)) questDetailText = t;
                }
            }
            else
            {
                BuildProceduralQuestUI();
            }
        }

        public void BuildProceduralQuestUI()
        {
            questCardRoot = new GameObject("QuestCard", typeof(RectTransform), typeof(Image));
            questCardRoot.transform.SetParent(transform, false);

            RectTransform cardRt = questCardRoot.GetComponent<RectTransform>();
            cardRt.anchorMin = new Vector2(0f, 0f);
            cardRt.anchorMax = new Vector2(1f, 0f);
            cardRt.pivot = new Vector2(0.5f, 1f);
            // Positioned 6px below ActivityCard (ActivityCard is anchored at -8f with height 72f -> bottom is -80f)
            cardRt.anchoredPosition = new Vector2(0f, -86f);
            cardRt.sizeDelta = new Vector2(0f, 84f);

            Image bg = questCardRoot.GetComponent<Image>();
            bg.color = new Color(0.06f, 0.08f, 0.13f, 0.94f);

            GameObject headerObj = new GameObject("QuestHeader", typeof(RectTransform), typeof(TextMeshProUGUI));
            headerObj.transform.SetParent(questCardRoot.transform, false);
            RectTransform headerRt = headerObj.GetComponent<RectTransform>();
            headerRt.anchorMin = new Vector2(0.05f, 0.68f);
            headerRt.anchorMax = new Vector2(0.95f, 0.96f);
            headerRt.offsetMin = Vector2.zero;
            headerRt.offsetMax = Vector2.zero;
            questHeaderBadgeText = headerObj.GetComponent<TextMeshProUGUI>();
            questHeaderBadgeText.fontSize = 12f;
            questHeaderBadgeText.fontStyle = FontStyles.Bold;
            questHeaderBadgeText.color = new Color(0.40f, 0.85f, 1f);
            questHeaderBadgeText.alignment = TextAlignmentOptions.Left;
            questHeaderBadgeText.text = "• SIDE QUEST • FIRST STEPS";

            GameObject detailObj = new GameObject("QuestDetail", typeof(RectTransform), typeof(TextMeshProUGUI));
            detailObj.transform.SetParent(questCardRoot.transform, false);
            RectTransform detailRt = detailObj.GetComponent<RectTransform>();
            detailRt.anchorMin = new Vector2(0.05f, 0.06f);
            detailRt.anchorMax = new Vector2(0.95f, 0.68f);
            detailRt.offsetMin = Vector2.zero;
            detailRt.offsetMax = Vector2.zero;
            questDetailText = detailObj.GetComponent<TextMeshProUGUI>();
            questDetailText.fontSize = 11f;
            questDetailText.color = new Color(0.92f, 0.94f, 0.96f);
            questDetailText.richText = true;
            questDetailText.textWrappingMode = TextWrappingModes.Normal;
            questDetailText.overflowMode = TextOverflowModes.Ellipsis;
            questDetailText.alignment = TextAlignmentOptions.TopLeft;
            questDetailText.text = "Take farming tools from Storage Chest:\n<color=#FFA0A0>[  ]</color> Hoe  <color=#FFA0A0>[  ]</color> Waterer\n<color=#FFA0A0>[  ]</color> Bottle  <color=#FFA0A0>[  ]</color> Sweet Potato Seed";
        }
    }
}
