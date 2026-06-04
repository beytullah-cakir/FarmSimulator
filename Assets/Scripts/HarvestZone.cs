using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class HarvestZone : MonoBehaviour
{
    [SerializeField] private List<Prop> targetTrees = new List<Prop>();

    [Header("Harvest Settings")]
    [SerializeField] private float harvestInterval = 0.2f;

    [SerializeField] private GameObject upgradeButtonObject;


    [Header("Upgrade Costs")]
    [SerializeField] private int incomeUpgradeCost = 50;
    [SerializeField] private int speedUpgradeCost = 50;
    [SerializeField] private int treePurchaseCost = 150;

    [Header("Upgrade Levels")]
    [SerializeField] private int incomeLevel = 0;
    [SerializeField] private int maxIncomeLevel = 10;

    [SerializeField] private int speedLevel = 0;
    [SerializeField] private int maxSpeedLevel = 10;


    // (UI text references are now managed by UIManager)

    private PlayerInventory activeInventory;
    private Coroutine harvestCoroutine;
    private int reservedSpace = 0;

    private void Start()
    {
        for (int i = 1; i < targetTrees.Count; i++)
        {
            if (targetTrees[i] != null)
            {
                targetTrees[i].gameObject.SetActive(false);
            }
        }

        // Hasat süresi/yenilenme süresini tüm ağaçlara uyguluyoruz
        FruitData fruit = targetTrees[0]?.FruitData;
        if (fruit != null)
        {
            foreach (Prop tree in targetTrees)
            {
                if (tree != null)
                {
                    tree.SetRegrowthDuration(fruit.RegrowthDuration);
                }
            }
        }
    }

    private void Update()
    {
        if (activeInventory == null) return;

        // Oyuncu alandayken buton durumunu ve UI verilerini sürekli güncel tut
        // UpdateUpgradeButtonState();
        // UpdateUpgradeUIDisplay();
    }

    private void UpdateUpgradeButtonState()
    {
        if (activeInventory != null)
        {
            int nextIndex = GetNextInactiveTreeIndex();
            bool canBuyTree = nextIndex != -1;
            if (canBuyTree)
            {
                upgradeButtonObject.SetActive(true);
                return;
            }
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
        float currentHarvestDuration = fruit.RegrowthDuration;
        float nextDuration = Mathf.Max(0.2f, currentHarvestDuration - 0.5f);
        bool treeMaxed = GetNextInactiveTreeIndex() == -1;

        int activeTrees = 0;
        foreach (var tree in targetTrees)
        {
            if (tree != null && tree.gameObject.activeSelf)
            {
                activeTrees++;
            }
        }
        int maxTrees = targetTrees.Count;

        UIManager.Instance.UpdateUpgradeUI(
            currentIncome, nextIncome, incomeLevel, maxIncomeLevel, incomeUpgradeCost,
            currentHarvestDuration, nextDuration, speedLevel, maxSpeedLevel, speedUpgradeCost,
            activeTrees, maxTrees, treePurchaseCost, treeMaxed
        );
    }

    private int GetNextInactiveTreeIndex()
    {
        for (int i = 0; i < targetTrees.Count; i++)
        {
            if (targetTrees[i] != null && !targetTrees[i].gameObject.activeSelf)
            {
                return i;
            }
        }
        return -1;
    }

    private void OnTriggerEnter(Collider other)
    {
        PlayerInventory inventory = other.GetComponent<PlayerInventory>();
        if (inventory != null)
        {
            activeInventory = inventory;

            if (UIManager.Instance != null)
            {
                UIManager.Instance.SetActiveHarvestZone(this);
            }

            UpdateUpgradeButtonState();
            UpdateUpgradeUIDisplay();

            if (harvestCoroutine != null)
            {
                StopCoroutine(harvestCoroutine);
            }
            harvestCoroutine = StartCoroutine(HarvestRoutine());
        }
    }
    public void AddNewTree()
    {
        int nextIndex = GetNextInactiveTreeIndex();
        if (GameManager.Instance == null || nextIndex == -1) return;




        if (GameManager.Instance.PlayerMoney >= treePurchaseCost)
        {
            bool success = GameManager.Instance.RemoveMoney(treePurchaseCost);
            if (success)
            {
                targetTrees[nextIndex].gameObject.SetActive(true);

                // Upgrade maliyetini artır
                treePurchaseCost += 50;

                //UpdateUpgradeButtonState();
                UpdateUpgradeUIDisplay();

                int activeCount = 0;
                foreach (var t in targetTrees)
                {
                    if (t != null && t.gameObject.activeSelf) activeCount++;
                }
            }
        }

    }

    public void UpgradeIncome()
    {
        if (GameManager.Instance == null || incomeLevel >= maxIncomeLevel) return;



        FruitData fruit = targetTrees[0].FruitData;


        if (GameManager.Instance.PlayerMoney >= incomeUpgradeCost)
        {
            bool success = GameManager.Instance.RemoveMoney(incomeUpgradeCost);
            if (success)
            {
                int newIncome = fruit.BasePrice + 5;
                fruit.SetBasePrice(newIncome);
                incomeLevel++;

                // Geliştirme maliyetini artır
                incomeUpgradeCost += 25;

                UpdateUpgradeUIDisplay();
            }
        }

    }

    public void UpgradeHarvestSpeed()
    {
        if (GameManager.Instance == null ||
        speedLevel >= maxSpeedLevel ||
        targetTrees[0].FruitData.RegrowthDuration <= 0.2f) return;


        FruitData fruit = targetTrees[0].FruitData;


        if (GameManager.Instance.PlayerMoney >= speedUpgradeCost)
        {
            bool success = GameManager.Instance.RemoveMoney(speedUpgradeCost);
            if (success)
            {
                float newDuration = Mathf.Max(0.2f, fruit.RegrowthDuration - 0.5f);
                fruit.SetRegrowthDuration(newDuration);
                speedLevel++;

                // Hasat yenilenme süresini tüm ağaçlara uyguluyoruz
                foreach (Prop tree in targetTrees)
                {
                    if (tree != null)
                    {
                        tree.SetRegrowthDuration(newDuration);
                    }
                }

                // Geliştirme maliyetini artır
                speedUpgradeCost += 25;

                UpdateUpgradeUIDisplay();
            }
        }

    }

    private void OnTriggerExit(Collider other)
    {
        PlayerInventory inventory = other.GetComponent<PlayerInventory>();
        if (activeInventory == inventory)
        {
            activeInventory = null;
            UIManager.Instance.SetActiveHarvestZone(null);


            upgradeButtonObject.SetActive(false);

            if (harvestCoroutine != null)
            {
                StopCoroutine(harvestCoroutine);
                harvestCoroutine = null;
            }
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
                        GameObject visualApple = CreateVisualApple(nextTree);

                        if (visualApple != null)
                        {
                            StartCoroutine(AnimateAppleFly(visualApple, activeInventory, nextTree.FruitData));
                        }
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
            {
                return targetTrees[i];
            }
        }
        return null;
    }

    private GameObject CreateVisualApple(Prop tree) => Instantiate(tree.FruitData.FruitPrefab, tree.transform.position + Vector3.up * 2f, Quaternion.identity);


    private IEnumerator AnimateAppleFly(GameObject appleObj, PlayerInventory inventory, FruitData fruitData)
    {
        Vector3 startPos = appleObj.transform.position;
        Quaternion startRot = appleObj.transform.rotation;
        Vector3 startScale = appleObj.transform.localScale;

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
    }
}
