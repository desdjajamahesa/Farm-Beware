using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using FarmBeware.Core.Runtime;

namespace FeaturesInteraction
{
    /// <summary>
    /// Modal dialog displayed when the player attempts to sleep early during daytime
    /// while crops in the farmland are about to ripen.
    /// Provides confirmation options: "Sleep Anyway" or "Cancel".
    /// </summary>
    public class BedSleepConfirmationUI : MonoBehaviour
    {
        private static BedSleepConfirmationUI _instance;

        private GameObject modalRoot;
        private TextMeshProUGUI warningDetailsText;
        private Action onConfirmAction;
        private Action onCancelAction;

        public static void Show(string cropSummary, Action onConfirm, Action onCancel = null)
        {
            if (_instance == null)
            {
                var go = new GameObject("BedSleepConfirmationUI");
                _instance = go.AddComponent<BedSleepConfirmationUI>();
                DontDestroyOnLoad(go);
            }

            _instance.DisplayModal(cropSummary, onConfirm, onCancel);
        }

        private void DisplayModal(string cropSummary, Action onConfirm, Action onCancel)
        {
            onConfirmAction = onConfirm;
            onCancelAction = onCancel;

            EnsureUIHierarchy();

            if (warningDetailsText != null)
            {
                warningDetailsText.text =
                    $"Your crops are almost ready for harvest:\n" +
                    $"<color=#FBBF24><b>{cropSummary}</b></color>\n\n" +
                    $"Sleeping early will immediately start the <color=#EF4444><b>Night Phase</b></color> and pause daytime crop growth.\n\n" +
                    $"Do you still want to go to sleep?";
            }

            if (modalRoot != null)
            {
                modalRoot.SetActive(true);
            }

            UIModalHelper.IsSaveUIOpen = true;
        }

        private void CloseModal()
        {
            if (modalRoot != null)
            {
                modalRoot.SetActive(false);
            }

            UIModalHelper.IsSaveUIOpen = false;
            UIModalHelper.LastFrameUIPanelClosed = Time.frameCount;
        }

        private void Update()
        {
            if (modalRoot != null && modalRoot.activeSelf)
            {
                if (UnityEngine.InputSystem.Keyboard.current != null)
                {
                    if (UnityEngine.InputSystem.Keyboard.current.escapeKey.wasPressedThisFrame)
                    {
                        HandleCancel();
                    }
                }
            }
        }

        private void HandleConfirm()
        {
            var action = onConfirmAction;
            CloseModal();
            action?.Invoke();
        }

        private void HandleCancel()
        {
            var action = onCancelAction;
            CloseModal();
            action?.Invoke();
        }

        private void EnsureUIHierarchy()
        {
            if (modalRoot != null) return;

            Canvas targetCanvas = null;
            var canvases = FindObjectsByType<Canvas>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            foreach (var c in canvases)
            {
                if (c.isRootCanvas && c.renderMode != RenderMode.WorldSpace)
                {
                    targetCanvas = c;
                    break;
                }
            }

            if (targetCanvas == null)
            {
                var canvasObj = new GameObject("BedSleepCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
                targetCanvas = canvasObj.GetComponent<Canvas>();
                targetCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
                targetCanvas.sortingOrder = 950;
                var scaler = canvasObj.GetComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1920, 1080);
            }

            // Modal Root Container
            modalRoot = new GameObject("SleepConfirmModalRoot", typeof(RectTransform));
            modalRoot.transform.SetParent(targetCanvas.transform, false);
            var rootRt = modalRoot.GetComponent<RectTransform>();
            rootRt.anchorMin = Vector2.zero;
            rootRt.anchorMax = Vector2.one;
            rootRt.offsetMin = Vector2.zero;
            rootRt.offsetMax = Vector2.zero;

            // Semi-transparent Backdrop
            var backdropObj = new GameObject("Backdrop", typeof(RectTransform), typeof(Image), typeof(Button));
            backdropObj.transform.SetParent(modalRoot.transform, false);
            var backdropRt = backdropObj.GetComponent<RectTransform>();
            backdropRt.anchorMin = Vector2.zero;
            backdropRt.anchorMax = Vector2.one;
            backdropRt.offsetMin = Vector2.zero;
            backdropRt.offsetMax = Vector2.zero;
            var backdropImg = backdropObj.GetComponent<Image>();
            backdropImg.color = new Color(0f, 0f, 0f, 0.65f);
            var backdropBtn = backdropObj.GetComponent<Button>();
            backdropBtn.onClick.AddListener(HandleCancel);

            // Dialog Panel Card
            var cardObj = new GameObject("DialogCard", typeof(RectTransform), typeof(Image));
            cardObj.transform.SetParent(modalRoot.transform, false);
            var cardRt = cardObj.GetComponent<RectTransform>();
            cardRt.anchorMin = new Vector2(0.5f, 0.5f);
            cardRt.anchorMax = new Vector2(0.5f, 0.5f);
            cardRt.pivot = new Vector2(0.5f, 0.5f);
            cardRt.sizeDelta = new Vector2(460f, 260f);
            var cardImg = cardObj.GetComponent<Image>();
            cardImg.color = new Color(0.12f, 0.14f, 0.18f, 0.96f);

            // Title Text
            var titleObj = new GameObject("TitleText", typeof(RectTransform), typeof(TextMeshProUGUI));
            titleObj.transform.SetParent(cardObj.transform, false);
            var titleRt = titleObj.GetComponent<RectTransform>();
            titleRt.anchorMin = new Vector2(0.06f, 0.78f);
            titleRt.anchorMax = new Vector2(0.94f, 0.94f);
            titleRt.offsetMin = Vector2.zero;
            titleRt.offsetMax = Vector2.zero;
            var titleTmp = titleObj.GetComponent<TextMeshProUGUI>();
            titleTmp.fontSize = 18f;
            titleTmp.fontStyle = FontStyles.Bold;
            titleTmp.color = new Color(1f, 0.78f, 0.25f);
            titleTmp.alignment = TextAlignmentOptions.Center;
            titleTmp.text = "⚠️ EARLY SLEEP WARNING";

            // Body Message
            var msgObj = new GameObject("MessageText", typeof(RectTransform), typeof(TextMeshProUGUI));
            msgObj.transform.SetParent(cardObj.transform, false);
            var msgRt = msgObj.GetComponent<RectTransform>();
            msgRt.anchorMin = new Vector2(0.08f, 0.28f);
            msgRt.anchorMax = new Vector2(0.92f, 0.76f);
            msgRt.offsetMin = Vector2.zero;
            msgRt.offsetMax = Vector2.zero;
            warningDetailsText = msgObj.GetComponent<TextMeshProUGUI>();
            warningDetailsText.fontSize = 13.5f;
            warningDetailsText.color = new Color(0.92f, 0.94f, 0.96f);
            warningDetailsText.alignment = TextAlignmentOptions.Center;
            warningDetailsText.textWrappingMode = TextWrappingModes.Normal;

            // Buttons Container
            var buttonsObj = new GameObject("ButtonRow", typeof(RectTransform));
            buttonsObj.transform.SetParent(cardObj.transform, false);
            var btnsRt = buttonsObj.GetComponent<RectTransform>();
            btnsRt.anchorMin = new Vector2(0.08f, 0.06f);
            btnsRt.anchorMax = new Vector2(0.92f, 0.24f);
            btnsRt.offsetMin = Vector2.zero;
            btnsRt.offsetMax = Vector2.zero;

            // Cancel Button (Left)
            CreateButton(buttonsObj.transform, "CancelBtn", "CANCEL (STAY)",
                new Vector2(0f, 0f), new Vector2(0.48f, 1f),
                new Color(0.28f, 0.33f, 0.40f, 1f), HandleCancel);

            // Confirm Button (Right)
            CreateButton(buttonsObj.transform, "ConfirmBtn", "SLEEP ANYWAY",
                new Vector2(0.52f, 0f), new Vector2(1f, 1f),
                new Color(0.85f, 0.22f, 0.22f, 1f), HandleConfirm);
        }

        private static void CreateButton(Transform parent, string name, string label, Vector2 anchorMin, Vector2 anchorMax, Color bgColor, Action onClick)
        {
            var btnObj = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            btnObj.transform.SetParent(parent, false);
            var btnRt = btnObj.GetComponent<RectTransform>();
            btnRt.anchorMin = anchorMin;
            btnRt.anchorMax = anchorMax;
            btnRt.offsetMin = Vector2.zero;
            btnRt.offsetMax = Vector2.zero;

            var img = btnObj.GetComponent<Image>();
            img.color = bgColor;

            var btn = btnObj.GetComponent<Button>();
            var colors = btn.colors;
            colors.normalColor = bgColor;
            colors.highlightedColor = bgColor * 1.25f;
            colors.pressedColor = bgColor * 0.8f;
            btn.colors = colors;
            btn.onClick.AddListener(() => onClick?.Invoke());

            var lblObj = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
            lblObj.transform.SetParent(btnObj.transform, false);
            var lblRt = lblObj.GetComponent<RectTransform>();
            lblRt.anchorMin = Vector2.zero;
            lblRt.anchorMax = Vector2.one;
            lblRt.offsetMin = Vector2.zero;
            lblRt.offsetMax = Vector2.zero;

            var tmp = lblObj.GetComponent<TextMeshProUGUI>();
            tmp.text = $"<b>{label}</b>";
            tmp.fontSize = 12f;
            tmp.color = Color.white;
            tmp.alignment = TextAlignmentOptions.Center;
        }
    }
}
