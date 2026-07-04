using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEngine;

public static class DialogSetupEditor
{
    private const string PopupDir = "Assets/8.Resources/Resources/UI/Popups/";

    [MenuItem("Tools/Dialog/Full Setup (Run All)")]
    public static void FullSetup()
    {
        EnsureDirectory();
        CreateCharacterData();
        CreatePhaseData();
        CreateDatabase();
        CreatePrefabWithReferences();
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[DialogSetup] Full setup complete!");
    }

    private static void EnsureDirectory()
    {
        if (!AssetDatabase.IsValidFolder(PopupDir.TrimEnd('/')))
        {
            var parent = "Assets/8.Resources/Resources/UI";
            if (!AssetDatabase.IsValidFolder(parent))
                AssetDatabase.CreateFolder("Assets/8.Resources/Resources", "UI");
            AssetDatabase.CreateFolder(parent, "Popups");
        }
    }

    private static void CreateCharacterData()
    {
        var list = new[]
        {
            (CharacterId.None, "Boy", Color.cyan),
            // (CharacterId.Girl, "Girl", Color.magenta),
            // (CharacterId.OldMan, "Old Man", Color.yellow),
            // (CharacterId.Witch, "Witch", Color.green),
            // (CharacterId.Monster, "Monster", Color.red),
        };

        foreach (var (id, name, color) in list)
        {
            var data = ScriptableObject.CreateInstance<CharacterDataSO>();
            data.characterId = id;
            data.displayName = name;
            data.nameColor = color;
            AssetDatabase.CreateAsset(data, $"{PopupDir}Char_{id}.asset");
        }
    }

    private static void CreatePhaseData()
    {
        // Phase Intro
        var intro = ScriptableObject.CreateInstance<DialogPhaseSO>();
        intro.phaseId = PhaseId.Intro;
        // intro.entries = new List<DialogEntry>
        // {
        //     new() { characterId = CharacterId.Boy, description = "Hello! Welcome to the forest." },
        //     new() { characterId = CharacterId.Girl, description = "Be careful, there are monsters around!" },
        //     new() { characterId = CharacterId.Boy, description = "I'll protect you!" },
        // };
        // AssetDatabase.CreateAsset(intro, $"{PopupDir}Phase_Intro.asset");
        //
        // // Phase Forest_01
        // var f1 = ScriptableObject.CreateInstance<DialogPhaseSO>();
        // f1.phaseId = PhaseId.Forest_01;
        // f1.entries = new List<DialogEntry>
        // {
        //     new() { characterId = CharacterId.OldMan, description = "Traveler, are you lost?" },
        //     new() { characterId = CharacterId.Boy, description = "Yes! Can you help us find the way out?" },
        //     new() { characterId = CharacterId.OldMan, description = "Follow the river east. It will lead you to the village." },
        // };
        // AssetDatabase.CreateAsset(f1, $"{PopupDir}Phase_Forest_01.asset");
    }

    private static void CreateDatabase()
    {
        var db = ScriptableObject.CreateInstance<DialogDatabaseSO>();
        AssetDatabase.CreateAsset(db, $"{PopupDir}DialogDatabase.asset");

        var so = new SerializedObject(db);
        var phases = so.FindProperty("phases");
        phases.ClearArray();

        var phase1 = AssetDatabase.LoadAssetAtPath<DialogPhaseSO>($"{PopupDir}Phase_Intro.asset");
        var phase2 = AssetDatabase.LoadAssetAtPath<DialogPhaseSO>($"{PopupDir}Phase_Forest_01.asset");

        if (phase1 != null)
        {
            phases.InsertArrayElementAtIndex(0);
            phases.GetArrayElementAtIndex(0).objectReferenceValue = phase1;
        }
        if (phase2 != null)
        {
            phases.InsertArrayElementAtIndex(1);
            phases.GetArrayElementAtIndex(1).objectReferenceValue = phase2;
        }
        so.ApplyModifiedProperties();
    }

    private static void CreatePrefabWithReferences()
    {
        // Load assets
        var charAssets = new List<CharacterDataSO>();
        foreach (CharacterId id in System.Enum.GetValues(typeof(CharacterId)))
        {
            if (id == CharacterId.None) continue;
            var asset = AssetDatabase.LoadAssetAtPath<CharacterDataSO>($"{PopupDir}Char_{id}.asset");
            if (asset != null) charAssets.Add(asset);
        }

        // Create temp GO
        var go = new GameObject("DialogPopup", typeof(RectTransform), typeof(CanvasGroup), typeof(DialogPopup));

        var rect = go.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        var canvasGroup = go.GetComponent<CanvasGroup>();
        canvasGroup.alpha = 0;
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;

        // Entry Container
        var container = new GameObject("EntryContainer", typeof(RectTransform));
        var cr = container.GetComponent<RectTransform>();
        cr.SetParent(go.transform, false);
        cr.anchorMin = new Vector2(0, 0.05f);
        cr.anchorMax = new Vector2(1, 0.95f);
        cr.offsetMin = Vector2.zero;
        cr.offsetMax = Vector2.zero;
        cr.pivot = new Vector2(0.5f, 0f);

        // Background
        var bg = new GameObject("Background", typeof(RectTransform));
        var br = bg.GetComponent<RectTransform>();
        br.SetParent(go.transform, false);
        br.anchorMin = Vector2.zero;
        br.anchorMax = Vector2.one;
        br.offsetMin = Vector2.zero;
        br.offsetMax = Vector2.zero;
        var bgImage = bg.AddComponent<UnityEngine.UI.Image>();
        bgImage.color = new Color(0, 0, 0, 0.7f);
        bg.transform.SetAsFirstSibling();

        // Tap hint
        var hintGo = new GameObject("TapHint", typeof(RectTransform));
        var hr = hintGo.GetComponent<RectTransform>();
        hr.SetParent(go.transform, false);
        hr.anchorMin = new Vector2(0.5f, 0);
        hr.anchorMax = new Vector2(0.5f, 0);
        hr.pivot = new Vector2(0.5f, 0);
        hr.anchoredPosition = new Vector2(0, 15);
        var hintTmp = hintGo.AddComponent<TextMeshProUGUI>();
        hintTmp.text = "Tap to continue";
        hintTmp.fontSize = 22;
        hintTmp.alignment = TMPro.TextAlignmentOptions.Center;
        hintTmp.color = Color.gray;

        // Entry prefab
        var entryGo = new GameObject("DialogEntryItem", typeof(RectTransform));
        entryGo.SetActive(false);
        var er = entryGo.GetComponent<RectTransform>();
        er.anchorMin = new Vector2(0, 0);
        er.anchorMax = new Vector2(1, 0);
        er.pivot = new Vector2(0.5f, 0);
        er.sizeDelta = new Vector2(0, 160);
        entryGo.AddComponent<CanvasGroup>();

        // Icon
        var iconGo = new GameObject("Icon", typeof(RectTransform));
        var ir = iconGo.GetComponent<RectTransform>();
        ir.SetParent(entryGo.transform, false);
        ir.anchorMin = new Vector2(0, 0.5f);
        ir.anchorMax = new Vector2(0, 0.5f);
        ir.pivot = new Vector2(0.5f, 0.5f);
        ir.sizeDelta = new Vector2(80, 80);
        ir.anchoredPosition = new Vector2(60, 0);
        var iconImg = iconGo.AddComponent<UnityEngine.UI.Image>();
        iconImg.color = Color.white;

        // Name
        var nameGo = new GameObject("NameText", typeof(RectTransform));
        var nr = nameGo.GetComponent<RectTransform>();
        nr.SetParent(entryGo.transform, false);
        nr.anchorMin = new Vector2(0, 1);
        nr.anchorMax = new Vector2(1, 1);
        nr.pivot = new Vector2(0, 1);
        nr.offsetMin = new Vector2(120, -35);
        nr.offsetMax = new Vector2(-20, 0);
        var nameTmp = nameGo.AddComponent<TextMeshProUGUI>();
        nameTmp.text = "Name";
        nameTmp.fontSize = 28;
        nameTmp.fontStyle = TMPro.FontStyles.Bold;
        nameTmp.alignment = TMPro.TextAlignmentOptions.Left;

        // Description
        var descGo = new GameObject("DescriptionText", typeof(RectTransform));
        var dr = descGo.GetComponent<RectTransform>();
        dr.SetParent(entryGo.transform, false);
        dr.anchorMin = new Vector2(0, 0);
        dr.anchorMax = new Vector2(1, 0.75f);
        dr.pivot = new Vector2(0, 1);
        dr.offsetMin = new Vector2(120, 10);
        dr.offsetMax = new Vector2(-20, 0);
        var descTmp = descGo.AddComponent<TextMeshProUGUI>();
        descTmp.text = "Description";
        descTmp.fontSize = 22;
        descTmp.alignment = TMPro.TextAlignmentOptions.TopLeft;
        descTmp.enableWordWrapping = true;

        var entryItem = entryGo.AddComponent<DialogEntryItem>();
        var eSo = new SerializedObject(entryItem);
        eSo.FindProperty("_icon").objectReferenceValue = iconImg;
        eSo.FindProperty("_nameText").objectReferenceValue = nameTmp;
        eSo.FindProperty("_descriptionText").objectReferenceValue = descTmp;
        eSo.ApplyModifiedProperties();

        var entryPrefab = PrefabUtility.SaveAsPrefabAsset(entryGo, $"{PopupDir}DialogEntryItem.prefab");
        Object.DestroyImmediate(entryGo);

        // Setup DialogPopup
        var popup = go.GetComponent<DialogPopup>();
        var pSo = new SerializedObject(popup);
        pSo.FindProperty("popupId").stringValue = "DialogPopup";
        pSo.FindProperty("canvasGroup").objectReferenceValue = canvasGroup;
        pSo.FindProperty("_content").objectReferenceValue = go.transform;
        pSo.FindProperty("_entryContainer").objectReferenceValue = cr;
        pSo.FindProperty("_entryPrefab").objectReferenceValue = entryPrefab.GetComponent<DialogEntryItem>();
        pSo.FindProperty("_tapHintText").objectReferenceValue = hintTmp;

        // Assign character data list
        var charList = pSo.FindProperty("_characterDataList");
        charList.ClearArray();
        for (int i = 0; i < charAssets.Count; i++)
        {
            charList.InsertArrayElementAtIndex(i);
            charList.GetArrayElementAtIndex(i).objectReferenceValue = charAssets[i];
        }

        // Assign database reference
        var db = AssetDatabase.LoadAssetAtPath<DialogDatabaseSO>($"{PopupDir}DialogDatabase.asset");
        if (db != null)
            pSo.FindProperty("_database").objectReferenceValue = db;

        pSo.ApplyModifiedProperties();

        PrefabUtility.SaveAsPrefabAsset(go, $"{PopupDir}DialogPopup.prefab");
        Object.DestroyImmediate(go);
    }
}
