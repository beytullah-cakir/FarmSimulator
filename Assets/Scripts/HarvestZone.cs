using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
using TMPro;

public class HarvestZone : MonoBehaviour
{
    [SerializeField] private List<Prop> targetTrees = new List<Prop>();

    [Header("Harvest Settings")]
    [SerializeField] private float harvestInterval = 0.2f;

    [SerializeField] private GameObject upgradeButtonObject;

    [Header("Upgradable Stats (In-Memory)")]
    [SerializeField] private int currentIncome = 10;
    [SerializeField] private float currentHarvestDuration = 10f;

    [Header("Upgrade Costs")]
    [SerializeField] private int incomeUpgradeCost = 50;
    [SerializeField] private int speedUpgradeCost = 50;
    [SerializeField] private int treePurchaseCost = 150;

    [Header("Upgrade Levels")]
    [SerializeField] private int incomeLevel = 0;
    [SerializeField] private int maxIncomeLevel = 10;

    [SerializeField] private int speedLevel = 0;
    [SerializeField] private int maxSpeedLevel = 10;

    // Tree level is derived from how many trees are active (index based)
    private int treeLevel = 0; // 0 means only first tree is active

    // (UI text references are now managed by UIManager)

    private PlayerInventory activeInventory;
    private Coroutine harvestCoroutine;
    private int reservedSpace = 0;

    private void Start()
    {
        if (upgradeButtonObject != null)
        {
            upgradeButtonObject.SetActive(false);
        }

        // Başlangıçta 1. ve sonraki ağaçları deaktif et (sadece 0. ağaç açık başlar)
        for (int i = 1; i < targetTrees.Count; i++)
        {
            if (targetTrees[i] != null)
            {
                targetTrees[i].gameObject.SetActive(false);
            }
        }

        // Başlangıç değerlerini eşitleyip hafızada tutuyoruz
        FruitData fruit = targetTrees[0]?.FruitData;
        if (fruit != null)
        {
            fruit.SetBasePrice(currentIncome);
        }

        // Hasat süresi/yenilenme süresini tüm ağaçlara uyguluyoruz
        foreach (Prop tree in targetTrees)
        {
            if (tree != null)
            {
                tree.SetRegrowthDuration(currentHarvestDuration);
            }
        }
    }

    private void Update()
    {
        if (activeInventory == null) return;

        // Oyuncu alandayken buton durumunu ve UI verilerini sürekli güncel tut
        UpdateUpgradeButtonState();
        UpdateUpgradeUIDisplay();
    }

    /// <summary>
    /// Manuel geliştirme butonunun görünürlüğünü kontrol eder.
    /// </summary>
    private void UpdateUpgradeButtonState()
    {
        if (activeInventory != null)
        {
            int nextIndex = GetNextInactiveTreeIndex();
            bool canBuyTree = nextIndex != -1;
            if (canBuyTree)
            {
                upgradeButtonObject?.SetActive(true);
                return;
            }
        }
        upgradeButtonObject?.SetActive(false);
    }

    /// <summary>
    /// Geliştirme menüsündeki tüm değerleri bu tarlanın güncel verileriyle yerel olarak günceller.
    /// </summary>
    private void UpdateUpgradeUIDisplay()
    {
        if (UIManager.Instance == null) return;

        int nextIncome = currentIncome + 5;
        float nextDuration = Mathf.Max(0.2f, currentHarvestDuration - 0.5f);
        bool treeMaxed = GetNextInactiveTreeIndex() == -1;

        UIManager.Instance.UpdateUpgradeUI(
            currentIncome, nextIncome, incomeLevel, maxIncomeLevel, incomeUpgradeCost,
            currentHarvestDuration, nextDuration, speedLevel, maxSpeedLevel, speedUpgradeCost,
            treeLevel, treePurchaseCost, treeMaxed
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

            UpdateUpgradeButtonState();
            UpdateUpgradeUIDisplay();

            if (harvestCoroutine != null)
            {
                StopCoroutine(harvestCoroutine);
            }
            harvestCoroutine = StartCoroutine(HarvestRoutine());
        }
    }

    /// <summary>
    /// Upgrade menüsündeki "Ağaç Ekle" butonuna tıklandığında çağrılır.
    /// Gerekli bakiye şartı sağlandıysa yeni bir ağaç satın alıp aktif eder.
    /// </summary>
    public void Upgrade()
    {
        AddNewTree();
    }

    /// <summary>
    /// Sıradaki ağacı satın alır ve aktif eder.
    /// </summary>
    public void AddNewTree()
    {
        if (GameManager.Instance == null) return;

        int nextIndex = GetNextInactiveTreeIndex();
        if (nextIndex == -1)
        {
            Debug.LogWarning("[HarvestZone] Açılacak başka ağaç yok!");
            return;
        }

        if (GameManager.Instance.PlayerMoney >= treePurchaseCost)
        {
            bool success = GameManager.Instance.RemoveMoney(treePurchaseCost);
            if (success)
            {
                targetTrees[nextIndex].gameObject.SetActive(true);
                treeLevel++;

                // Upgrade maliyetini artır
                treePurchaseCost += 50;

                UpdateUpgradeButtonState();
                UpdateUpgradeUIDisplay();
                Debug.Log($"[HarvestZone] Yeni ağaç eklendi. Ağaç Level: {treeLevel}");
            }
        }
        else
        {
            Debug.LogWarning($"[HarvestZone] Yetersiz bakiye! Ağaç ekleme maliyeti: {treePurchaseCost}");
        }
    }

    /// <summary>
    /// Hasat alanındaki ağacın kazancını (BasePrice) artırır.
    /// </summary>
    public void UpgradeIncome()
    {
        if (GameManager.Instance == null) return;

        if (incomeLevel >= maxIncomeLevel)
        {
            Debug.LogWarning("[HarvestZone] Kazanç upgrade'i maksimum seviyeye ulaştı!");
            return;
        }

        FruitData fruit = targetTrees[0]?.FruitData;
        if (fruit == null) return;

        if (GameManager.Instance.PlayerMoney >= incomeUpgradeCost)
        {
            bool success = GameManager.Instance.RemoveMoney(incomeUpgradeCost);
            if (success)
            {
                currentIncome += 5;
                fruit.SetBasePrice(currentIncome);
                incomeLevel++;

                // Geliştirme maliyetini artır
                incomeUpgradeCost += 25;

                UpdateUpgradeUIDisplay();
                Debug.Log($"[HarvestZone] Kazanç Yükseltildi! Yeni Kazanç: {currentIncome}, Level: {incomeLevel}/{maxIncomeLevel}");
            }
        }
        else
        {
            Debug.LogWarning($"[HarvestZone] Yetersiz bakiye! Kazanç yükseltme maliyeti: {incomeUpgradeCost}");
        }
    }

    /// <summary>
    /// Hasat süresini (hızını) azaltır.
    /// </summary>
    public void UpgradeHarvestSpeed()
    {
        if (GameManager.Instance == null) return;

        if (speedLevel >= maxSpeedLevel)
        {
            Debug.LogWarning("[HarvestZone] Hız upgrade'i maksimum seviyeye ulaştı!");
            return;
        }

        if (currentHarvestDuration <= 0.2f)
        {
            Debug.LogWarning("[HarvestZone] Hasat süresi zaten maksimum hızda (0.2s)!");
            return;
        }

        if (GameManager.Instance.PlayerMoney >= speedUpgradeCost)
        {
            bool success = GameManager.Instance.RemoveMoney(speedUpgradeCost);
            if (success)
            {
                currentHarvestDuration = Mathf.Max(0.2f, currentHarvestDuration - 0.5f);
                speedLevel++;

                // Hasat yenilenme süresini tüm ağaçlara uyguluyoruz
                foreach (Prop tree in targetTrees)
                {
                    if (tree != null)
                    {
                        tree.SetRegrowthDuration(currentHarvestDuration);
                    }
                }

                // Geliştirme maliyetini artır
                speedUpgradeCost += 25;

                UpdateUpgradeUIDisplay();
                Debug.Log($"[HarvestZone] Hasat Süresi Kısaltıldı! Yeni Süre: {currentHarvestDuration}s, Level: {speedLevel}/{maxSpeedLevel}");
            }
        }
        else
        {
            Debug.LogWarning($"[HarvestZone] Yetersiz bakiye! Hız yükseltme maliyeti: {speedUpgradeCost}");
        }
    }

    private void OnTriggerExit(Collider other)
    {
        PlayerInventory inventory = other.GetComponent<PlayerInventory>();
        if (inventory != null && activeInventory == inventory)
        {
            activeInventory = null;

            upgradeButtonObject?.SetActive(false);

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

    private GameObject CreateVisualApple(Prop tree)
    {
        if (tree.FruitData != null && tree.FruitData.FruitPrefab != null)
        {
            return Instantiate(tree.FruitData.FruitPrefab, tree.transform.position + Vector3.up * 2f, Quaternion.identity);
        }
        return null;
    }

    private IEnumerator AnimateAppleFly(GameObject appleObj, PlayerInventory inventory, FruitData fruitData)
    {
        Vector3 startPos = appleObj.transform.position;
        Quaternion startRot = appleObj.transform.rotation;
        Vector3 startScale = appleObj.transform.localScale;

        float duration = 0.6f;
        float elapsed = 0f;
        float arcHeight = 3.0f;

        appleObj.transform.parent = null;

        Collider col = appleObj.GetComponent<Collider>();
        if (col != null)
        {
            col.enabled = false;
        }

        Rigidbody rb = appleObj.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = true;
        }

        while (elapsed < duration)
        {
            if (appleObj == null)
            {
                reservedSpace = Mathf.Max(0, reservedSpace - 1);
                yield break;
            }

            if (inventory == null)
            {
                Destroy(appleObj);
                reservedSpace = Mathf.Max(0, reservedSpace - 1);
                yield break;
            }

            elapsed += Time.deltaTime;
            float t = elapsed / duration;

            Vector3 targetPos = inventory.transform.position + Vector3.up * 1f;

            Vector3 currentPos = Vector3.Lerp(startPos, targetPos, t);
            currentPos.y += Mathf.Sin(t * Mathf.PI) * arcHeight;

            appleObj.transform.position = currentPos;
            appleObj.transform.rotation = startRot * Quaternion.Euler(t * 360f, t * 720f, 0f);
            appleObj.transform.localScale = Vector3.Lerp(startScale, startScale * 0.4f, t);

            yield return null;
        }

        if (appleObj != null)
        {
            Destroy(appleObj);
        }

        if (inventory != null)
        {
            inventory.AddFruit(fruitData, 1);
        }

        reservedSpace = Mathf.Max(0, reservedSpace - 1);
    }
}
