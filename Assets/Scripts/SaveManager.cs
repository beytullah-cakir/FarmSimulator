using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class SaveManager : MonoBehaviour
{
    public static SaveManager Instance { get; private set; }

    [SerializeField] private float autoSaveInterval = 30f; // Her 30 saniyede bir otomatik kayit

    private const string SAVE_FILE_NAME = "savegame.json";

    private bool hasSaved = false; // Tekrar eden kaydi onler

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject); // Sahne gecislerinde yok edilmez
    }

    private void Start()
    {
        if (HasSave()) LoadGame();

        if (autoSaveInterval > 0f)
            StartCoroutine(AutoSaveRoutine());

        // Para her degistiginde kaydet
        if (GameManager.Instance != null)
            GameManager.Instance.OnMoneyChanged += OnMoneyChangedSave;
    }

    private void OnDestroy()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.OnMoneyChanged -= OnMoneyChangedSave;
    }

    private void OnMoneyChangedSave(int newAmount)
    {
        SaveGame();
    }

    private void OnApplicationPause(bool pauseStatus)
    {
        // Mobilde arka plana geçince kaydet
        if (pauseStatus)
        {
            hasSaved = false;
            SaveGame();
        }
    }

    private void OnApplicationFocus(bool hasFocus)
    {
        // Odak kaybedilince kaydet (masaüstü çıkış, alt+tab vb.)
        if (!hasFocus)
        {
            hasSaved = false;
            SaveGame();
        }
    }

    private void OnApplicationQuit()
    {
        hasSaved = false;
        SaveGame();
    }

    public void SaveGame()
    {
        SaveData data = CollectSaveData();
        data.saveDate = System.DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss");

        string json = JsonUtility.ToJson(data, true);
        string path = GetSavePath();

        try
        {
            File.WriteAllText(path, json);
            Debug.Log($"[SaveManager] Oyun kaydedildi: {path} ({data.saveDate})");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[SaveManager] Kayit hatasi: {e.Message}");
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

                if (state is UnlockZoneSaveData unlockData)
                    data.unlockZones.Add(unlockData);
            }
        }

        // Save UpgradeManager
        if (UpgradeManager.Instance != null)
        {
            foreach (var entry in UpgradeManager.Instance.GetAllEntries())
            {
                int activeCount = 0;
                foreach (var tree in entry.harvestZone.TargetTrees)
                {
                    if (tree != null && tree.gameObject.activeSelf) activeCount++;
                }

                data.upgradeEntries.Add(new UpgradeEntrySaveData
                {
                    entryName = entry.entryName,
                    incomeLevel = entry.incomeLevel,
                    speedLevel = entry.speedLevel,
                    incomeUpgradeCost = entry.incomeUpgradeCost,
                    speedUpgradeCost = entry.speedUpgradeCost,
                    treePurchaseCost = entry.treePurchaseCost,
                    activeTreeCount = activeCount
                });
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

                UnlockZoneSaveData unlockMatch = data.unlockZones.Find(u => u.saveID == id);
                if (unlockMatch != null)
                    saveable.RestoreState(unlockMatch);
            }
        }

        // Restore UpgradeManager
        if (UpgradeManager.Instance != null && data.upgradeEntries != null)
        {
            foreach (var savedEntry in data.upgradeEntries)
            {
                var entries = UpgradeManager.Instance.GetAllEntries();
                var entry = entries.Find(e => e.entryName == savedEntry.entryName);
                if (entry == null) continue;

                entry.incomeLevel = savedEntry.incomeLevel;
                entry.speedLevel = savedEntry.speedLevel;
                entry.incomeUpgradeCost = savedEntry.incomeUpgradeCost;
                entry.speedUpgradeCost = savedEntry.speedUpgradeCost;
                entry.treePurchaseCost = savedEntry.treePurchaseCost;

                for (int i = 0; i < entry.harvestZone.TargetTrees.Count; i++)
                {
                    if (entry.harvestZone.TargetTrees[i] != null)
                        entry.harvestZone.TargetTrees[i].gameObject.SetActive(i < savedEntry.activeTreeCount);
                }

                if (entry.harvestZone.TargetTrees.Count > 0 && entry.harvestZone.TargetTrees[0] != null)
                {
                    FruitData fruit = entry.harvestZone.TargetTrees[0].FruitData;
                    if (fruit != null)
                    {
                        foreach (Prop tree in entry.harvestZone.TargetTrees)
                        {
                            if (tree != null) tree.SetRegrowthDuration(fruit.RegrowthDuration);
                        }
                    }
                }
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
