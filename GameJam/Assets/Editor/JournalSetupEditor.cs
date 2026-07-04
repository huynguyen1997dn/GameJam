using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// One-click setup for the Journal feature, so no UI hierarchy or serialized
/// reference has to be wired by hand:
///   GameJam/Journal/1. Create Sample Journal Data — database + example entries/clues
///     under Assets/8.Resources/Resources/Journal (the path JournalManager loads from).
///   GameJam/Journal/2. Build Journal UI In Scene — JournalSystem root (manager +
///     cheats), the map-HUD Journal button and the full overlay, all references
///     assigned. Targets the selected Canvas, else the one holding DayHUD.
/// Everything it creates is plain placeholder UI (built-in sprites, TMP default
/// font) meant to be restyled in the editor afterwards.
/// </summary>
public static class JournalSetupEditor
{
    private const string JournalFolder = "Assets/8.Resources/Resources/Journal";
    private const string DatabasePath = JournalFolder + "/JournalDatabase.asset";

    // Placeholder palette — parchment panel, dark ink text.
    private static readonly Color Parchment = new Color(0.949f, 0.898f, 0.788f);
    private static readonly Color ParchmentDark = new Color(0.906f, 0.835f, 0.686f);
    private static readonly Color Ink = new Color(0.25f, 0.2f, 0.15f);
    private static readonly Color InkMuted = new Color(0.25f, 0.2f, 0.15f, 0.55f);
    private static readonly Color DimColor = new Color(0f, 0f, 0f, 0.65f);
    private static readonly Color UnreadRed = new Color(0.85f, 0.25f, 0.2f);

    // ==================================================================
    //  MENU 1 — SAMPLE DATA
    // ==================================================================

    [MenuItem("GameJam/Journal/1. Create Sample Journal Data")]
    public static void CreateSampleData()
    {
        EnsureFolder("Assets/8.Resources/Resources", "Journal");
        EnsureFolder(JournalFolder, "Entries");
        EnsureFolder(JournalFolder, "Clues");

        // Older sample layout had the arrival story as a Day 1 page; it is the D0
        // prologue now, so drop the obsolete asset if it exists.
        AssetDatabase.DeleteAsset($"{JournalFolder}/Entries/day_01_arrival.asset");

        var entries = new List<JournalEntryData>
        {
            // D0 — the only page the player sees when the game starts. Day pages
            // unlock one day late: finishing Day N writes the DN entry.
            Entry("day_00_prologue", 0, "The Village That Forgets",
                "I arrived at dusk. They greeted me warmly — then greeted me again an hour " +
                "later, as if for the first time.\n\n" +
                "Every dawn wipes them clean. Names, debts, promises. Only the village itself " +
                "keeps the score: doors I opened stay open, paths I cleared stay clear.\n\n" +
                "If I want answers, I will have to leave marks the morning cannot erase.",
                proof: null, unlockedFromStart: true,
                "clue_dawn_reset", "clue_paper_remembers"),

            Entry("day_01_first_traces", 1, "First Traces",
                "A fallen tree blocked the path to the square. I cut it clear before sundown.\n\n" +
                "Nobody helped — they could not understand why it mattered. By tomorrow they " +
                "will not remember it ever lay there.\n\n" +
                "But the path stays open. That is how I will speak to them: not in words, " +
                "in changes.",
                proof: "The road stays clear, though no one remembers the tree.",
                unlockedFromStart: false,
                "clue_fresh_cut"),

            Entry("day_02_the_well", 2, "Water From The Old Well",
                "Nobody could say why the well stood dry — they simply accepted it, the way " +
                "they accept everything each morning.\n\n" +
                "The rope was down in the shaft. Hauling it up took most of the day, and by " +
                "evening the buckets were full again.\n\n" +
                "Tomorrow they will drink and not ask who fixed it. But the water will still " +
                "be there.",
                proof: "The well keeps giving water, though no one remembers it dry.",
                unlockedFromStart: false,
                "clue_well_rope", "clue_stranger_boots"),

            Entry("day_03_bridge_repaired", 3, "The Bridge Was Repaired",
                "They do not remember who built the bridge.\n" +
                "But this morning, people crossed the river for the first time.\n\n" +
                "The market is louder now.\n" +
                "Someone brought tools from outside the village.",
                proof: "The village has changed.", unlockedFromStart: false,
                "clue_bridge_connects", "clue_old_carpenter", "clue_tool_marks"),

            Entry("day_04_festival", 4, "The Festival Nobody Planned",
                "Lanterns are being strung between the houses, and not one villager can say " +
                "who decided there should be a festival.\n\n" +
                "Perhaps the village decided for them.",
                proof: null, unlockedFromStart: false,
                "clue_lantern_oil"),
        };

        var clues = new List<JournalClueData>
        {
            Clue("clue_dawn_reset", "day_00_prologue", "Memories reset at dawn. Objects do not."),
            Clue("clue_paper_remembers", "day_00_prologue", "Paper remembers. Write everything down."),
            Clue("clue_fresh_cut", "day_01_first_traces", "The stump is cut clean — that tree was felled, not blown down."),
            Clue("clue_well_rope", "day_02_the_well", "The well rope was cut, not worn — someone did it on purpose."),
            Clue("clue_stranger_boots", "day_02_the_well", "Muddy boot prints lead from the well toward the forest."),
            Clue("clue_bridge_connects", "day_03_bridge_repaired", "The bridge connects the old temple and the well."),
            Clue("clue_old_carpenter", "day_03_bridge_repaired", "Ask about the old carpenter."),
            Clue("clue_tool_marks", "day_03_bridge_repaired", "Tool marks were found near the riverbank."),
            Clue("clue_lantern_oil", "day_04_festival", "The lantern oil smells of the same resin as the bridge planks."),
        };

        var database = CreateOrLoad<JournalDatabase>(DatabasePath);
        database.EditorSetContent(entries, clues);
        EditorUtility.SetDirty(database);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Selection.activeObject = database;
        Debug.Log($"[JournalSetup] Sample data ready at {JournalFolder} " +
                  $"({entries.Count} entries, {clues.Count} clues).");
    }

    private static JournalEntryData Entry(string id, int day, string title, string body,
        string proof, bool unlockedFromStart, params string[] clueIds)
    {
        var entry = CreateOrLoad<JournalEntryData>($"{JournalFolder}/Entries/{id}.asset");
        entry.entryId = id;
        entry.dayNumber = day;
        entry.title = title;
        entry.bodyText = body;
        entry.proofText = proof;
        entry.unlockedFromStart = unlockedFromStart;
        entry.clueIds = new List<string>(clueIds);
        EditorUtility.SetDirty(entry);
        return entry;
    }

    private static JournalClueData Clue(string id, string entryId, string text)
    {
        var clue = CreateOrLoad<JournalClueData>($"{JournalFolder}/Clues/{id}.asset");
        clue.clueId = id;
        clue.entryId = entryId;
        clue.text = text;
        EditorUtility.SetDirty(clue);
        return clue;
    }

    private static T CreateOrLoad<T>(string path) where T : ScriptableObject
    {
        var asset = AssetDatabase.LoadAssetAtPath<T>(path);
        if (asset != null) return asset;
        asset = ScriptableObject.CreateInstance<T>();
        AssetDatabase.CreateAsset(asset, path);
        return asset;
    }

    private static void EnsureFolder(string parent, string child)
    {
        if (!AssetDatabase.IsValidFolder($"{parent}/{child}"))
            AssetDatabase.CreateFolder(parent, child);
    }

    // ==================================================================
    //  MENU 2 — SCENE UI
    // ==================================================================

    [MenuItem("GameJam/Journal/2. Build Journal UI In Scene")]
    public static void BuildJournalUI()
    {
        if (Object.FindAnyObjectByType<JournalUIView>(FindObjectsInactive.Include) != null)
        {
            EditorUtility.DisplayDialog("Journal Setup",
                "A JournalUIView already exists in the scene. Delete it first if you want " +
                "the hierarchy regenerated.", "OK");
            return;
        }

        var canvas = FindTargetCanvas();
        if (canvas == null)
        {
            EditorUtility.DisplayDialog("Journal Setup",
                "No Canvas found. Select the map UI Canvas (the one holding DayHUD) and run " +
                "this again.", "OK");
            return;
        }

        var database = AssetDatabase.LoadAssetAtPath<JournalDatabase>(DatabasePath);
        if (database == null)
        {
            // A journal without pages is useless in a demo — bootstrap the samples.
            CreateSampleData();
            database = AssetDatabase.LoadAssetAtPath<JournalDatabase>(DatabasePath);
        }

        var systemGO = BuildJournalSystem(database);
        var buttonGO = BuildMapButton(canvas.transform);
        var overlayGO = BuildOverlay(canvas.transform);

        // The day transition must keep rendering above everything, journal included.
        var transition = Object.FindAnyObjectByType<DayTransitionOverlay>(FindObjectsInactive.Include);
        if (transition != null && transition.transform.parent == canvas.transform)
            transition.transform.SetAsLastSibling();

        EditorSceneManager.MarkSceneDirty(canvas.gameObject.scene);
        Selection.activeGameObject = overlayGO;
        Debug.Log($"[JournalSetup] Built '{systemGO.name}', '{buttonGO.name}' and " +
                  $"'{overlayGO.name}' under canvas '{canvas.name}'.");
    }

    private static Canvas FindTargetCanvas()
    {
        if (Selection.activeGameObject != null)
        {
            var selected = Selection.activeGameObject.GetComponentInParent<Canvas>();
            if (selected != null) return selected;
        }

        var dayHud = Object.FindAnyObjectByType<DayHUD>(FindObjectsInactive.Include);
        if (dayHud != null)
        {
            var hudCanvas = dayHud.GetComponentInParent<Canvas>();
            if (hudCanvas != null) return hudCanvas;
        }

        return Object.FindAnyObjectByType<Canvas>(FindObjectsInactive.Include);
    }

    private static GameObject BuildJournalSystem(JournalDatabase database)
    {
        var manager = Object.FindAnyObjectByType<JournalManager>(FindObjectsInactive.Include);
        GameObject go;
        if (manager == null)
        {
            go = new GameObject("JournalSystem");
            Undo.RegisterCreatedObjectUndo(go, "Create Journal System");
            manager = go.AddComponent<JournalManager>();
            go.AddComponent<JournalDebugCheats>();
        }
        else
        {
            go = manager.gameObject;
        }

        var so = new SerializedObject(manager);
        so.FindProperty("database").objectReferenceValue = database;
        so.ApplyModifiedPropertiesWithoutUndo();
        return go;
    }

    // ---------------------- MAP BUTTON ----------------------

    private static GameObject BuildMapButton(Transform canvas)
    {
        var buttonRT = NewRect("JournalButton", canvas);
        Place(buttonRT, new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(1f, 0f),
            new Vector2(-30f, 320f), new Vector2(150f, 150f));
        Undo.RegisterCreatedObjectUndo(buttonRT.gameObject, "Create Journal Button");
        var mapButton = buttonRT.gameObject.AddComponent<JournalMapButton>();

        var rootRT = NewRect("Root", buttonRT);
        StretchFull(rootRT);

        var bgRT = NewRect("Bg", rootRT);
        StretchFull(bgRT);
        var bgImage = AddSpriteImage(bgRT, "UISprite.psd", Parchment);
        var button = bgRT.gameObject.AddComponent<Button>();
        button.targetGraphic = bgImage;

        var icon = AddText(bgRT, "Icon", "J", 72, Ink, TextAlignmentOptions.Center, FontStyles.Bold);
        StretchFull((RectTransform)icon.transform);

        var dotRT = NewRect("UnreadDot", rootRT);
        Place(dotRT, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(1f, 1f),
            new Vector2(-4f, -4f), new Vector2(44f, 44f));
        AddSpriteImage(dotRT, "Knob.psd", UnreadRed).raycastTarget = false;
        dotRT.gameObject.SetActive(false);

        var so = new SerializedObject(mapButton);
        so.FindProperty("root").objectReferenceValue = rootRT.gameObject;
        so.FindProperty("button").objectReferenceValue = button;
        so.FindProperty("unreadIndicator").objectReferenceValue = dotRT.gameObject;
        so.ApplyModifiedPropertiesWithoutUndo();

        return buttonRT.gameObject;
    }

    // ---------------------- OVERLAY ----------------------

    private static GameObject BuildOverlay(Transform canvas)
    {
        var overlayRT = NewRect("JournalOverlay", canvas);
        StretchFull(overlayRT);
        Undo.RegisterCreatedObjectUndo(overlayRT.gameObject, "Create Journal Overlay");
        var view = overlayRT.gameObject.AddComponent<JournalUIView>();
        var canvasGroup = overlayRT.gameObject.AddComponent<CanvasGroup>();

        var rootRT = NewRect("Root", overlayRT);
        StretchFull(rootRT);

        // Dim — full-screen click catcher behind the panel.
        var dimRT = NewRect("Dim", rootRT);
        StretchFull(dimRT);
        var dimImage = AddPlainImage(dimRT, DimColor);
        var dimButton = dimRT.gameObject.AddComponent<Button>();
        dimButton.targetGraphic = dimImage;
        dimButton.transition = Selectable.Transition.None;

        // Panel — parchment sheet covering most of the portrait screen.
        var panelRT = NewRect("Panel", rootRT);
        Stretch(panelRT, new Vector2(0.035f, 0.025f), new Vector2(0.965f, 0.94f));
        AddSpriteImage(panelRT, "Background.psd", Parchment);

        var header = BuildHeader(panelRT);
        var content = BuildContent(panelRT);
        var emptyState = BuildEmptyState(panelRT);
        var footer = BuildFooter(panelRT);
        var toast = BuildLockedToast(panelRT);
        var sidebar = BuildSidebar(rootRT); // after Panel so it draws on top

        var so = new SerializedObject(view);
        so.FindProperty("root").objectReferenceValue = rootRT.gameObject;
        so.FindProperty("canvasGroup").objectReferenceValue = canvasGroup;
        so.FindProperty("closeButton").objectReferenceValue = header.closeButton;
        so.FindProperty("dayListButton").objectReferenceValue = header.dayListButton;
        so.FindProperty("dimButton").objectReferenceValue = dimButton;
        so.FindProperty("contentRoot").objectReferenceValue = content.root;
        so.FindProperty("entryTitleText").objectReferenceValue = content.titleText;
        so.FindProperty("illustrationImage").objectReferenceValue = content.illustrationImage;
        so.FindProperty("illustrationPlaceholder").objectReferenceValue = content.illustrationPlaceholder;
        so.FindProperty("bodyText").objectReferenceValue = content.bodyText;
        so.FindProperty("clueSection").objectReferenceValue = content.clueSection;
        so.FindProperty("clueEmptyText").objectReferenceValue = content.clueEmptyText;
        so.FindProperty("clueContainer").objectReferenceValue = content.clueContainer;
        so.FindProperty("clueItemTemplate").objectReferenceValue = content.clueItemTemplate;
        so.FindProperty("proofSection").objectReferenceValue = content.proofSection;
        so.FindProperty("proofText").objectReferenceValue = content.proofText;
        so.FindProperty("contentScroll").objectReferenceValue = content.scrollRect;
        so.FindProperty("emptyStateRoot").objectReferenceValue = emptyState.root;
        so.FindProperty("emptyStateText").objectReferenceValue = emptyState.text;
        so.FindProperty("previousButton").objectReferenceValue = footer.previousButton;
        so.FindProperty("nextButton").objectReferenceValue = footer.nextButton;
        so.FindProperty("footerDayLabel").objectReferenceValue = footer.dayLabel;
        so.FindProperty("sidebarRoot").objectReferenceValue = sidebar.root;
        so.FindProperty("dayButtonContainer").objectReferenceValue = sidebar.container;
        so.FindProperty("dayButtonTemplate").objectReferenceValue = sidebar.template;
        so.FindProperty("lockedToast").objectReferenceValue = toast.root;
        so.FindProperty("lockedToastText").objectReferenceValue = toast.text;
        so.ApplyModifiedPropertiesWithoutUndo();

        // Left visible in the editor for styling; JournalUIView.Awake resets to the
        // hidden rest state on play.
        return overlayRT.gameObject;
    }

    private struct HeaderParts { public Button dayListButton; public Button closeButton; }

    private static HeaderParts BuildHeader(RectTransform panel)
    {
        var headerRT = NewRect("Header", panel);
        Place(headerRT, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f),
            Vector2.zero, new Vector2(0f, 130f));

        var dayListRT = NewRect("DayListButton", headerRT);
        Place(dayListRT, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f),
            new Vector2(24f, 0f), new Vector2(100f, 100f));
        var dayListButton = MakeSquareButton(dayListRT, "=");

        var title = AddText(headerRT, "TitleText", "JOURNAL", 54, Ink,
            TextAlignmentOptions.Center, FontStyles.Bold);
        StretchFull((RectTransform)title.transform);
        title.characterSpacing = 8f;

        var closeRT = NewRect("CloseButton", headerRT);
        Place(closeRT, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(1f, 0.5f),
            new Vector2(-24f, 0f), new Vector2(100f, 100f));
        var closeButton = MakeSquareButton(closeRT, "×");

        return new HeaderParts { dayListButton = dayListButton, closeButton = closeButton };
    }

    private struct ContentParts
    {
        public GameObject root;
        public TextMeshProUGUI titleText;
        public ScrollRect scrollRect;
        public Image illustrationImage;
        public GameObject illustrationPlaceholder;
        public TextMeshProUGUI bodyText;
        public GameObject clueSection;
        public TextMeshProUGUI clueEmptyText;
        public Transform clueContainer;
        public JournalClueItem clueItemTemplate;
        public GameObject proofSection;
        public TextMeshProUGUI proofText;
    }

    private static ContentParts BuildContent(RectTransform panel)
    {
        var parts = new ContentParts();

        var contentRootRT = NewRect("ContentRoot", panel);
        Stretch(contentRootRT, Vector2.zero, Vector2.one, new Vector2(0f, 120f), new Vector2(0f, -130f));
        parts.root = contentRootRT.gameObject;

        // Entry title line: "Day 3 — The Bridge Was Repaired".
        parts.titleText = AddText(contentRootRT, "EntryTitle", "Day 1 — Title", 44, Ink,
            TextAlignmentOptions.Center, FontStyles.Bold);
        var titleRT = (RectTransform)parts.titleText.transform;
        Place(titleRT, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f),
            Vector2.zero, new Vector2(-72f, 90f));
        parts.titleText.enableAutoSizing = true;
        parts.titleText.fontSizeMin = 26f;
        parts.titleText.fontSizeMax = 44f;

        // Scroll area with everything else stacked vertically inside.
        var scrollRT = NewRect("ContentScroll", contentRootRT);
        Stretch(scrollRT, Vector2.zero, Vector2.one, new Vector2(28f, 10f), new Vector2(-28f, -100f));
        parts.scrollRect = scrollRT.gameObject.AddComponent<ScrollRect>();
        parts.scrollRect.horizontal = false;
        parts.scrollRect.scrollSensitivity = 30f;

        var viewportRT = NewRect("Viewport", scrollRT);
        StretchFull(viewportRT);
        // Invisible but raycastable so drags anywhere in the page reach the ScrollRect.
        AddPlainImage(viewportRT, new Color(1f, 1f, 1f, 0f));
        viewportRT.gameObject.AddComponent<RectMask2D>();

        var scrollContentRT = NewRect("Content", viewportRT);
        Place(scrollContentRT, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f),
            Vector2.zero, new Vector2(0f, 0f));
        var layout = scrollContentRT.gameObject.AddComponent<VerticalLayoutGroup>();
        ConfigureLayout(layout, new RectOffset(8, 8, 8, 30), 26f);
        var fitter = scrollContentRT.gameObject.AddComponent<ContentSizeFitter>();
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        parts.scrollRect.viewport = viewportRT;
        parts.scrollRect.content = scrollContentRT;

        // Illustration slot (fixed height, placeholder shown when the entry has none).
        var illustrationRT = NewRect("Illustration", scrollContentRT);
        illustrationRT.gameObject.AddComponent<LayoutElement>().preferredHeight = 420f;

        var illustrationImageRT = NewRect("IllustrationImage", illustrationRT);
        StretchFull(illustrationImageRT);
        parts.illustrationImage = AddPlainImage(illustrationImageRT, Color.white);
        parts.illustrationImage.preserveAspect = true;
        parts.illustrationImage.raycastTarget = false;
        parts.illustrationImage.enabled = false;

        var placeholderRT = NewRect("IllustrationPlaceholder", illustrationRT);
        StretchFull(placeholderRT);
        AddSpriteImage(placeholderRT, "Background.psd", ParchmentDark).raycastTarget = false;
        var placeholderText = AddText(placeholderRT, "Label", "( no illustration )", 30, InkMuted,
            TextAlignmentOptions.Center, FontStyles.Italic);
        StretchFull((RectTransform)placeholderText.transform);
        parts.illustrationPlaceholder = placeholderRT.gameObject;

        // Body text.
        parts.bodyText = AddText(scrollContentRT, "BodyText", "Body text...", 38, Ink,
            TextAlignmentOptions.TopLeft);

        // Clue box.
        var clueSectionRT = NewRect("ClueSection", scrollContentRT);
        AddSpriteImage(clueSectionRT, "Background.psd", ParchmentDark).raycastTarget = false;
        var clueLayout = clueSectionRT.gameObject.AddComponent<VerticalLayoutGroup>();
        ConfigureLayout(clueLayout, new RectOffset(24, 24, 20, 24), 14f);
        parts.clueSection = clueSectionRT.gameObject;

        AddText(clueSectionRT, "ClueHeader", "Clues", 36, Ink,
            TextAlignmentOptions.TopLeft, FontStyles.Bold);
        parts.clueEmptyText = AddText(clueSectionRT, "ClueEmptyText", "No clues recorded.", 32,
            InkMuted, TextAlignmentOptions.TopLeft, FontStyles.Italic);

        var clueContainerRT = NewRect("ClueContainer", clueSectionRT);
        var clueContainerLayout = clueContainerRT.gameObject.AddComponent<VerticalLayoutGroup>();
        ConfigureLayout(clueContainerLayout, new RectOffset(0, 0, 0, 0), 12f);
        parts.clueContainer = clueContainerRT;

        var clueItemText = AddText(clueContainerRT, "ClueItemTemplate", "• Clue text", 34, Ink,
            TextAlignmentOptions.TopLeft);
        var clueItem = clueItemText.gameObject.AddComponent<JournalClueItem>();
        var clueItemSO = new SerializedObject(clueItem);
        clueItemSO.FindProperty("clueText").objectReferenceValue = clueItemText;
        clueItemSO.ApplyModifiedPropertiesWithoutUndo();
        clueItemText.gameObject.SetActive(false);
        parts.clueItemTemplate = clueItem;

        // Proof box ("the world remembers").
        var proofSectionRT = NewRect("ProofSection", scrollContentRT);
        AddSpriteImage(proofSectionRT, "Background.psd",
            new Color(0.878f, 0.784f, 0.612f)).raycastTarget = false;
        var proofLayout = proofSectionRT.gameObject.AddComponent<VerticalLayoutGroup>();
        ConfigureLayout(proofLayout, new RectOffset(24, 24, 20, 24), 10f);
        parts.proofSection = proofSectionRT.gameObject;

        AddText(proofSectionRT, "ProofHeader", "Proof", 36, Ink,
            TextAlignmentOptions.TopLeft, FontStyles.Bold);
        parts.proofText = AddText(proofSectionRT, "ProofText", "Proof text...", 34, Ink,
            TextAlignmentOptions.TopLeft, FontStyles.Italic);

        return parts;
    }

    private struct EmptyStateParts { public GameObject root; public TextMeshProUGUI text; }

    private static EmptyStateParts BuildEmptyState(RectTransform panel)
    {
        var emptyRT = NewRect("EmptyState", panel);
        Stretch(emptyRT, Vector2.zero, Vector2.one, new Vector2(60f, 130f), new Vector2(-60f, -140f));
        var text = AddText(emptyRT, "Message",
            "No journal entry yet.\nExplore the village to leave your first trace.",
            36, InkMuted, TextAlignmentOptions.Center, FontStyles.Italic);
        StretchFull((RectTransform)text.transform);
        emptyRT.gameObject.SetActive(false);
        return new EmptyStateParts { root = emptyRT.gameObject, text = text };
    }

    private struct FooterParts
    {
        public Button previousButton;
        public Button nextButton;
        public TextMeshProUGUI dayLabel;
    }

    private static FooterParts BuildFooter(RectTransform panel)
    {
        var footerRT = NewRect("FooterNavigation", panel);
        Place(footerRT, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0.5f, 0f),
            new Vector2(0f, 10f), new Vector2(0f, 100f));

        var prevRT = NewRect("PreviousButton", footerRT);
        Place(prevRT, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f),
            new Vector2(28f, 0f), new Vector2(100f, 90f));
        var previousButton = MakeSquareButton(prevRT, "<");

        var dayLabel = AddText(footerRT, "DayLabel", "D1", 42, Ink,
            TextAlignmentOptions.Center, FontStyles.Bold);
        StretchFull((RectTransform)dayLabel.transform);

        var nextRT = NewRect("NextButton", footerRT);
        Place(nextRT, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(1f, 0.5f),
            new Vector2(-28f, 0f), new Vector2(100f, 90f));
        var nextButton = MakeSquareButton(nextRT, ">");

        return new FooterParts { previousButton = previousButton, nextButton = nextButton, dayLabel = dayLabel };
    }

    private struct ToastParts { public GameObject root; public TextMeshProUGUI text; }

    private static ToastParts BuildLockedToast(RectTransform panel)
    {
        var toastRT = NewRect("LockedToast", panel);
        Place(toastRT, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
            new Vector2(0f, 140f), new Vector2(720f, 90f));
        AddSpriteImage(toastRT, "UISprite.psd", new Color(0.1f, 0.08f, 0.06f, 0.9f))
            .raycastTarget = false;
        var text = AddText(toastRT, "Message", "This page has not been written yet.", 32,
            new Color(0.95f, 0.92f, 0.85f), TextAlignmentOptions.Center);
        StretchFull((RectTransform)text.transform);
        toastRT.gameObject.SetActive(false);
        return new ToastParts { root = toastRT.gameObject, text = text };
    }

    private struct SidebarParts
    {
        public GameObject root;
        public Transform container;
        public JournalDayButton template;
    }

    private static SidebarParts BuildSidebar(RectTransform overlayRoot)
    {
        var sidebarRT = NewRect("DayListSidebar", overlayRoot);
        sidebarRT.anchorMin = new Vector2(0.035f, 0.025f);
        sidebarRT.anchorMax = new Vector2(0.035f, 0.94f);
        sidebarRT.pivot = new Vector2(0f, 0.5f);
        sidebarRT.anchoredPosition = Vector2.zero;
        sidebarRT.sizeDelta = new Vector2(280f, 0f);
        AddSpriteImage(sidebarRT, "Background.psd", new Color(0.878f, 0.816f, 0.671f));

        var header = AddText(sidebarRT, "SidebarHeader", "DAYS", 34, Ink,
            TextAlignmentOptions.Center, FontStyles.Bold);
        var headerRT = (RectTransform)header.transform;
        Place(headerRT, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f),
            new Vector2(0f, -16f), new Vector2(0f, 60f));

        var containerRT = NewRect("DayButtonContainer", sidebarRT);
        Stretch(containerRT, Vector2.zero, Vector2.one, new Vector2(20f, 20f), new Vector2(-20f, -90f));
        var layout = containerRT.gameObject.AddComponent<VerticalLayoutGroup>();
        ConfigureLayout(layout, new RectOffset(0, 0, 0, 0), 14f);
        layout.childAlignment = TextAnchor.UpperCenter;

        var template = BuildDayButtonTemplate(containerRT);

        sidebarRT.gameObject.SetActive(false);
        return new SidebarParts
        {
            root = sidebarRT.gameObject,
            container = containerRT,
            template = template,
        };
    }

    private static JournalDayButton BuildDayButtonTemplate(RectTransform container)
    {
        var buttonRT = NewRect("DayButtonTemplate", container);
        buttonRT.gameObject.AddComponent<LayoutElement>().preferredHeight = 100f;
        var dayButton = buttonRT.gameObject.AddComponent<JournalDayButton>();

        var bgImage = AddSpriteImage(buttonRT, "UISprite.psd", ParchmentDark);
        var button = buttonRT.gameObject.AddComponent<Button>();
        button.targetGraphic = bgImage;

        var highlightRT = NewRect("SelectedHighlight", buttonRT);
        StretchFull(highlightRT);
        AddSpriteImage(highlightRT, "UISprite.psd", new Color(1f, 0.85f, 0.5f, 0.55f))
            .raycastTarget = false;
        highlightRT.gameObject.SetActive(false);

        var label = AddText(buttonRT, "Label", "D1", 40, Ink,
            TextAlignmentOptions.Center, FontStyles.Bold);
        StretchFull((RectTransform)label.transform);

        var lockRT = NewRect("LockIcon", buttonRT);
        StretchFull(lockRT);
        AddSpriteImage(lockRT, "UISprite.psd", new Color(0f, 0f, 0f, 0.25f)).raycastTarget = false;
        lockRT.gameObject.SetActive(false);

        var dotRT = NewRect("UnreadDot", buttonRT);
        Place(dotRT, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(1f, 1f),
            new Vector2(-6f, -6f), new Vector2(30f, 30f));
        AddSpriteImage(dotRT, "Knob.psd", UnreadRed).raycastTarget = false;
        dotRT.gameObject.SetActive(false);

        var so = new SerializedObject(dayButton);
        so.FindProperty("button").objectReferenceValue = button;
        so.FindProperty("label").objectReferenceValue = label;
        so.FindProperty("selectedHighlight").objectReferenceValue = highlightRT.gameObject;
        so.FindProperty("lockIcon").objectReferenceValue = lockRT.gameObject;
        so.FindProperty("unreadDot").objectReferenceValue = dotRT.gameObject;
        so.ApplyModifiedPropertiesWithoutUndo();

        buttonRT.gameObject.SetActive(false);
        return dayButton;
    }

    // ==================================================================
    //  MENU 3 — INSTALL INTO GAMEPLAYVIEW (UIManager flow)
    // ==================================================================

    private const string GamePlayViewPath = "Assets/8.Resources/Resources/UI/Views/GamePlayView.prefab";
    private const string DaySystemUIPath = "Assets/5.Prefabs/NavigatingMap/DaySystemUI.prefab";

    // Moves the map-screen UI under UIManager control: everything becomes part of the
    // GamePlayView prefab, which UIManager instantiates/shows/hides. After running
    // this, delete the scene copies of DaySystemUI, JournalButton, JournalOverlay and
    // DayTransitionOverlay — but KEEP JournalSystem (manager, not UI) in the scene.
    [MenuItem("GameJam/Journal/3. Install Map UI Into GamePlayView Prefab")]
    public static void InstallMapUIIntoGamePlayView()
    {
        if (AssetDatabase.LoadAssetAtPath<GameObject>(GamePlayViewPath) == null)
        {
            EditorUtility.DisplayDialog("Journal Setup",
                $"GamePlayView prefab not found at:\n{GamePlayViewPath}", "OK");
            return;
        }

        var root = PrefabUtility.LoadPrefabContents(GamePlayViewPath);
        try
        {
            var rootT = root.transform;

            // Day HUD comes in as a nested DaySystemUI prefab so it keeps its own asset.
            var dayHud = root.GetComponentInChildren<DayHUD>(true);
            Transform daySystem = dayHud != null ? TopLevelChildOf(dayHud.transform, rootT) : null;
            if (daySystem == null)
            {
                var daySystemPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(DaySystemUIPath);
                if (daySystemPrefab != null)
                {
                    var instance = (GameObject)PrefabUtility.InstantiatePrefab(daySystemPrefab, rootT);
                    daySystem = instance.transform;
                    StretchFull((RectTransform)daySystem);
                }
                else
                {
                    Debug.LogWarning($"[JournalSetup] DaySystemUI prefab not found at {DaySystemUIPath} — skipped.");
                }
            }

            var mapButton = root.GetComponentInChildren<JournalMapButton>(true);
            var buttonGO = mapButton != null ? mapButton.gameObject : BuildMapButton(rootT);

            var journalView = root.GetComponentInChildren<JournalUIView>(true);
            var overlayGO = journalView != null ? journalView.gameObject : BuildOverlay(rootT);

            var transition = root.GetComponentInChildren<DayTransitionOverlay>(true);
            var transitionGO = transition != null ? transition.gameObject : BuildDayTransitionOverlay(rootT);

            // Render order (bottom → top): day HUD, journal button, journal overlay,
            // day transition — the darkened screen must cover everything, journal included.
            if (daySystem != null) daySystem.SetAsLastSibling();
            buttonGO.transform.SetAsLastSibling();
            overlayGO.transform.SetAsLastSibling();
            transitionGO.transform.SetAsLastSibling();

            PrefabUtility.SaveAsPrefabAsset(root, GamePlayViewPath);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }

        Debug.Log("[JournalSetup] Map UI installed into GamePlayView.prefab. Now delete the " +
                  "scene copies of DaySystemUI, JournalButton, JournalOverlay and " +
                  "DayTransitionOverlay — keep JournalSystem (and Managers) in the scene.");
    }

    /// <summary>Walks up from a nested object to the direct child of `root` containing it.</summary>
    private static Transform TopLevelChildOf(Transform descendant, Transform root)
    {
        var t = descendant;
        while (t.parent != null && t.parent != root) t = t.parent;
        return t.parent == root ? t : descendant;
    }

    // Mirrors the hand-built scene object: black full-screen image (raycastTarget stays
    // on — the CanvasGroup controls whether it swallows clicks), inactive DAY XX label.
    private static GameObject BuildDayTransitionOverlay(Transform parent)
    {
        var rt = NewRect("DayTransitionOverlay", parent);
        StretchFull(rt);
        AddPlainImage(rt, Color.black);
        var canvasGroup = rt.gameObject.AddComponent<CanvasGroup>();
        canvasGroup.alpha = 0f;
        canvasGroup.blocksRaycasts = false;

        var label = AddText(rt, "DayLabel", "DAY 01", 110, Color.white,
            TextAlignmentOptions.Center, FontStyles.Bold);
        StretchFull((RectTransform)label.transform);
        label.gameObject.SetActive(false);

        var overlay = rt.gameObject.AddComponent<DayTransitionOverlay>();
        var so = new SerializedObject(overlay);
        so.FindProperty("canvasGroup").objectReferenceValue = canvasGroup;
        so.FindProperty("dayLabel").objectReferenceValue = label;
        so.ApplyModifiedPropertiesWithoutUndo();
        return rt.gameObject;
    }

    // ==================================================================
    //  SMALL BUILD HELPERS
    // ==================================================================

    private static RectTransform NewRect(string name, Transform parent)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.layer = parent.gameObject.layer;
        var rt = (RectTransform)go.transform;
        rt.SetParent(parent, false);
        return rt;
    }

    private static void StretchFull(RectTransform rt) =>
        Stretch(rt, Vector2.zero, Vector2.one);

    private static void Stretch(RectTransform rt, Vector2 anchorMin, Vector2 anchorMax)
    {
        Stretch(rt, anchorMin, anchorMax, Vector2.zero, Vector2.zero);
    }

    private static void Stretch(RectTransform rt, Vector2 anchorMin, Vector2 anchorMax,
        Vector2 offsetMin, Vector2 offsetMax)
    {
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.offsetMin = offsetMin;
        rt.offsetMax = offsetMax;
    }

    private static void Place(RectTransform rt, Vector2 anchorMin, Vector2 anchorMax,
        Vector2 pivot, Vector2 anchoredPosition, Vector2 sizeDelta)
    {
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.pivot = pivot;
        rt.anchoredPosition = anchoredPosition;
        rt.sizeDelta = sizeDelta;
    }

    private static Image AddPlainImage(RectTransform rt, Color color)
    {
        var image = rt.gameObject.AddComponent<Image>();
        image.color = color;
        return image;
    }

    private static Image AddSpriteImage(RectTransform rt, string builtinSprite, Color color)
    {
        var image = rt.gameObject.AddComponent<Image>();
        image.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>($"UI/Skin/{builtinSprite}");
        image.type = Image.Type.Sliced;
        image.color = color;
        return image;
    }

    private static TextMeshProUGUI AddText(RectTransform parent, string name, string text,
        float fontSize, Color color, TextAlignmentOptions alignment,
        FontStyles style = FontStyles.Normal)
    {
        var rt = NewRect(name, parent);
        var tmp = rt.gameObject.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = fontSize;
        tmp.color = color;
        tmp.alignment = alignment;
        tmp.fontStyle = style;
        tmp.raycastTarget = false;
        return tmp;
    }

    private static Button MakeSquareButton(RectTransform rt, string label)
    {
        var bgImage = AddSpriteImage(rt, "UISprite.psd", ParchmentDark);
        var button = rt.gameObject.AddComponent<Button>();
        button.targetGraphic = bgImage;
        var text = AddText(rt, "Label", label, 52, Ink, TextAlignmentOptions.Center, FontStyles.Bold);
        StretchFull((RectTransform)text.transform);
        return button;
    }

    private static void ConfigureLayout(VerticalLayoutGroup layout, RectOffset padding, float spacing)
    {
        layout.padding = padding;
        layout.spacing = spacing;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;
    }
}
