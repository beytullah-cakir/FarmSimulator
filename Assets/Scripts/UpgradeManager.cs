using System.Collections.Generic;
using UnityEngine;

public class UpgradeManager : MonoBehaviour
{
    public static UpgradeManager Instance;

    [System.Serializable]
    public class UpgradeEntry
    {
        public string entryName;
        public HarvestZone harvestZone;

        [Header("Income Upgrade")]
        public int incomeUpgradeCost = 50;
        public int incomeLevel = 0;
        public int maxIncomeLevel = 10;
        public int incomeCostIncrement = 25;
        public int incomeValueIncrement = 5;

        [Header("Speed Upgrade")]
        public int speedUpgradeCost = 50;
        public int speedLevel = 0;
        public int maxSpeedLevel = 10;
        public int speedCostIncrement = 25;
        public float speedReduction = 0.5f;
        public float minHarvestDuration = 0.2f;

        [Header("Tree Purchase")]
        public int treePurchaseCost = 150;
        public int treeCostIncrement = 50;
    }

    [SerializeField] private List<UpgradeEntry> upgradeEntries = new List<UpgradeEntry>();

    private UpgradeEntry activeEntry;

    private void Awake() => Instance = this;

    public UpgradeEntry GetActiveEntry() => activeEntry;
    public List<UpgradeEntry> GetAllEntries() => upgradeEntries;

    public void SetActiveEntry(HarvestZone zone)
    {
        if (zone == null)
        {
            activeEntry = null;
            return;
        }

        activeEntry = FindEntryByZone(zone);
    }

    public UpgradeEntry FindEntryByZone(HarvestZone zone)
    {
        if (zone == null) return null;
        UpgradeEntry entry = upgradeEntries.Find(e => e.harvestZone == zone);
        if (entry == null)
        {
            entry = new UpgradeEntry
            {
                entryName = zone.gameObject.name + " Upgrade",
                harvestZone = zone
            };
            upgradeEntries.Add(entry);
        }
        return entry;
    }

    // ─── Income Upgrade ───────────────────────────────────────

    public void UpgradeIncome()
    {
        if (activeEntry == null) return;
        UpgradeIncomeForEntry(activeEntry);
    }

    private void UpgradeIncomeForEntry(UpgradeEntry entry)
    {
        if (GameManager.Instance == null || entry.incomeLevel >= entry.maxIncomeLevel) return;
        if (GameManager.Instance.PlayerMoney < entry.incomeUpgradeCost) return;

        FruitData fruit = GetFruitFromEntry(entry);
        if (fruit == null) return;

        if (GameManager.Instance.RemoveMoney(entry.incomeUpgradeCost))
        {
            fruit.SetBasePrice(fruit.BasePrice + entry.incomeValueIncrement);
            entry.incomeLevel++;
            entry.incomeUpgradeCost += entry.incomeCostIncrement;
            UpdateUpgradeUI();
            AudioManager.Instance?.PlaySFX(AudioManager.Instance.cashPayment);
        }
    }

    // ─── Speed Upgrade ────────────────────────────────────────

    public void UpgradeHarvestSpeed()
    {
        if (activeEntry == null) return;
        UpgradeSpeedForEntry(activeEntry);
    }

    private void UpgradeSpeedForEntry(UpgradeEntry entry)
    {
        if (GameManager.Instance == null || entry.speedLevel >= entry.maxSpeedLevel) return;

        FruitData fruit = GetFruitFromEntry(entry);
        if (fruit == null || fruit.RegrowthDuration <= entry.minHarvestDuration) return;
        if (GameManager.Instance.PlayerMoney < entry.speedUpgradeCost) return;

        if (GameManager.Instance.RemoveMoney(entry.speedUpgradeCost))
        {
            float newDuration = Mathf.Max(entry.minHarvestDuration, fruit.RegrowthDuration - entry.speedReduction);
            fruit.SetRegrowthDuration(newDuration);
            entry.speedLevel++;

            foreach (Prop tree in entry.harvestZone.TargetTrees)
            {
                if (tree != null) tree.SetRegrowthDuration(newDuration);
            }

            entry.speedUpgradeCost += entry.speedCostIncrement;
            UpdateUpgradeUI();
            AudioManager.Instance?.PlaySFX(AudioManager.Instance.cashPayment);
        }
    }

    // ─── Add New Tree ─────────────────────────────────────────

    public void AddNewTree()
    {
        if (activeEntry == null) return;
        AddTreeForEntry(activeEntry);
    }

    private void AddTreeForEntry(UpgradeEntry entry)
    {
        int nextIndex = GetNextInactiveTreeIndex(entry);
        if (GameManager.Instance == null || nextIndex == -1) return;
        if (GameManager.Instance.PlayerMoney < entry.treePurchaseCost) return;

        if (GameManager.Instance.RemoveMoney(entry.treePurchaseCost))
        {
            entry.harvestZone.TargetTrees[nextIndex].gameObject.SetActive(true);
            entry.treePurchaseCost += entry.treeCostIncrement;
            UpdateUpgradeUI();
            AudioManager.Instance?.PlaySFX(AudioManager.Instance.cashPayment);
        }
    }

    // ─── Helpers ──────────────────────────────────────────────

    public int GetNextInactiveTreeIndex(UpgradeEntry entry)
    {
        for (int i = 0; i < entry.harvestZone.TargetTrees.Count; i++)
        {
            if (entry.harvestZone.TargetTrees[i] != null && !entry.harvestZone.TargetTrees[i].gameObject.activeSelf)
                return i;
        }
        return -1;
    }

    private FruitData GetFruitFromEntry(UpgradeEntry entry)
    {
        if (entry.harvestZone.TargetTrees.Count > 0 && entry.harvestZone.TargetTrees[0] != null)
            return entry.harvestZone.TargetTrees[0].FruitData;
        return null;
    }

    public void UpdateUpgradeUI()
    {
        if (activeEntry == null || UIManager.Instance == null) return;

        FruitData fruit = GetFruitFromEntry(activeEntry);
        if (fruit == null) return;

        int currentIncome = fruit.BasePrice;
        int nextIncome = currentIncome + activeEntry.incomeValueIncrement;
        float currentDuration = fruit.RegrowthDuration;
        float nextDuration = Mathf.Max(activeEntry.minHarvestDuration, currentDuration - activeEntry.speedReduction);
        bool treeMaxed = GetNextInactiveTreeIndex(activeEntry) == -1;

        int activeTrees = 0;
        foreach (var tree in activeEntry.harvestZone.TargetTrees)
        {
            if (tree != null && tree.gameObject.activeSelf) activeTrees++;
        }

        UIManager.Instance.UpdateUpgradeUI(
            currentIncome, nextIncome, activeEntry.incomeLevel, activeEntry.maxIncomeLevel, activeEntry.incomeUpgradeCost,
            currentDuration, nextDuration, activeEntry.speedLevel, activeEntry.maxSpeedLevel, activeEntry.speedUpgradeCost,
            activeTrees, activeEntry.harvestZone.TargetTrees.Count, activeEntry.treePurchaseCost, treeMaxed
        );
    }
}

