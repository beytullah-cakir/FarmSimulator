using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
using TMPro;

public class HarvestZone : MonoBehaviour
{
    [SerializeField] private List<Tree> targetTrees = new List<Tree>();

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

    [Header("Local Upgrade Menu UI References (TextMesh Pro Only)")]
    [SerializeField] private TextMeshProUGUI harvestDurationText;
    [SerializeField] private TextMeshProUGUI fruitIncomeText;
    [SerializeField] private TextMeshProUGUI incomeCostText;
    [SerializeField] private TextMeshProUGUI speedCostText;
    [SerializeField] private TextMeshProUGUI buyTreeCostText;

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
        foreach (Tree tree in targetTrees)
        {
            if (tree != null)
            {
                tree.SetRegrowthDuration(currentHarvestDuration);
            }
        }
    }

    private void Update()
    {
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
            if (nextIndex != -1)
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
        if (harvestDurationText != null)
        {
            if (currentHarvestDuration > 0.2f)
            {
                float nextDuration = Mathf.Max(0.2f, currentHarvestDuration - 0.5f);
                harvestDurationText.text = $"{currentHarvestDuration}s-><color=#2ecc71>{nextDuration:F1}s</color>";
            }
            else
            {
                harvestDurationText.text = $"{currentHarvestDuration:F1}s (Max)";
            }
        }
        if (fruitIncomeText != null)
        {
            int nextIncome = currentIncome + 5;
            fruitIncomeText.text = $"{currentIncome}-><color=#2ecc71>{nextIncome}</color>";
        }
        if (incomeCostText != null) incomeCostText.text = incomeUpgradeCost.ToString();
        if (speedCostText != null) speedCostText.text = speedUpgradeCost.ToString();
        if (buyTreeCostText != null) buyTreeCostText.text = treePurchaseCost.ToString();
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
    /// Oyuncu parası yettiğinde ve gerekli meyve seviyesine ulaştığında sıradaki ağacı satın alır.
    /// </summary>
    /// <summary>
    /// Oyuncu parası yettiğinde ve gerekli meyve seviyesine ulaştığında sıradaki ağacı satın alır.
    /// </summary>
    public void Upgrade()
    {
        AddNewTree(); // Geriye uyumluluk için eski metodu yeni metoda bağlıyoruz
    }

    /// <summary>
    /// Upgrade menüsündeki "Ağaç Ekle" butonuna tıklandığında çağrılır.
    /// Gerekli bakiye ve meyve seviyesi şartı sağlandıysa yeni bir ağaç satın alıp aktif eder.
    /// </summary>
    public void AddNewTree()
    {
        if (GameManager.Instance == null) return;

        int nextIndex = GetNextInactiveTreeIndex();
        if (nextIndex != -1)
        {
            Tree nextInactiveTree = targetTrees[nextIndex];
            FruitData fruit = targetTrees[0]?.FruitData;

            if (fruit != null)
            {
                int requiredLevel = nextIndex * 4;
                if (fruit.Level < requiredLevel)
                {
                    Debug.LogWarning($"[HarvestZone] Bu ağacı açmak/eklemek için meyve seviyesi en az {requiredLevel} olmalı!");
                    return;
                }
            }

            if (GameManager.Instance.PlayerMoney >= treePurchaseCost)
            {
                bool success = GameManager.Instance.RemoveMoney(treePurchaseCost);
                if (success)
                {
                    // Ağacı doğrudan aktif ediyoruz (animasyonsuz)
                    nextInactiveTree.gameObject.SetActive(true);

                    // Buton ve UI durumunu hemen güncelle
                    UpdateUpgradeButtonState();
                    UpdateUpgradeUIDisplay();
                    Debug.Log($"[HarvestZone] Yeni ağaç başarıyla eklendi: {nextInactiveTree.name}");
                }
            }
            else
            {
                Debug.LogWarning($"[HarvestZone] Yetersiz bakiye! Ağaç ekleme maliyeti: {treePurchaseCost}");
            }
        }
    }

    /// <summary>
    /// Hasat alanındaki ağacın kazancını (BasePrice) artırır.
    /// </summary>
    public void UpgradeIncome()
    {
        if (GameManager.Instance == null) return;

        FruitData fruit = targetTrees[0]?.FruitData;
        if (fruit == null) return;

        if (GameManager.Instance.PlayerMoney >= incomeUpgradeCost)
        {
            bool success = GameManager.Instance.RemoveMoney(incomeUpgradeCost);
            if (success)
            {
                currentIncome += 5; // Kazancı 5 birim artır
                fruit.SetBasePrice(currentIncome);

                // Geliştirme maliyetini artır
                incomeUpgradeCost += 25;

                // UI metinlerini hemen güncelle
                UpdateUpgradeUIDisplay();
                Debug.Log($"[HarvestZone] Kazanç Yükseltildi! Yeni Kazanç: {currentIncome}, Sıradaki Maliyet: {incomeUpgradeCost}");
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
                // Süreyi 0.5 saniye azalt (en az 0.2 saniye olabilir)
                currentHarvestDuration = Mathf.Max(0.2f, currentHarvestDuration - 0.5f);

                // Hasat yenilenme süresini tüm ağaçlara uyguluyoruz
                foreach (Tree tree in targetTrees)
                {
                    if (tree != null)
                    {
                        tree.SetRegrowthDuration(currentHarvestDuration);
                    }
                }

                // Geliştirme maliyetini artır
                speedUpgradeCost += 25;

                // UI metinlerini hemen güncelle
                UpdateUpgradeUIDisplay();
                Debug.Log($"[HarvestZone] Hasat Süresi Kısaltıldı! Yeni Süre: {currentHarvestDuration}s, Sıradaki Maliyet: {speedUpgradeCost}");
            }
        }
        else
        {
            Debug.LogWarning($"[HarvestZone] Yetersiz bakiye! Hız yükseltme maliyeti: {speedUpgradeCost}");
        }
    }

    private Tree GetFirstInactiveTree()
    {
        int index = GetNextInactiveTreeIndex();
        if (index != -1)
        {
            return targetTrees[index];
        }
        return null;
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

                Tree nextTree = FindNextTreeWithFruit();

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

    private Tree FindNextTreeWithFruit()
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

    private GameObject CreateVisualApple(Tree tree)
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
