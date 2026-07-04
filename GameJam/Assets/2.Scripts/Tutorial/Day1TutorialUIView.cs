using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// First-pass tutorial UI. Builds a small fallback UI at runtime when scene
/// references are not assigned, so the jam flow can be wired with one controller.
/// </summary>
public class Day1TutorialUIView : MonoBehaviour
{
    [Header("Optional Scene References")]
    [SerializeField] private Canvas canvas;
    [SerializeField] private RectTransform root;
    [SerializeField] private Image darkenOverlay;
    [SerializeField] private Day1TutorialSpotlightOverlay spotlightOverlay;
    [SerializeField] private RectTransform highlightFrame;
    [SerializeField] private RectTransform objectiveRoot;
    [SerializeField] private TextMeshProUGUI objectiveText;
    [SerializeField] private RectTransform dialogueRoot;
    [SerializeField] private TextMeshProUGUI speakerText;
    [SerializeField] private TextMeshProUGUI dialogueText;
    [SerializeField] private Button dialogueButton;
    [SerializeField] private CanvasGroup dialogueCanvasGroup;
    [SerializeField] private RectTransform toastRoot;
    [SerializeField] private TextMeshProUGUI toastText;

    [Header("Fallback Layout")]
    [Range(0f, 1f)]
    [SerializeField] private float darkOverlayOpacity = 0.55f;
    [SerializeField] private int sortingOrder = 5000;
    [SerializeField] private Vector2 worldTargetFallbackSize = new Vector2(140f, 140f);
    [SerializeField] private Vector2 highlightPadding = new Vector2(54f, 54f);

    private readonly Vector3[] _rectCorners = new Vector3[4];
    private readonly Vector3[] _boundsCorners = new Vector3[8];

    private Transform _worldTarget;
    private RectTransform _uiTarget;
    private TutorialDialogueLine[] _dialogueLines;
    private Action _dialogueComplete;
    private Coroutine _toastRoutine;
    private int _dialogueIndex;
    private bool _highlightVisible;
    private bool _blockingDialogue;

    private void Awake()
    {
        EnsureBuilt();
        HideAll();
    }

    private void LateUpdate()
    {
        if (_highlightVisible) RefreshHighlightFrame();
    }

    public void ShowHighlight(Transform target)
    {
        EnsureBuilt();

        _worldTarget = target;
        _uiTarget = null;
        _highlightVisible = target != null;

        if (!_highlightVisible) SetSpotlightActive(false);
        if (highlightFrame != null) highlightFrame.gameObject.SetActive(_highlightVisible);
        BringToFront();
        RefreshHighlightFrame();
    }

    public void ShowHighlight(RectTransform target)
    {
        EnsureBuilt();

        _worldTarget = null;
        _uiTarget = target;
        _highlightVisible = target != null;

        if (!_highlightVisible) SetSpotlightActive(false);
        if (highlightFrame != null) highlightFrame.gameObject.SetActive(_highlightVisible);
        BringToFront();
        RefreshHighlightFrame();
    }

    public void HideHighlight()
    {
        _worldTarget = null;
        _uiTarget = null;
        _highlightVisible = false;

        SetSpotlightActive(false);
        if (highlightFrame != null) highlightFrame.gameObject.SetActive(false);
    }

    public void ShowObjective(string text)
    {
        EnsureBuilt();
        if (objectiveText != null) objectiveText.text = text;
        if (objectiveRoot != null) objectiveRoot.gameObject.SetActive(!string.IsNullOrEmpty(text));
        BringToFront();
    }

    public void HideObjective()
    {
        if (objectiveRoot != null) objectiveRoot.gameObject.SetActive(false);
    }

    public void ShowGuidedDialogue(string speakerId, string text)
    {
        EnsureBuilt();

        _blockingDialogue = false;
        _dialogueLines = null;
        _dialogueComplete = null;

        if (speakerText != null)
        {
            speakerText.gameObject.SetActive(!string.IsNullOrEmpty(speakerId));
            speakerText.text = speakerId;
        }

        if (dialogueText != null) dialogueText.text = text;
        if (dialogueRoot != null) dialogueRoot.gameObject.SetActive(!string.IsNullOrEmpty(text));
        if (dialogueCanvasGroup != null)
        {
            dialogueCanvasGroup.interactable = false;
            dialogueCanvasGroup.blocksRaycasts = false;
        }

        BringToFront();
    }

    public void PlayBlockingDialogue(TutorialDialogueLine[] lines, Action onComplete)
    {
        EnsureBuilt();

        if (lines == null || lines.Length == 0)
        {
            onComplete?.Invoke();
            return;
        }

        _blockingDialogue = true;
        _dialogueLines = lines;
        _dialogueComplete = onComplete;
        _dialogueIndex = 0;

        if (dialogueCanvasGroup != null)
        {
            dialogueCanvasGroup.interactable = true;
            dialogueCanvasGroup.blocksRaycasts = true;
        }

        if (dialogueRoot != null) dialogueRoot.gameObject.SetActive(true);
        ShowDialogueLine(_dialogueLines[_dialogueIndex]);
        BringToFront();
    }

    public void HideDialogue()
    {
        _blockingDialogue = false;
        _dialogueLines = null;
        _dialogueComplete = null;

        if (dialogueRoot != null) dialogueRoot.gameObject.SetActive(false);
        if (dialogueCanvasGroup != null)
        {
            dialogueCanvasGroup.interactable = false;
            dialogueCanvasGroup.blocksRaycasts = false;
        }
    }

    public void ShowToast(string message, float durationSeconds = 2f)
    {
        EnsureBuilt();

        if (_toastRoutine != null) StopCoroutine(_toastRoutine);
        _toastRoutine = StartCoroutine(ToastRoutine(message, durationSeconds));
    }

    public void HideToast()
    {
        if (_toastRoutine != null)
        {
            StopCoroutine(_toastRoutine);
            _toastRoutine = null;
        }

        if (toastRoot != null) toastRoot.gameObject.SetActive(false);
    }

    public void HideAll()
    {
        HideHighlight();
        HideObjective();
        HideDialogue();
        HideToast();
    }

    public void SetDarkOverlayOpacity(float opacity)
    {
        darkOverlayOpacity = Mathf.Clamp01(opacity);
        ApplyDarkOverlayOpacity();
    }

    private void AdvanceDialogue()
    {
        if (!_blockingDialogue || _dialogueLines == null) return;

        _dialogueIndex++;
        if (_dialogueIndex < _dialogueLines.Length)
        {
            ShowDialogueLine(_dialogueLines[_dialogueIndex]);
            return;
        }

        var complete = _dialogueComplete;
        HideDialogue();
        complete?.Invoke();
    }

    private void ShowDialogueLine(TutorialDialogueLine line)
    {
        string speaker = line != null ? line.SpeakerId : null;
        string text = line != null ? line.Text : string.Empty;

        if (speakerText != null)
        {
            speakerText.gameObject.SetActive(!string.IsNullOrEmpty(speaker));
            speakerText.text = speaker;
        }

        if (dialogueText != null) dialogueText.text = text;
    }

    private IEnumerator ToastRoutine(string message, float durationSeconds)
    {
        if (toastText != null) toastText.text = message;
        if (toastRoot != null) toastRoot.gameObject.SetActive(true);
        BringToFront();

        yield return new WaitForSeconds(Mathf.Max(0.1f, durationSeconds));

        if (toastRoot != null) toastRoot.gameObject.SetActive(false);
        _toastRoutine = null;
    }

    private void RefreshHighlightFrame()
    {
        if (!_highlightVisible || highlightFrame == null) return;

        if (!TryGetTargetRect(out var center, out var size))
        {
            highlightFrame.gameObject.SetActive(false);
            SetSpotlightActive(false);
            return;
        }

        var paddedSize = size + highlightPadding;

        highlightFrame.gameObject.SetActive(true);
        highlightFrame.anchoredPosition = center;
        highlightFrame.sizeDelta = paddedSize;

        if (spotlightOverlay != null) spotlightOverlay.SetCutout(center, paddedSize);
        SetSpotlightActive(true);
    }

    private bool TryGetTargetRect(out Vector2 center, out Vector2 size)
    {
        if (_uiTarget != null && _uiTarget.gameObject.activeInHierarchy)
            return TryGetUiTargetRect(_uiTarget, out center, out size);

        if (_worldTarget != null && _worldTarget.gameObject.activeInHierarchy)
            return TryGetWorldTargetRect(_worldTarget, out center, out size);

        center = Vector2.zero;
        size = Vector2.zero;
        return false;
    }

    private bool TryGetUiTargetRect(RectTransform target, out Vector2 center, out Vector2 size)
    {
        target.GetWorldCorners(_rectCorners);

        Vector2 min = new Vector2(float.MaxValue, float.MaxValue);
        Vector2 max = new Vector2(float.MinValue, float.MinValue);
        for (int i = 0; i < _rectCorners.Length; i++)
        {
            Vector2 screen = RectTransformUtility.WorldToScreenPoint(GetUiCamera(), _rectCorners[i]);
            if (!ScreenToCanvasLocal(screen, out var local)) continue;
            min = Vector2.Min(min, local);
            max = Vector2.Max(max, local);
        }

        if (min.x == float.MaxValue)
        {
            center = Vector2.zero;
            size = Vector2.zero;
            return false;
        }

        center = (min + max) * 0.5f;
        size = max - min;
        return true;
    }

    private bool TryGetWorldTargetRect(Transform target, out Vector2 center, out Vector2 size)
    {
        Camera camera = Camera.main;
        if (camera == null)
        {
            center = Vector2.zero;
            size = Vector2.zero;
            return false;
        }

        if (!TryGetWorldBounds(target, out var bounds))
        {
            Vector3 screenPoint = camera.WorldToScreenPoint(target.position);
            if (screenPoint.z < 0f)
            {
                center = Vector2.zero;
                size = Vector2.zero;
                return false;
            }

            if (!ScreenToCanvasLocal(screenPoint, out center))
            {
                size = Vector2.zero;
                return false;
            }

            size = worldTargetFallbackSize;
            return true;
        }

        FillBoundsCorners(bounds);

        Vector2 min = new Vector2(float.MaxValue, float.MaxValue);
        Vector2 max = new Vector2(float.MinValue, float.MinValue);
        for (int i = 0; i < _boundsCorners.Length; i++)
        {
            Vector3 screenPoint = camera.WorldToScreenPoint(_boundsCorners[i]);
            if (screenPoint.z < 0f) continue;
            if (!ScreenToCanvasLocal(screenPoint, out var local)) continue;

            min = Vector2.Min(min, local);
            max = Vector2.Max(max, local);
        }

        if (min.x == float.MaxValue)
        {
            center = Vector2.zero;
            size = Vector2.zero;
            return false;
        }

        center = (min + max) * 0.5f;
        size = max - min;
        return true;
    }

    private bool TryGetWorldBounds(Transform target, out Bounds bounds)
    {
        // Prefer the node visual's collider AABB: renderer.bounds spans the sprite's
        // full texture rect (transparent pixels included) and overshoots the art.
        var mapNode = target.GetComponentInParent<MapNode>();
        if (mapNode != null && mapNode.TryGetVisualColliderBounds(out bounds))
            return true;

        var renderer = target.GetComponentInChildren<Renderer>();
        if (renderer != null)
        {
            bounds = renderer.bounds;
            return true;
        }

        var collider2D = target.GetComponentInChildren<Collider2D>();
        if (collider2D != null)
        {
            bounds = collider2D.bounds;
            return true;
        }

        bounds = new Bounds();
        return false;
    }

    private void FillBoundsCorners(Bounds bounds)
    {
        Vector3 min = bounds.min;
        Vector3 max = bounds.max;

        _boundsCorners[0] = new Vector3(min.x, min.y, min.z);
        _boundsCorners[1] = new Vector3(min.x, min.y, max.z);
        _boundsCorners[2] = new Vector3(min.x, max.y, min.z);
        _boundsCorners[3] = new Vector3(min.x, max.y, max.z);
        _boundsCorners[4] = new Vector3(max.x, min.y, min.z);
        _boundsCorners[5] = new Vector3(max.x, min.y, max.z);
        _boundsCorners[6] = new Vector3(max.x, max.y, min.z);
        _boundsCorners[7] = new Vector3(max.x, max.y, max.z);
    }

    private bool ScreenToCanvasLocal(Vector2 screenPoint, out Vector2 localPoint)
    {
        if (root == null)
        {
            localPoint = Vector2.zero;
            return false;
        }

        return RectTransformUtility.ScreenPointToLocalPointInRectangle(
            root,
            screenPoint,
            GetUiCamera(),
            out localPoint);
    }

    private Camera GetUiCamera()
    {
        if (canvas == null || canvas.renderMode == RenderMode.ScreenSpaceOverlay) return null;
        return canvas.worldCamera;
    }

    private void EnsureBuilt()
    {
        if (canvas == null) canvas = GetComponentInParent<Canvas>();
        if (canvas == null) canvas = FindRootCanvas();
        if (canvas == null) canvas = CreateFallbackCanvas();
        ConfigureCanvas();

        if (root == null) root = transform as RectTransform;
        if (root == null || root.GetComponentInParent<Canvas>() == null)
        {
            var rootObject = new GameObject("Day1TutorialUIRoot", typeof(RectTransform));
            rootObject.transform.SetParent(canvas.transform, false);
            root = rootObject.GetComponent<RectTransform>();
        }

        if (root.GetComponentInParent<Canvas>() == null)
            root.SetParent(canvas.transform, false);

        Stretch(root);
        root.SetAsLastSibling();

        if (spotlightOverlay == null)
            spotlightOverlay = GetComponentInChildren<Day1TutorialSpotlightOverlay>(true);
        if (spotlightOverlay == null)
            spotlightOverlay = CreateSpotlightOverlay("SpotlightOverlay", root, Color.black);

        if (spotlightOverlay.transform.parent != root)
            spotlightOverlay.transform.SetParent(root, false);

        Stretch(spotlightOverlay.rectTransform);
        spotlightOverlay.transform.SetAsFirstSibling();
        spotlightOverlay.raycastTarget = false;

        if (darkenOverlay != null)
        {
            darkenOverlay.raycastTarget = false;
            darkenOverlay.gameObject.SetActive(false);
        }

        ApplyDarkOverlayOpacity();

        if (highlightFrame == null) highlightFrame = CreateHighlightFrame(root);
        if (objectiveRoot == null) CreateObjective();
        if (dialogueRoot == null) CreateDialogue();
        if (toastRoot == null) CreateToast();
    }

    private Canvas FindRootCanvas()
    {
        var canvases = FindObjectsOfType<Canvas>();
        foreach (var candidate in canvases)
            if (candidate != null && candidate.isRootCanvas) return candidate;

        return canvases.Length > 0 ? canvases[0] : null;
    }

    private Canvas CreateFallbackCanvas()
    {
        var canvasObject = new GameObject("TutorialCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        var fallbackCanvas = canvasObject.GetComponent<Canvas>();
        fallbackCanvas.renderMode = RenderMode.ScreenSpaceOverlay;

        var scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080f, 1920f);
        scaler.matchWidthOrHeight = 0.5f;
        return fallbackCanvas;
    }

    private void ConfigureCanvas()
    {
        if (canvas == null) return;

        canvas.overrideSorting = true;
        canvas.sortingOrder = sortingOrder;
    }

    private void ApplyDarkOverlayOpacity()
    {
        if (spotlightOverlay != null)
        {
            Color spotlightColor = spotlightOverlay.color;
            spotlightColor.r = 0f;
            spotlightColor.g = 0f;
            spotlightColor.b = 0f;
            spotlightColor.a = darkOverlayOpacity;
            spotlightOverlay.color = spotlightColor;
        }

        if (darkenOverlay == null) return;

        Color color = darkenOverlay.color;
        color.r = 0f;
        color.g = 0f;
        color.b = 0f;
        color.a = darkOverlayOpacity;
        darkenOverlay.color = color;
    }

    private void CreateObjective()
    {
        objectiveRoot = CreatePanel("ObjectivePill", root, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
            new Vector2(0.5f, 1f), new Vector2(620f, 76f), new Vector2(0f, -140f),
            new Color(0.08f, 0.1f, 0.13f, 0.92f));
        objectiveRoot.GetComponent<Image>().raycastTarget = false;

        objectiveText = CreateText("ObjectiveText", objectiveRoot, 30f, FontStyles.Bold, TextAlignmentOptions.Center);
        StretchWithPadding(objectiveText.rectTransform, 28f, 12f);
        objectiveText.raycastTarget = false;

        // TODO: Add subtle slide-in animation.
        // TODO: Add small icon beside objective text.
        // TODO: Add objective completion tick effect.
    }

    private void CreateDialogue()
    {
        dialogueRoot = CreatePanel("DialoguePanel", root, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
            new Vector2(0.5f, 0f), new Vector2(900f, 210f), new Vector2(0f, 72f),
            new Color(0.06f, 0.07f, 0.08f, 0.94f));

        dialogueCanvasGroup = dialogueRoot.gameObject.AddComponent<CanvasGroup>();
        dialogueButton = dialogueRoot.gameObject.AddComponent<Button>();
        dialogueButton.targetGraphic = dialogueRoot.GetComponent<Image>();
        dialogueButton.onClick.AddListener(AdvanceDialogue);

        speakerText = CreateText("SpeakerText", dialogueRoot, 24f, FontStyles.Bold, TextAlignmentOptions.Left);
        speakerText.rectTransform.anchorMin = new Vector2(0f, 1f);
        speakerText.rectTransform.anchorMax = new Vector2(1f, 1f);
        speakerText.rectTransform.pivot = new Vector2(0.5f, 1f);
        speakerText.rectTransform.anchoredPosition = new Vector2(0f, -18f);
        speakerText.rectTransform.sizeDelta = new Vector2(-56f, 34f);
        speakerText.raycastTarget = false;

        dialogueText = CreateText("DialogueText", dialogueRoot, 32f, FontStyles.Normal, TextAlignmentOptions.Left);
        dialogueText.enableWordWrapping = true;
        dialogueText.rectTransform.anchorMin = new Vector2(0f, 0f);
        dialogueText.rectTransform.anchorMax = new Vector2(1f, 1f);
        dialogueText.rectTransform.offsetMin = new Vector2(28f, 24f);
        dialogueText.rectTransform.offsetMax = new Vector2(-28f, -58f);
        dialogueText.raycastTarget = false;

        // TODO: Add typewriter text effect.
        // TODO: Add speaker portrait.
        // TODO: Add dialogue panel fade in/out.
        // TODO: Add tap indicator animation.
    }

    private void CreateToast()
    {
        toastRoot = CreatePanel("TutorialToast", root, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
            new Vector2(0.5f, 1f), new Vector2(520f, 66f), new Vector2(0f, -238f),
            new Color(0.1f, 0.12f, 0.16f, 0.95f));
        toastRoot.GetComponent<Image>().raycastTarget = false;

        toastText = CreateText("ToastText", toastRoot, 28f, FontStyles.Bold, TextAlignmentOptions.Center);
        StretchWithPadding(toastText.rectTransform, 24f, 10f);
        toastText.raycastTarget = false;

        // TODO: Add toast slide-in animation.
        // TODO: Add small Journal icon.
        // TODO: Add Journal button pulse.
        // TODO: Add soft notification sound.
    }

    private RectTransform CreateHighlightFrame(RectTransform parent)
    {
        var frameObject = new GameObject("HighlightFrame", typeof(RectTransform));
        frameObject.transform.SetParent(parent, false);
        var frame = frameObject.GetComponent<RectTransform>();
        frame.anchorMin = new Vector2(0.5f, 0.5f);
        frame.anchorMax = new Vector2(0.5f, 0.5f);
        frame.pivot = new Vector2(0.5f, 0.5f);

        CreateFrameEdge("Top", frame, true, true);
        CreateFrameEdge("Bottom", frame, true, false);
        CreateFrameEdge("Left", frame, false, false);
        CreateFrameEdge("Right", frame, false, true);

        // TODO: Add pulsing ring around highlighted target.
        // TODO: Add arrow pointer toward highlighted target.
        // TODO: Add smooth fade in/out for dark overlay.
        // TODO: Add small bounce animation on target node.

        return frame;
    }

    private void CreateFrameEdge(string name, RectTransform parent, bool horizontal, bool maxSide)
    {
        const float thickness = 8f;

        var edge = CreateImage(name, parent, new Color(1f, 0.86f, 0.3f, 1f));
        edge.raycastTarget = false;
        var rect = edge.rectTransform;
        if (horizontal)
        {
            rect.anchorMin = new Vector2(0f, maxSide ? 1f : 0f);
            rect.anchorMax = new Vector2(1f, maxSide ? 1f : 0f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(0f, thickness);
        }
        else
        {
            rect.anchorMin = new Vector2(maxSide ? 1f : 0f, 0f);
            rect.anchorMax = new Vector2(maxSide ? 1f : 0f, 1f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(thickness, 0f);
        }
    }

    private RectTransform CreatePanel(string name, RectTransform parent, Vector2 anchorMin, Vector2 anchorMax,
        Vector2 pivot, Vector2 size, Vector2 anchoredPosition, Color color)
    {
        var image = CreateImage(name, parent, color);
        var rect = image.rectTransform;
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.pivot = pivot;
        rect.sizeDelta = size;
        rect.anchoredPosition = anchoredPosition;
        return rect;
    }

    private Image CreateImage(string name, RectTransform parent, Color color)
    {
        var imageObject = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        imageObject.transform.SetParent(parent, false);

        var image = imageObject.GetComponent<Image>();
        image.color = color;
        return image;
    }

    private Day1TutorialSpotlightOverlay CreateSpotlightOverlay(string name, RectTransform parent, Color color)
    {
        var overlayObject = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Day1TutorialSpotlightOverlay));
        overlayObject.transform.SetParent(parent, false);

        var overlay = overlayObject.GetComponent<Day1TutorialSpotlightOverlay>();
        overlay.color = color;
        return overlay;
    }

    private TextMeshProUGUI CreateText(string name, RectTransform parent, float fontSize, FontStyles style,
        TextAlignmentOptions alignment)
    {
        var textObject = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        textObject.transform.SetParent(parent, false);

        var text = textObject.GetComponent<TextMeshProUGUI>();
        text.fontSize = fontSize;
        text.fontStyle = style;
        text.alignment = alignment;
        text.color = Color.white;
        text.overflowMode = TextOverflowModes.Ellipsis;
        text.raycastTarget = false;
        return text;
    }

    private void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    private void StretchWithPadding(RectTransform rect, float horizontal, float vertical)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = new Vector2(horizontal, vertical);
        rect.offsetMax = new Vector2(-horizontal, -vertical);
    }

    private void BringToFront()
    {
        if (root != null) root.SetAsLastSibling();
    }

    private void SetSpotlightActive(bool active)
    {
        if (spotlightOverlay != null)
        {
            if (!active) spotlightOverlay.ClearCutout();
            spotlightOverlay.gameObject.SetActive(active);
        }

        if (darkenOverlay != null) darkenOverlay.gameObject.SetActive(active && spotlightOverlay == null);
    }
}
