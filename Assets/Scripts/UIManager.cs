using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance;

    [SerializeField] private TextMeshProUGUI moneyText;

    [SerializeField] private TextMeshProUGUI harvestDurationText;
    [SerializeField] private TextMeshProUGUI fruitIncomeText;
    [SerializeField] private TextMeshProUGUI incomeCostText;
    [SerializeField] private TextMeshProUGUI speedCostText;
    [SerializeField] private TextMeshProUGUI buyTreeCostText;
    [SerializeField] private TextMeshProUGUI incomeLevelText;
    [SerializeField] private TextMeshProUGUI speedLevelText;
    [SerializeField] private TextMeshProUGUI treeLevelText;

    [System.Serializable]
    public class UnlockZoneUI
    {
        public FruitData fruit;
        public TextMeshProUGUI costText;
        public Slider paymentSlider;
    }

    [SerializeField] private System.Collections.Generic.List<UnlockZoneUI> unlockZoneUIs = new();

    [Header("Ad & Boost Timers")]
    [SerializeField] private Slider speedBoostSlider;
    [SerializeField] private TextMeshProUGUI speedBoostTimerText;
    [SerializeField] private Slider incomeBoostSlider;
    [SerializeField] private TextMeshProUGUI incomeBoostTimerText;
    [SerializeField] private Slider adBoostSlider;
    [SerializeField] private TextMeshProUGUI adBoostTimerText;

    private HarvestZone activeHarvestZone;

    private void Awake() => Instance = this;

    private void Start()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnMoneyChanged += UpdateMoneyDisplay;
            UpdateMoneyDisplay(GameManager.Instance.PlayerMoney);
        }

        if (speedBoostSlider != null) speedBoostSlider.gameObject.SetActive(false);
        if (speedBoostTimerText != null) speedBoostTimerText.gameObject.SetActive(false);
        if (incomeBoostSlider != null) incomeBoostSlider.gameObject.SetActive(false);
        if (incomeBoostTimerText != null) incomeBoostTimerText.gameObject.SetActive(false);
        if (adBoostSlider != null) adBoostSlider.gameObject.SetActive(false);
        if (adBoostTimerText != null) adBoostTimerText.gameObject.SetActive(false);
    }

    private void OnDestroy()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.OnMoneyChanged -= UpdateMoneyDisplay;
    }

    public void UpdateMoneyDisplay(int currentMoney)
    {
        if (moneyText != null) moneyText.text = FormatUtility.FormatNumber(currentMoney);
    }

    public void UpdateUpgradeUI(
        int currentIncome, int nextIncome, int incomeLevel, int maxIncomeLevel, int incomeUpgradeCost,
        float currentHarvestDuration, float nextDuration, int speedLevel, int maxSpeedLevel, int speedUpgradeCost,
        int activeTrees, int maxTrees, int treePurchaseCost, bool treeMaxed)
    {
        if (fruitIncomeText != null)
        {
            fruitIncomeText.text = incomeLevel >= maxIncomeLevel
                ? $"{FormatUtility.FormatNumber(currentIncome)} <color=#f1c40f>(MAX)</color>"
                : $"{FormatUtility.FormatNumber(currentIncome)}-><color=#2ecc71>{FormatUtility.FormatNumber(nextIncome)}</color>";
        }

        if (incomeLevelText != null)
            incomeLevelText.text = incomeLevel >= maxIncomeLevel ? "Lv MAX" : $"Lv {incomeLevel}/{maxIncomeLevel}";

        if (incomeCostText != null)
            incomeCostText.text = incomeLevel >= maxIncomeLevel ? "MAX" : FormatUtility.FormatNumber(incomeUpgradeCost);

        bool speedMaxed = speedLevel >= maxSpeedLevel || currentHarvestDuration <= 0.2f;

        if (harvestDurationText != null)
        {
            harvestDurationText.text = speedMaxed
                ? $"{currentHarvestDuration:F1}s <color=#f1c40f>(MAX)</color>"
                : $"{currentHarvestDuration}s-><color=#2ecc71>{nextDuration:F1}s</color>";
        }

        if (speedLevelText != null)
            speedLevelText.text = speedMaxed ? "Lv MAX" : $"Lv {speedLevel}/{maxSpeedLevel}";

        if (speedCostText != null)
            speedCostText.text = speedMaxed ? "MAX" : FormatUtility.FormatNumber(speedUpgradeCost);

        if (buyTreeCostText != null)
            buyTreeCostText.text = treeMaxed ? "MAX" : FormatUtility.FormatNumber(treePurchaseCost);

        if (treeLevelText != null)
            treeLevelText.text = $"{activeTrees}/{maxTrees}";
    }

    public void UpdateUnlockUI(FruitData fruit, int remainingCost, float fillAmount)
    {
        if (fruit == null) return;

        var ui = unlockZoneUIs.Find(x => x.fruit == fruit);
        if (ui == null) return;

        if (ui.costText != null) ui.costText.text = FormatUtility.FormatNumber(remainingCost);

        if (ui.paymentSlider != null)
        {
            ui.paymentSlider.minValue = 0f;
            ui.paymentSlider.maxValue = 1f;
            ui.paymentSlider.value = fillAmount;
        }
    }

    public void SetActiveHarvestZone(HarvestZone zone) => activeHarvestZone = zone;

    public void TriggerActiveIncomeUpgrade()
    {
        if (UpgradeManager.Instance != null) UpgradeManager.Instance.UpgradeIncome();
    }

    public void TriggerActiveSpeedUpgrade()
    {
        if (UpgradeManager.Instance != null) UpgradeManager.Instance.UpgradeHarvestSpeed();
    }

    public void TriggerActiveAddNewTree()
    {
        if (UpgradeManager.Instance != null) UpgradeManager.Instance.AddNewTree();
    }

    public void UpdateSpeedBoostSlider(float remainingTime, float duration)
    {
        float fill = duration > 0f ? Mathf.Clamp01(remainingTime / duration) : 0f;
        bool active = remainingTime > 0f;

        if (speedBoostSlider != null)
        {
            speedBoostSlider.gameObject.SetActive(active);
            speedBoostSlider.minValue = 0f;
            speedBoostSlider.maxValue = 1f;
            speedBoostSlider.value = fill;
        }

        if (speedBoostTimerText != null)
        {
            speedBoostTimerText.gameObject.SetActive(active);
            speedBoostTimerText.text = active ? $"{Mathf.CeilToInt(remainingTime)}s" : "";
        }
    }

    public void UpdateIncomeBoostSlider(float remainingTime, float duration)
    {
        float fill = duration > 0f ? Mathf.Clamp01(remainingTime / duration) : 0f;
        bool active = remainingTime > 0f;

        if (incomeBoostSlider != null)
        {
            incomeBoostSlider.gameObject.SetActive(active);
            incomeBoostSlider.minValue = 0f;
            incomeBoostSlider.maxValue = 1f;
            incomeBoostSlider.value = fill;
        }

        if (incomeBoostTimerText != null)
        {
            incomeBoostTimerText.gameObject.SetActive(active);
            incomeBoostTimerText.text = active ? $"{Mathf.CeilToInt(remainingTime)}s" : "";
        }
    }

    public void UpdateAdBoostSlider(float remainingTime, float duration, string label = "")
    {
        float fill = duration > 0f ? Mathf.Clamp01(remainingTime / duration) : 0f;
        bool active = remainingTime > 0f;

        if (adBoostSlider != null)
        {
            adBoostSlider.gameObject.SetActive(active);
            adBoostSlider.minValue = 0f;
            adBoostSlider.maxValue = 1f;
            adBoostSlider.value = fill;
        }

        if (adBoostTimerText != null)
        {
            adBoostTimerText.gameObject.SetActive(active);
            adBoostTimerText.text = active ? $"{label} {Mathf.CeilToInt(remainingTime)}s".Trim() : "";
        }
    }
}
