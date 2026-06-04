using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class SaveManager : MonoBehaviour
{
    public static SaveManager Instance { get; private set; }

    [SerializeField] private float autoSaveInterval = 60f;

    private const string SAVE_FILE_NAME = "savegame.json";

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void Start()
    {
        if (HasSave()) LoadGame();

        if (autoSaveInterval > 0f)
            StartCoroutine(AutoSaveRoutine());
    }

    private void OnApplicationPause(bool pauseStatus)
    {
        if (pauseStatus) SaveGame();
    }

    private void OnApplicationQuit() => SaveGame();

    public void SaveGame()
    {
        SaveData data = CollectSaveData();
        data.saveDate = System.DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss");

        string json = JsonUtility.ToJson(data, true);

        try
        {
            File.WriteAllText(GetSavePath(), json);
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[SaveManager] Save error: {e.Message}");
        }
    }

    public void LoadGame()
    {
        string path = GetSavePath();
        if (!File.Exists(path)) return;

        try
        {
            string json = File.ReadAllText(path);
            SaveData data = JsonUtility.FromJson<SaveData>(json);
            if (data != null) ApplySaveData(data);
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[SaveManager] Load error: {e.Message}");
        }
    }

    public void DeleteSave()
    {
        string path = GetSavePath();
        if (File.Exists(path)) File.Delete(path);
    }

    public bool HasSave() => File.Exists(GetSavePath());

    private SaveData CollectSaveData()
    {
        SaveData data = new SaveData();

        if (GameManager.Instance != null)
        {
            data.playerMoney = GameManager.Instance.PlayerMoney;

            List<FruitData> allFruits = GameManager.Instance.GetAllFruits();
            List<bool> activeStates = GameManager.Instance.GetFruitActiveStates();

            for (int i = 0; i < allFruits.Count; i++)
            {
                data.fruitStates.Add(new FruitSaveData
                {
                    fruitName = allFruits[i].FruitName,
                    isActive = activeStates[i],
                    basePrice = allFruits[i].BasePrice,
                    regrowthDuration = allFruits[i].RegrowthDuration
                });
            }
        }

        MonoBehaviour[] allBehaviours = FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None);
        foreach (MonoBehaviour mb in allBehaviours)
        {
            if (mb is ISaveable saveable)
            {
                object state = saveable.CaptureState();

                if (state is HarvestZoneSaveData harvestData)
                    data.harvestZones.Add(harvestData);
                else if (state is UnlockZoneSaveData unlockData)
                    data.unlockZones.Add(unlockData);
            }
        }

        return data;
    }

    private void ApplySaveData(SaveData data)
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.SetMoney(data.playerMoney);

            foreach (FruitSaveData fruitSave in data.fruitStates)
            {
                FruitData fruit = GameManager.Instance.FindFruitByName(fruitSave.fruitName);
                if (fruit != null)
                {
                    GameManager.Instance.SetFruitActive(fruit, fruitSave.isActive);
                    fruit.SetBasePrice(fruitSave.basePrice);
                    fruit.SetRegrowthDuration(fruitSave.regrowthDuration);
                }
            }
        }

        MonoBehaviour[] allBehaviours = FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None);
        foreach (MonoBehaviour mb in allBehaviours)
        {
            if (mb is ISaveable saveable)
            {
                string id = saveable.SaveID;

                HarvestZoneSaveData harvestMatch = data.harvestZones.Find(h => h.saveID == id);
                if (harvestMatch != null)
                {
                    saveable.RestoreState(harvestMatch);
                    continue;
                }

                UnlockZoneSaveData unlockMatch = data.unlockZones.Find(u => u.saveID == id);
                if (unlockMatch != null)
                    saveable.RestoreState(unlockMatch);
            }
        }
    }

    private IEnumerator AutoSaveRoutine()
    {
        while (true)
        {
            yield return new WaitForSeconds(autoSaveInterval);
            SaveGame();
        }
    }

    private string GetSavePath() => Path.Combine(Application.persistentDataPath, SAVE_FILE_NAME);
}
