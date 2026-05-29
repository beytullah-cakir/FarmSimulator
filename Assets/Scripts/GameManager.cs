using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [System.Serializable]
    public class FruitActivationSetting
    {
        [Tooltip("The FruitData asset.")]
        public FruitData fruit;

        [Tooltip("Is this fruit active (unlocked) in the game? Customers will only request active fruits.")]
        public bool isActive = true;
    }

    [Header("Fruit Unlock Settings")]
    [Tooltip("Manage which fruits are currently active/unlocked in the game.")]
    [SerializeField] private List<FruitActivationSetting> fruitSettings = new List<FruitActivationSetting>();

    [Header("Economy Settings")]
    [Tooltip("The player's total current money balance.")]
    [SerializeField] private int playerMoney = 0;

    public int PlayerMoney => playerMoney;

    // Fired whenever money increases or decreases. Passes new money balance.
    public event System.Action<int> OnMoneyChanged;

    /// <summary>
    /// Adds money to the player's total balance and updates UIs.
    /// </summary>
    public void AddMoney(int amount)
    {
        if (amount <= 0) return;
        playerMoney += amount;
        Debug.Log($"[GameManager] Para eklendi: +{amount}. Toplam Para: {playerMoney}");
        OnMoneyChanged?.Invoke(playerMoney);
    }

    /// <summary>
    /// Deducts money from the player's total balance. Returns true if successful, false if insufficient funds.
    /// </summary>
    public bool RemoveMoney(int amount)
    {
        if (amount <= 0) return false;
        
        if (playerMoney >= amount)
        {
            playerMoney -= amount;
            Debug.Log($"[GameManager] Para harcandı: -{amount}. Kalan Para: {playerMoney}");
            OnMoneyChanged?.Invoke(playerMoney);
            return true;
        }

        Debug.LogWarning($"[GameManager] Yetersiz bakiye! Harcanmak istenen: {amount}, Mevcut: {playerMoney}");
        return false;
    }

    private void Awake()
    {
        // Singleton initialization
        if (Instance == null)
        {
            Instance = this;
            // Optional: Keep GameManager alive between scenes if needed
            // DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// Returns a list of all currently active (unlocked) fruits.
    /// </summary>
    public List<FruitData> GetActiveFruits()
    {
        List<FruitData> activeFruits = new List<FruitData>();
        
        foreach (var setting in fruitSettings)
        {
            if (setting != null && setting.isActive && setting.fruit != null)
            {
                activeFruits.Add(setting.fruit);
            }
        }

        return activeFruits;
    }

    /// <summary>
    /// Call this programmatically to unlock/lock a fruit during gameplay (e.g. when buying a new field/stand).
    /// </summary>
    /// <param name="fruit">The FruitData to unlock/lock.</param>
    /// <param name="active">True to activate (unlock) the fruit, false to lock.</param>
    public void SetFruitActive(FruitData fruit, bool active)
    {
        if (fruit == null) return;

        FruitActivationSetting setting = fruitSettings.Find(s => s.fruit == fruit);
        if (setting != null)
        {
            setting.isActive = active;
            Debug.Log($"[GameManager] {fruit.FruitName} aktif durumu güncellendi: {active}");
        }
        else
        {
            // If the fruit is not in the list, dynamically add it
            FruitActivationSetting newSetting = new FruitActivationSetting
            {
                fruit = fruit,
                isActive = active
            };
            fruitSettings.Add(newSetting);
            Debug.Log($"[GameManager] Yeni meyve {fruit.FruitName} eklendi ve aktif edildi: {active}");
        }
    }
}
