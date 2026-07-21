using System.Collections;
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

    // ─── Gelir carpani ────────────────────────────────────────────────────────
    public float IncomeMultiplier { get; private set; } = 1f;
    private Coroutine incomeBoostRoutine;
    // ─────────────────────────────────────────────────────────────────────────

    public event System.Action<int> OnMoneyChanged;

    // ─── Durdurma (Pause) Sistemi ─────────────────────────────────────────────
    public bool IsPaused { get; private set; } = false;

    /// <summary>
    /// Oyunu durdurur veya devam ettirir. Buton onClick olayina baglanabilir.
    /// </summary>
    public void TogglePauseGame()
    {
        SetPauseState(!IsPaused);
    }

    /// <summary>
    /// Oyunu durdurur (Time.timeScale = 0).
    /// </summary>
    public void PauseGame()
    {
        SetPauseState(true);
    }

    /// <summary>
    /// Oyunu devam ettirir (Time.timeScale = 1).
    /// </summary>
    public void ResumeGame()
    {
        SetPauseState(false);
    }

    public void SetPauseState(bool pause)
    {
        IsPaused = pause;
        Time.timeScale = pause ? 0f : 1f;
        Debug.Log($"[GameManager] Oyun {(pause ? "durduruldu" : "devam ettiriliyor")}.");
    }
    // ─────────────────────────────────────────────────────────────────────────

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

    /// <summary>
    /// Gelir carpanini gecici olarak uygular.
    /// Aktif carpan varsa sure sifirlanarak yeniden baslar.
    /// </summary>
    public void ApplyIncomeMultiplier(float multiplier, float duration)
    {
        if (incomeBoostRoutine != null) StopCoroutine(incomeBoostRoutine);
        incomeBoostRoutine = StartCoroutine(IncomeBoostRoutine(multiplier, duration));
    }

    private IEnumerator IncomeBoostRoutine(float multiplier, float duration)
    {
        IncomeMultiplier = multiplier;
        Debug.Log($"[GameManager] Gelir carpani aktif: {multiplier}x ({duration}s)");

        yield return new WaitForSeconds(duration);

        IncomeMultiplier = 1f;
        incomeBoostRoutine = null;
        Debug.Log("[GameManager] Gelir carpani sona erdi.");
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
