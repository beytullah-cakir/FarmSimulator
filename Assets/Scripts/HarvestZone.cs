using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HarvestZone : MonoBehaviour, ISaveable
{
    [SerializeField] private string saveID = "harvest_zone_default";
    [SerializeField] private List<Prop> targetTrees = new List<Prop>();
    [SerializeField] private float harvestInterval = 0.2f;
    [SerializeField] private GameObject upgradeButtonObject;

    [SerializeField] private int incomeUpgradeCost = 50;
    [SerializeField] private int speedUpgradeCost = 50;
    [SerializeField] private int treePurchaseCost = 150;

    [SerializeField] private int incomeLevel = 0;
    [SerializeField] private int maxIncomeLevel = 10;
    [SerializeField] private int speedLevel = 0;
    [SerializeField] private int maxSpeedLevel = 10;

    public string SaveID => saveID;

    private PlayerInventory activeInventory;
    private Coroutine harvestCoroutine;
    private int reservedSpace = 0;

    private void Start()
    {
        for (int i = 1; i < targetTrees.Count; i++)
        {
            if (targetTrees[i] != null)
                targetTrees[i].gameObject.SetActive(false);
        }

        FruitData fruit = targetTrees[0]?.FruitData;
        if (fruit != null)
        {
            foreach (Prop tree in targetTrees)
            {
                if (tree != null) tree.SetRegrowthDuration(fruit.RegrowthDuration);
            }
        }
    }

    private void UpdateUpgradeButtonState()
    {
        if (activeInventory != null && GetNextInactiveTreeIndex() != -1)
        {
            upgradeButtonObject.SetActive(true);
            return;
        }
        upgradeButtonObject.SetActive(false);
    }

    private void UpdateUpgradeUIDisplay()
    {
        if (UIManager.Instance == null) return;

        FruitData fruit = targetTrees[0].FruitData;
        if (fruit == null) return;

        int currentIncome = fruit.BasePrice;
        int nextIncome = currentIncome + 5;
        float currentDuration = fruit.RegrowthDuration;
        float nextDuration = Mathf.Max(0.2f, currentDuration - 0.5f);
        bool treeMaxed = GetNextInactiveTreeIndex() == -1;

        int activeTrees = 0;
        foreach (var tree in targetTrees)
        {
            if (tree != null && tree.gameObject.activeSelf) activeTrees++;
        }

        UIManager.Instance.UpdateUpgradeUI(
            currentIncome, nextIncome, incomeLevel, maxIncomeLevel, incomeUpgradeCost,
            currentDuration, nextDuration, speedLevel, maxSpeedLevel, speedUpgradeCost,
            activeTrees, targetTrees.Count, treePurchaseCost, treeMaxed
        );
    }

    private int GetNextInactiveTreeIndex()
    {
        for (int i = 0; i < targetTrees.Count; i++)
        {
            if (targetTrees[i] != null && !targetTrees[i].gameObject.activeSelf)
                return i;
        }
        return -1;
    }

    private void OnTriggerEnter(Collider other)
    {
        PlayerInventory inventory = other.GetComponent<PlayerInventory>();
        if (inventory == null) return;

        activeInventory = inventory;

        if (UIManager.Instance != null)
            UIManager.Instance.SetActiveHarvestZone(this);

        UpdateUpgradeButtonState();
        UpdateUpgradeUIDisplay();

        if (harvestCoroutine != null) StopCoroutine(harvestCoroutine);
        harvestCoroutine = StartCoroutine(HarvestRoutine());
    }

    private void OnTriggerExit(Collider other)
    {
        PlayerInventory inventory = other.GetComponent<PlayerInventory>();
        if (activeInventory != inventory) return;

        activeInventory = null;
        UIManager.Instance.SetActiveHarvestZone(null);
        upgradeButtonObject.SetActive(false);

        if (harvestCoroutine != null)
        {
            StopCoroutine(harvestCoroutine);
            harvestCoroutine = null;
        }
    }

    public void AddNewTree()
    {
        int nextIndex = GetNextInactiveTreeIndex();
        if (GameManager.Instance == null || nextIndex == -1) return;
        if (GameManager.Instance.PlayerMoney < treePurchaseCost) return;

        if (GameManager.Instance.RemoveMoney(treePurchaseCost))
        {
            targetTrees[nextIndex].gameObject.SetActive(true);
            treePurchaseCost += 50;
            UpdateUpgradeUIDisplay();
            AudioManager.Instance?.PlaySFX(AudioManager.Instance.cashPayment);
        }
    }

    public void UpgradeIncome()
    {
        if (GameManager.Instance == null || incomeLevel >= maxIncomeLevel) return;
        if (GameManager.Instance.PlayerMoney < incomeUpgradeCost) return;

        FruitData fruit = targetTrees[0].FruitData;

        if (GameManager.Instance.RemoveMoney(incomeUpgradeCost))
        {
            fruit.SetBasePrice(fruit.BasePrice + 5);
            incomeLevel++;
            incomeUpgradeCost += 25;
            UpdateUpgradeUIDisplay();
            AudioManager.Instance?.PlaySFX(AudioManager.Instance.cashPayment);
        }
    }

    public void UpgradeHarvestSpeed()
    {
        if (GameManager.Instance == null || speedLevel >= maxSpeedLevel) return;
        if (targetTrees[0].FruitData.RegrowthDuration <= 0.2f) return;
        if (GameManager.Instance.PlayerMoney < speedUpgradeCost) return;

        FruitData fruit = targetTrees[0].FruitData;

        if (GameManager.Instance.RemoveMoney(speedUpgradeCost))
        {
            float newDuration = Mathf.Max(0.2f, fruit.RegrowthDuration - 0.5f);
            fruit.SetRegrowthDuration(newDuration);
            speedLevel++;

            foreach (Prop tree in targetTrees)
            {
                if (tree != null) tree.SetRegrowthDuration(newDuration);
            }

            speedUpgradeCost += 25;
            UpdateUpgradeUIDisplay();
            AudioManager.Instance?.PlaySFX(AudioManager.Instance.cashPayment);
        }
    }

    private IEnumerator HarvestRoutine()
    {
        while (activeInventory != null)
        {
            PlayerController player = activeInventory.GetComponent<PlayerController>();
            if (player != null && !player.enabled)
            {
                yield return new WaitForSeconds(harvestInterval);
                continue;
            }

            int currentSpaceAvailable = activeInventory.GetSpaceAvailable() - reservedSpace;
            if (currentSpaceAvailable > 0)
            {
                Prop nextTree = FindNextTreeWithFruit();
                if (nextTree != null)
                {
                    reservedSpace++;
                    int harvested = nextTree.Harvest(1);

                    if (harvested > 0)
                    {
                        GameObject visualApple = Instantiate(nextTree.FruitData.FruitPrefab,
                            nextTree.transform.position + Vector3.up * 2f, Quaternion.identity);

                        if (visualApple != null)
                            StartCoroutine(AnimateAppleFly(visualApple, activeInventory, nextTree.FruitData));
                        else
                        {
                            activeInventory.AddFruit(nextTree.FruitData, 1);
                            reservedSpace = Mathf.Max(0, reservedSpace - 1);
                        }
                    }
                    else
                    {
                        reservedSpace = Mathf.Max(0, reservedSpace - 1);
                    }
                }
            }

            yield return new WaitForSeconds(harvestInterval);
        }
    }

    private Prop FindNextTreeWithFruit()
    {
        for (int i = 0; i < targetTrees.Count; i++)
        {
            if (targetTrees[i] != null && targetTrees[i].gameObject.activeInHierarchy && targetTrees[i].CurrentAmount > 0)
                return targetTrees[i];
        }
        return null;
    }

    private IEnumerator AnimateAppleFly(GameObject appleObj, PlayerInventory inventory, FruitData fruitData)
    {
        Vector3 startPos = appleObj.transform.position;
        float duration = 0.6f;
        float elapsed = 0f;
        float arcHeight = 3.0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;

            Vector3 targetPos = inventory.transform.position + Vector3.up * 1f;
            Vector3 currentPos = Vector3.Lerp(startPos, targetPos, t);
            currentPos.y += Mathf.Sin(t * Mathf.PI) * arcHeight;

            appleObj.transform.position = currentPos;
            yield return null;
        }

        Destroy(appleObj);
        inventory.AddFruit(fruitData, 1);
        reservedSpace = Mathf.Max(0, reservedSpace - 1);

        AudioManager.Instance?.PlaySFX(AudioManager.Instance.itemPickUp);
    }

    #region ISaveable

    public object CaptureState()
    {
        int activeCount = 0;
        foreach (var tree in targetTrees)
        {
            if (tree != null && tree.gameObject.activeSelf) activeCount++;
        }

        return new HarvestZoneSaveData
        {
            saveID = this.saveID,
            incomeLevel = this.incomeLevel,
            speedLevel = this.speedLevel,
            incomeUpgradeCost = this.incomeUpgradeCost,
            speedUpgradeCost = this.speedUpgradeCost,
            treePurchaseCost = this.treePurchaseCost,
            activeTreeCount = activeCount
        };
    }

    public void RestoreState(object state)
    {
        if (state is not HarvestZoneSaveData data) return;

        incomeLevel = data.incomeLevel;
        speedLevel = data.speedLevel;
        incomeUpgradeCost = data.incomeUpgradeCost;
        speedUpgradeCost = data.speedUpgradeCost;
        treePurchaseCost = data.treePurchaseCost;

        ActivateTreesByCount(data.activeTreeCount);

        if (targetTrees.Count > 0 && targetTrees[0] != null)
        {
            FruitData fruit = targetTrees[0].FruitData;
            if (fruit != null)
            {
                foreach (Prop tree in targetTrees)
                {
                    if (tree != null) tree.SetRegrowthDuration(fruit.RegrowthDuration);
                }
            }
        }
    }

    private void ActivateTreesByCount(int count)
    {
        for (int i = 0; i < targetTrees.Count; i++)
        {
            if (targetTrees[i] != null)
                targetTrees[i].gameObject.SetActive(i < count);
        }
    }

    #endregion
}
