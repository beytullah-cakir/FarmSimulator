using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HarvestZone : MonoBehaviour
{
    [SerializeField] private List<Tree> targetTrees = new List<Tree>();

    [Header("Harvest Settings")]
    [SerializeField] private float harvestInterval = 0.2f;
   
    [SerializeField] private GameObject upgradeButtonObject;

    private PlayerInventory activeInventory;
    private Coroutine harvestCoroutine;
    private int reservedSpace = 0;

    private void Start()
    {
        upgradeButtonObject.SetActive(false);
    }

    private void OnTriggerEnter(Collider other)
    {

        PlayerInventory inventory = other.GetComponent<PlayerInventory>();
        if (inventory != null)
        {
            activeInventory = inventory;
            upgradeButtonObject?.SetActive(true);

            if (harvestCoroutine != null)
            {
                StopCoroutine(harvestCoroutine);
            }
            harvestCoroutine = StartCoroutine(HarvestRoutine());
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
            if (targetTrees[i] != null && targetTrees[i].CurrentAmount > 0)
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
