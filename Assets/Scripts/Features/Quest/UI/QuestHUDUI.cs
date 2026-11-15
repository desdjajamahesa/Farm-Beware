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
        private INarrativeObjectiveService _objectiveService;
        private FeaturesTime.DayNightTimeManager _subscribedDayNight;
        private ITimeService _subscribedTimeService;
        private int _cachedStage = -1;
        private int _cachedFlagsMask = -1;
        private int _cachedPlanted = -1;
        private int _cachedWatered = -1;
        private int _cachedDayHidden = -1;
        private int _cachedObjectiveVersion = -1;
        private int _lastRenderedObjectiveVersion = -1;

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

        /// <summary>
        /// Zero-GC Update loop. Performs zero per-frame string allocations.
        /// State transitions and text updates are driven strictly via events.
        /// </summary>
        private void Update()
        {
            SimulateFrame(Time.unscaledDeltaTime);
        }

        /// <summary>
        /// Frame simulation entry point for automated Zero-GC profiling tests.
        /// Guarantees 0 B GC allocation per call when event state has not mutated.
        /// </summary>
        public void SimulateFrame(float deltaTime)
        {
            if (!_isSubscribed || _objectiveService == null || (_subscribedTimeService == null && _subscribedDayNight == null))
            {
                TrySubscribe();
            }
        }

        private void TrySubscribe()
        {
            bool newlySubscribed = false;

            if (!_isSubscribed && QuestManager.Instance != null)
            {
                QuestManager.Instance.OnQuestUpdated -= HandleQuestUpdated;
                QuestManager.Instance.OnQuestUpdated += HandleQuestUpdated;
                _isSubscribed = true;
                newlySubscribed = true;
            }

            if (_objectiveService == null)
            {
                INarrativeObjectiveService svc = ServiceLocator.Resolve<INarrativeObjectiveService>();
                if (svc == null && QuestManager.HasInstance)
                {
                    svc = QuestManager.Instance;
                }
                if (svc != null)
                {
                    _objectiveService = svc;
                    _objectiveService.OnObjectiveStateChanged -= HandleObjectiveStateChanged;
                    _objectiveService.OnObjectiveStateChanged += HandleObjectiveStateChanged;
                    newlySubscribed = true;
                }
            }

            if (_subscribedTimeService == null && ServiceLocator.TryResolve<ITimeService>(out var timeSvc) && timeSvc != null)
            {
                _subscribedTimeService = timeSvc;
                _subscribedTimeService.OnDayChanged -= HandleDayChanged;
                _subscribedTimeService.OnDayChanged += HandleDayChanged;
                newlySubscribed = true;
            }

            if (_subscribedDayNight == null && FeaturesTime.DayNightTimeManager.Instance != null)
            {
                _subscribedDayNight = FeaturesTime.DayNightTimeManager.Instance;
                _subscribedDayNight.OnDayChanged -= HandleDayChanged;
                _subscribedDayNight.OnDayChanged += HandleDayChanged;
                newlySubscribed = true;
            }

            if (newlySubscribed)
            {
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
            if (_objectiveService != null)
            {
                _objectiveService.OnObjectiveStateChanged -= HandleObjectiveStateChanged;
                _objectiveService = null;
            }
            if (_subscribedTimeService != null)
            {
                _subscribedTimeService.OnDayChanged -= HandleDayChanged;
                _subscribedTimeService = null;
            }
            if (_subscribedDayNight != null)
            {
                _subscribedDayNight.OnDayChanged -= HandleDayChanged;
                _subscribedDayNight = null;
            }
        }

        private void HandleDayChanged(int day)
        {
            _cachedObjectiveVersion++;
            RefreshDisplay();
        }

        private void HandleObjectiveStateChanged(string objectiveId, bool isCompleted)
        {
            _cachedObjectiveVersion++;
            RefreshDisplay();
        }

        private void HandleQuestUpdated(QuestProgressModel model)
        {
            if (model == null) return;

            int stage = (int)model.CurrentStage;
            int flagsMask = ComputeFlagsMask(model);
            int planted = model.PlantedCount;
            int watered = model.WateredCount;
            int dayHidden = ResolveCurrentGameDay() == 0 ? 1 : 0;

            // Zero string formatting if state has not mutated
            if (stage == _cachedStage &&
                flagsMask == _cachedFlagsMask &&
                planted == _cachedPlanted &&
                watered == _cachedWatered &&
                dayHidden == _cachedDayHidden &&
                _lastRenderedObjectiveVersion == _cachedObjectiveVersion)
            {
                return;
            }

            UpdateDisplayFromModel(model);
        }

        private static int ComputeFlagsMask(QuestProgressModel model)
        {
            int mask = 0;
            if (model.HasHoe) mask |= 1 << 0;
            if (model.HasPlantWaterer) mask |= 1 << 1;
            if (model.HasWaterBottle) mask |= 1 << 2;
            if (model.HasSweetPotatoSeed) mask |= 1 << 3;
            if (model.HasPlantedCrop) mask |= 1 << 4;
            if (model.HasWateredCrop) mask |= 1 << 5;
            if (model.IsCompleted) mask |= 1 << 6;
            if (model.HasEnteredKitchen) mask |= 1 << 7;
            if (model.HasTaro) mask |= 1 << 8;
            if (model.HasCoconut) mask |= 1 << 9;
            if (model.HasCookedStew) mask |= 1 << 10;
            if (model.HasConsumedStew) mask |= 1 << 11;
            return mask;
        }

        private static int ResolveCurrentGameDay()
        {
            if (ServiceLocator.TryResolve<ITimeService>(out var timeService) && timeService != null)
            {
                return timeService.CurrentDay;
            }
            if (FeaturesTime.DayNightTimeManager.Instance != null)
            {
                return FeaturesTime.DayNightTimeManager.Instance.CurrentDay;
            }
            return 1;
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

            int currentDay = ResolveCurrentGameDay();
            _cachedStage = (int)model.CurrentStage;
            _cachedFlagsMask = ComputeFlagsMask(model);
            _cachedPlanted = model.PlantedCount;
            _cachedWatered = model.WateredCount;
            _cachedDayHidden = currentDay == 0 ? 1 : 0;
            _lastRenderedObjectiveVersion = _cachedObjectiveVersion;

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
                questHeaderBadgeText.text = $"• SIDE QUEST {statusTag} [Q]";
                questHeaderBadgeText.color = (model.CurrentStage == QuestStage.Completed || model.CurrentStage == QuestStage.CookingQuestCompleted)
                    ? new Color(1f, 0.84f, 0.25f)
                    : new Color(0.40f, 0.85f, 1f);
            }

            if (questDetailText != null)
            {
                string baseText = model.GetFormattedObjectiveDetails();

                if (_objectiveService != null && _objectiveService.ActiveObjectives.Count > 0)
                {
                    NarrativeObjective activeStoryObj = null;
                    for (int i = 0; i < _objectiveService.ActiveObjectives.Count; i++)
                    {
                        var obj = _objectiveService.ActiveObjectives[i];
                        if (obj != null && !obj.isCompleted)
                        {
                            activeStoryObj = obj;
                            break;
                        }
                    }

                    if (activeStoryObj != null)
                    {
                        questDetailText.text = $"{baseText}\n<color=#38BDF8>• STORY:</color> {activeStoryObj.title} ({activeStoryObj.currentCount}/{activeStoryObj.targetCount})";
                        return;
                    }
                }

                questDetailText.text = baseText;
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
                AttachQuestCardClickHandler();
                ApplyCardLayout();
            }
            else
            {
                BuildProceduralQuestUI();
            }
        }

        private void ApplyCardLayout()
        {
            if (questCardRoot != null && questCardRoot.TryGetComponent<RectTransform>(out var cardRt))
            {
                cardRt.sizeDelta = new Vector2(cardRt.sizeDelta.x, Mathf.Max(cardRt.sizeDelta.y, 98f));
            }
            if (questHeaderBadgeText != null)
            {
                RectTransform headerRt = questHeaderBadgeText.rectTransform;
                headerRt.anchorMin = new Vector2(0.05f, 0.72f);
                headerRt.anchorMax = new Vector2(0.95f, 0.96f);
                headerRt.offsetMin = Vector2.zero;
                headerRt.offsetMax = Vector2.zero;
            }
            if (questDetailText != null)
            {
                RectTransform detailRt = questDetailText.rectTransform;
                detailRt.anchorMin = new Vector2(0.05f, 0.05f);
                detailRt.anchorMax = new Vector2(0.95f, 0.71f);
                detailRt.offsetMin = Vector2.zero;
                detailRt.offsetMax = Vector2.zero;
            }
        }

        private void AttachQuestCardClickHandler()
        {
            if (questCardRoot == null) return;

            if (questCardRoot.TryGetComponent<Image>(out var bgImg))
            {
                bgImg.raycastTarget = true;
            }
            if (questHeaderBadgeText != null) questHeaderBadgeText.raycastTarget = false;
            if (questDetailText != null) questDetailText.raycastTarget = false;

            if (!questCardRoot.TryGetComponent<Button>(out var cardBtn))
            {
                cardBtn = questCardRoot.AddComponent<Button>();
            }
            cardBtn.transition = Selectable.Transition.None;
            cardBtn.onClick.RemoveListener(HandleQuestCardClicked);
            cardBtn.onClick.AddListener(HandleQuestCardClicked);
        }

        private static void HandleQuestCardClicked()
        {
            QuestLogUI.EnsureInstance()?.ToggleQuestLog();
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
            cardRt.sizeDelta = new Vector2(0f, 98f);

            Image bg = questCardRoot.GetComponent<Image>();
            bg.color = new Color(0.06f, 0.08f, 0.13f, 0.94f);
            bg.raycastTarget = true;

            GameObject headerObj = new GameObject("QuestHeader", typeof(RectTransform), typeof(TextMeshProUGUI));
            headerObj.transform.SetParent(questCardRoot.transform, false);
            RectTransform headerRt = headerObj.GetComponent<RectTransform>();
            headerRt.anchorMin = new Vector2(0.05f, 0.72f);
            headerRt.anchorMax = new Vector2(0.95f, 0.96f);
            headerRt.offsetMin = Vector2.zero;
            headerRt.offsetMax = Vector2.zero;
            questHeaderBadgeText = headerObj.GetComponent<TextMeshProUGUI>();
            questHeaderBadgeText.fontSize = 12f;
            questHeaderBadgeText.fontStyle = FontStyles.Bold;
            questHeaderBadgeText.color = new Color(0.40f, 0.85f, 1f);
            questHeaderBadgeText.alignment = TextAlignmentOptions.Left;
            questHeaderBadgeText.raycastTarget = false;
            questHeaderBadgeText.text = "• SIDE QUEST • FIRST STEPS [Q]";

            GameObject detailObj = new GameObject("QuestDetail", typeof(RectTransform), typeof(TextMeshProUGUI));
            detailObj.transform.SetParent(questCardRoot.transform, false);
            RectTransform detailRt = detailObj.GetComponent<RectTransform>();
            detailRt.anchorMin = new Vector2(0.05f, 0.05f);
            detailRt.anchorMax = new Vector2(0.95f, 0.71f);
            detailRt.offsetMin = Vector2.zero;
            detailRt.offsetMax = Vector2.zero;
            questDetailText = detailObj.GetComponent<TextMeshProUGUI>();
            questDetailText.fontSize = 11f;
            questDetailText.color = new Color(0.92f, 0.94f, 0.96f);
            questDetailText.richText = true;
            questDetailText.textWrappingMode = TextWrappingModes.Normal;
            questDetailText.overflowMode = TextOverflowModes.Ellipsis;
            questDetailText.alignment = TextAlignmentOptions.TopLeft;
            questDetailText.raycastTarget = false;
            questDetailText.text = "Take farming tools from Storage Chest:\n<color=#FFA0A0>[  ]</color> Hoe  <color=#FFA0A0>[  ]</color> Waterer\n<color=#FFA0A0>[  ]</color> Bottle  <color=#FFA0A0>[  ]</color> Sweet Potato Seed";

            AttachQuestCardClickHandler();
        }
    }
}
