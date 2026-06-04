using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [System.Serializable]
    public class FruitActivationSetting
    {
        public FruitData fruit;

        public bool isActive = true;
    }

    [SerializeField] private List<FruitActivationSetting> fruitSettings = new();

    private int playerMoney = 0;

    public int PlayerMoney => playerMoney;

    public event System.Action<int> OnMoneyChanged;

    public void AddMoney(int amount)
    {
        if (amount <= 0) return;
        playerMoney += amount;
        OnMoneyChanged?.Invoke(playerMoney);
    }

    public bool RemoveMoney(int amount)
    {
        if (amount <= 0) return false;

        if (playerMoney >= amount)
        {
            playerMoney -= amount;
            OnMoneyChanged?.Invoke(playerMoney);
            return true;
        }
        return false;
    }

    private void Awake() => Instance = this;


    public List<FruitData> GetActiveFruits()
    {
        List<FruitData> activeFruits = new List<FruitData>();

        foreach (var setting in fruitSettings)
        {
            if (setting.isActive) activeFruits.Add(setting.fruit);

        }

        return activeFruits;
    }

    public void SetFruitActive(FruitData fruit, bool active)
    {

        FruitActivationSetting setting = fruitSettings.Find(s => s.fruit == fruit);
        setting.isActive = active;
    }
}
