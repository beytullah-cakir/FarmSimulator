using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance;

    [Header("Money UI References (TextMesh Pro Only)")]
    [SerializeField] private TextMeshProUGUI moneyText;

    [Header("Harvest Zone Upgrade UI References")]
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
        [Tooltip("The fruit unlocked by this zone.")]
        public FruitData fruit;
        [Tooltip("The TMPro component displaying the remaining cost.")]
        public TextMeshProUGUI costText;
        [Tooltip("The Slider component displaying payment progress.")]
        public Slider paymentSlider;
    }

    [Header("Unlock Zone UI References")]
    [SerializeField] private System.Collections.Generic.List<UnlockZoneUI> unlockZoneUIs = new System.Collections.Generic.List<UnlockZoneUI>();

    private void Awake()
    {
        Instance=this;
    }

    private void Start()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnMoneyChanged += UpdateMoneyDisplay;
            UpdateMoneyDisplay(GameManager.Instance.PlayerMoney);
        }
    }

    private void OnDestroy()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnMoneyChanged -= UpdateMoneyDisplay;
        }
    }

    public void UpdateMoneyDisplay(int currentMoney)
    {
        if (moneyText != null)
        {
            moneyText.text = currentMoney.ToString();
        }
    }

    /// <summary>
    /// Updates the local or screen-space upgrade panel with values from a HarvestZone.
    /// </summary>
    public void UpdateUpgradeUI(
        int currentIncome, int nextIncome, int incomeLevel, int maxIncomeLevel, int incomeUpgradeCost,
        float currentHarvestDuration, float nextDuration, int speedLevel, int maxSpeedLevel, int speedUpgradeCost,
        int treeLevel, int treePurchaseCost, bool treeMaxed)
    {
        // --- Income Upgrade ---
        if (fruitIncomeText != null)
        {
            if (incomeLevel >= maxIncomeLevel)
                fruitIncomeText.text = $"{currentIncome} <color=#f1c40f>(MAX)</color>";
            else
                fruitIncomeText.text = $"{currentIncome}-><color=#2ecc71>{nextIncome}</color>";
        }

        if (incomeLevelText != null)
            incomeLevelText.text = incomeLevel >= maxIncomeLevel ? "Lv MAX" : $"Lv {incomeLevel}/{maxIncomeLevel}";

        if (incomeCostText != null)
            incomeCostText.text = incomeLevel >= maxIncomeLevel ? "MAX" : incomeUpgradeCost.ToString();

        // --- Speed Upgrade ---
        if (harvestDurationText != null)
        {
            if (speedLevel >= maxSpeedLevel || currentHarvestDuration <= 0.2f)
                harvestDurationText.text = $"{currentHarvestDuration:F1}s <color=#f1c40f>(MAX)</color>";
            else
                harvestDurationText.text = $"{currentHarvestDuration}s-><color=#2ecc71>{nextDuration:F1}s</color>";
        }

        if (speedLevelText != null)
            speedLevelText.text = (speedLevel >= maxSpeedLevel || currentHarvestDuration <= 0.2f) ? "Lv MAX" : $"Lv {speedLevel}/{maxSpeedLevel}";

        if (speedCostText != null)
            speedCostText.text = (speedLevel >= maxSpeedLevel || currentHarvestDuration <= 0.2f) ? "MAX" : speedUpgradeCost.ToString();

        // --- Tree Upgrade ---
        if (buyTreeCostText != null)
            buyTreeCostText.text = treeMaxed ? "MAX" : treePurchaseCost.ToString();

        if (treeLevelText != null)
            treeLevelText.text = treeMaxed ? "Lv MAX" : $"Lv {treeLevel}";
    }

    /// <summary>
    /// Updates the active UnlockZone UI elements for a specific fruit.
    /// </summary>
    public void UpdateUnlockUI(FruitData fruit, int remainingCost, float fillAmount)
    {
        if (fruit == null) return;

        var ui = unlockZoneUIs.Find(x => x.fruit == fruit);
        if (ui != null)
        {
            if (ui.costText != null)
                ui.costText.text = remainingCost.ToString();

            if (ui.paymentSlider != null)
            {
                ui.paymentSlider.minValue = 0f;
                ui.paymentSlider.maxValue = 1f;
                ui.paymentSlider.value = fillAmount;
            }
        }
    }
}
