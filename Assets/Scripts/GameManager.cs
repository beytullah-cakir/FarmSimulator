using System.Collections.Generic;
using UnityEngine;
using Unity.Services.Core;

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
    [SerializeField] private string appKey = "YOUR_APP_KEY";

    private int playerMoney = 0;

    public int PlayerMoney => playerMoney;

    public event System.Action<int> OnMoneyChanged;

    private void Awake() => Instance = this;

    public void AddMoney(int amount)
    {
        if (amount <= 0) return;
        playerMoney += amount;
        OnMoneyChanged?.Invoke(playerMoney);
    }

    public bool RemoveMoney(int amount)
    {
        if (amount <= 0) return false;
        if (playerMoney < amount) return false;

        playerMoney -= amount;
        OnMoneyChanged?.Invoke(playerMoney);
        return true;
    }

    public void SetMoney(int amount)
    {
        playerMoney = Mathf.Max(0, amount);
        OnMoneyChanged?.Invoke(playerMoney);
    }

    public List<FruitData> GetActiveFruits()
    {
        List<FruitData> activeFruits = new List<FruitData>();
        foreach (var setting in fruitSettings)
        {
            if (setting.isActive) activeFruits.Add(setting.fruit);
        }
        return activeFruits;
    }

    public List<FruitData> GetAllFruits()
    {
        List<FruitData> allFruits = new List<FruitData>();
        foreach (var setting in fruitSettings)
        {
            if (setting.fruit != null)
                allFruits.Add(setting.fruit);
        }
        return allFruits;
    }

    public List<bool> GetFruitActiveStates()
    {
        List<bool> states = new List<bool>();
        foreach (var setting in fruitSettings)
        {
            if (setting.fruit != null)
                states.Add(setting.isActive);
        }
        return states;
    }

    public FruitData FindFruitByName(string fruitName)
    {
        foreach (var setting in fruitSettings)
        {
            if (setting.fruit != null && setting.fruit.FruitName == fruitName)
                return setting.fruit;
        }
        return null;
    }

    public void SetFruitActive(FruitData fruit, bool active)
    {
        FruitActivationSetting setting = fruitSettings.Find(s => s.fruit == fruit);
        if (setting != null) setting.isActive = active;
    }

    public void PlayButtonSound()
    {
        if (AudioManager.Instance != null && AudioManager.Instance.buttonClick != null)
        {
            AudioManager.Instance.PlaySFX(AudioManager.Instance.buttonClick);
        }
    }
}
