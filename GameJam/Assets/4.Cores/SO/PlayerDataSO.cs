#if UNITY_EDITOR
using UnityEditor;
#endif
using System;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

[CreateAssetMenu(fileName = "PlayerDataSO", menuName = "Configs/Player Data")]
public class PlayerDataSO : ScriptableObject
{
    public Action<PlayerData> OnPlayerDataChanged;
    [SerializeField] private PlayerData Value;

    public void Clean()
    {
        Value.Clean();
    }
    

    [Header("Asset")]
    private const string PREF_KEY = "PLAYER_DATA";

    public void Save()
    {
        string json = JsonUtility.ToJson(Value);
        PlayerPrefs.SetString(PREF_KEY, json);
        PlayerPrefs.Save();
#if UNITY_EDITOR
        Debug.Log("PlayerDataSO saved: " + json);
#endif
        DispathEvent();
    }

    [Button]
    public void DispathEvent()
    {
        OnPlayerDataChanged?.Invoke(Value);
    }

    public void Load()
    {
        if (PlayerPrefs.HasKey(PREF_KEY))
        {
            string json = PlayerPrefs.GetString(PREF_KEY);
            Value = JsonUtility.FromJson<PlayerData>(json);

#if UNITY_EDITOR
            Debug.Log("PlayerDataSO loaded: " + json);
#endif
            return;
        }

        Debug.LogError("PlayerDataSO loaded: null");

        ClearData();
        Save();
    }

    public void ClearData()
    {
        Clean();
        
        PlayerPrefs.DeleteKey(PREF_KEY);
        PlayerPrefs.Save();
    }

    private void OnApplicationPause(bool pause)
    {
        if (pause)
        {
            Save();
        }
    }

    private void OnApplicationQuit()
    {
        Save();
    }

    public void CheckAndUpdateDayStreak()
    {
    }

    [Serializable]
    public class PlayerData
    {
        public int level;
        public bool useTimeMode;
        public int totalScore;

        public void Clean()
        {
            level = 1;
            useTimeMode = false;
            totalScore = 0;
        }
    }

    [Serializable]
    public class PlayerInventoryItem
    {
        public int itemId;
        public int amount;
    }

    public int GetLevel()
    {
        return Value.level;
    }

    public void PassLevel()
    {
        Value.level++;
        Save();
    }

    public bool GetUseTimeMode()
    {
        return Value.useTimeMode;
    }

    public void SetUseTimeMode(bool value)
    {
        Value.useTimeMode = value;
        Save();
    }

    public int GetScore()
    {
        return Value.totalScore;
    }

    public void AddScore(int amount)
    {
        Value.totalScore += amount;
        Save();
    }
}


#if UNITY_EDITOR
[CustomEditor(typeof(PlayerDataSO))]
public class PlayerDataSOEditor : Editor
{
    public override void OnInspectorGUI()
    {
        // Vẽ default inspector trước
        DrawDefaultInspector();

        PlayerDataSO data = (PlayerDataSO)target;

        GUILayout.Space(10);
        EditorGUILayout.LabelField("Editor Actions", EditorStyles.boldLabel);

        if (GUILayout.Button("Save Data (PlayerPrefs)"))
        {
            data.Save();
            Debug.Log("✅ PlayerDataSO saved to PlayerPrefs");
        }

        if (GUILayout.Button("Load Data (PlayerPrefs)"))
        {
            data.Load();
            Debug.Log("✅ PlayerDataSO loaded from PlayerPrefs");
        }

        if (GUILayout.Button("Dispath Event"))
        {
            // data.DispathEvent();
            Debug.Log("✅ PlayerDataSO DispathEvent");
        }

        if (GUILayout.Button("Clear Data"))
        {
            if (EditorUtility.DisplayDialog("Clear Player Data",
                    "Are you sure you want to clear saved player data?", "Yes", "No"))
            {
                data.ClearData();
                Debug.Log("🗑 PlayerDataSO cleared");
            }
        }
    }
}
#endif