using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HarvestZone : MonoBehaviour
{
    [SerializeField] private List<Prop> targetTrees = new List<Prop>();
    [SerializeField] private float harvestInterval = 0.2f;
    [SerializeField] private GameObject upgradeButtonObject;

    private PlayerInventory activeInventory;
    private Coroutine harvestCoroutine;
    private int reservedSpace = 0;

    public List<Prop> TargetTrees => targetTrees;

    private void Start()
    {
        if (upgradeButtonObject == null)
        {
            Transform btn = transform.Find("UpgradeButton");
            if (btn == null)
            {
                foreach (Transform child in transform)
                {
                    if (child.name.ToLower().Contains("upgrade") || child.name.ToLower().Contains("button"))
                    {
                        upgradeButtonObject = child.gameObject;
                        break;
                    }
                }
            }
            else
            {
                upgradeButtonObject = btn.gameObject;
            }
        }

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
        if (upgradeButtonObject == null) return;

        if (activeInventory != null && UpgradeManager.Instance != null)
        {
            var entry = UpgradeManager.Instance.FindEntryByZone(this);
            if (entry != null)
            {
                upgradeButtonObject.SetActive(true);
                return;
            }
        }
        upgradeButtonObject.SetActive(false);
    }

    private void OnTriggerEnter(Collider other)
    {
        PlayerInventory inventory = other.GetComponentInParent<PlayerInventory>();
        if (inventory == null) return;

        activeInventory = inventory;

        if (UIManager.Instance != null)
            UIManager.Instance.SetActiveHarvestZone(this);

        if (UpgradeManager.Instance != null)
        {
            UpgradeManager.Instance.SetActiveEntry(this);
            UpgradeManager.Instance.UpdateUpgradeUI();
        }

        UpdateUpgradeButtonState();

        if (harvestCoroutine != null) StopCoroutine(harvestCoroutine);
        harvestCoroutine = StartCoroutine(HarvestRoutine());
    }

    private void OnTriggerExit(Collider other)
    {
        PlayerInventory inventory = other.GetComponentInParent<PlayerInventory>();
        if (activeInventory != inventory) return;

        activeInventory = null;

        if (UIManager.Instance != null)
            UIManager.Instance.SetActiveHarvestZone(null);

        if (UpgradeManager.Instance != null)
            UpgradeManager.Instance.SetActiveEntry(null);

        if (upgradeButtonObject != null)
            upgradeButtonObject.SetActive(false);

        if (harvestCoroutine != null)
        {
            StopCoroutine(harvestCoroutine);
            harvestCoroutine = null;
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
}
