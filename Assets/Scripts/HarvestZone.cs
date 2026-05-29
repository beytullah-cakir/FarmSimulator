using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HarvestZone : MonoBehaviour
{
    [Header("Target Trees Configuration")]
    [Tooltip("List of trees to be harvested in sequence. Assign your 4 trees here in the desired order (0 to 3).")]
    [SerializeField] private List<Tree> targetTrees = new List<Tree>();

    [Header("Harvest Settings")]
    [Tooltip("Time interval (in seconds) between harvesting each individual fruit.")]
    [SerializeField] private float harvestInterval = 0.2f;

    private PlayerInventory activeInventory;
    private Coroutine harvestCoroutine;
    private int reservedSpace = 0;

    private void OnTriggerEnter(Collider other)
    {
        // Check if the entering object is the player and has the inventory component
        PlayerInventory inventory = other.GetComponent<PlayerInventory>();
        if (inventory != null)
        {
            activeInventory = inventory;
            
            // Start the sequential harvesting process
            if (harvestCoroutine != null)
            {
                StopCoroutine(harvestCoroutine);
            }
            harvestCoroutine = StartCoroutine(HarvestRoutine());
        }
    }

    private void OnTriggerExit(Collider other)
    {
        // Stop harvesting when the player exits the zone
        PlayerInventory inventory = other.GetComponent<PlayerInventory>();
        if (inventory != null && activeInventory == inventory)
        {
            activeInventory = null;
            if (harvestCoroutine != null)
            {
                StopCoroutine(harvestCoroutine);
                harvestCoroutine = null;
            }
        }
    }

    /// <summary>
    /// Coroutine that tick-harvests fruits one-by-one from the assigned trees sequentially.
    /// </summary>
    private IEnumerator HarvestRoutine()
    {
        while (activeInventory != null)
        {
            // Prevent harvesting if the player controller is currently disabled (e.g. during a cutscene)
            PlayerController player = activeInventory.GetComponent<PlayerController>();
            if (player != null && !player.enabled)
            {
                yield return new WaitForSeconds(harvestInterval);
                continue;
            }

            // 1. Check if the player has capacity to carry more items (taking reserved mid-air fruits into account)
            int currentSpaceAvailable = activeInventory.GetSpaceAvailable() - reservedSpace;
            if (currentSpaceAvailable > 0)
            {
                // 2. Find the first tree in the sequence (0, 1, 2, 3...) that currently has fruits
                Tree nextTree = FindNextTreeWithFruit();

                if (nextTree != null)
                {
                    // Reserve space for the incoming apple
                    reservedSpace++;

                    // 3. Harvest exactly 1 fruit from this tree
                    int harvested = nextTree.Harvest(1);

                    if (harvested > 0)
                    {
                        Debug.Log($"[Hasat] {nextTree.FruitData.FruitName} toplanıyor...");
                        // Create a new visual apple object at the tree's position for flight animation
                        GameObject visualApple = CreateVisualApple(nextTree);

                        if (visualApple != null)
                        {
                            // Start animating the apple flight to the player
                            StartCoroutine(AnimateAppleFly(visualApple, activeInventory, nextTree.FruitData));
                        }
                        else
                        {
                            // Fallback: Add directly to inventory if visual cannot be resolved
                            activeInventory.AddFruit(nextTree.FruitData, 1);
                            reservedSpace = Mathf.Max(0, reservedSpace - 1);
                        }
                    }
                    else
                    {
                        // Harvest failed, decrement reserve
                        reservedSpace = Mathf.Max(0, reservedSpace - 1);
                    }
                }
            }

            // Wait for the specified interval before the next harvest tick
            yield return new WaitForSeconds(harvestInterval);
        }
    }

    /// <summary>
    /// Searches the tree list in sequence and returns the first tree that has fruits remaining.
    /// </summary>
    private Tree FindNextTreeWithFruit()
    {
        for (int i = 0; i < targetTrees.Count; i++)
        {
            if (targetTrees[i] != null && targetTrees[i].CurrentAmount > 0)
            {
                return targetTrees[i];
            }
        }
        return null; // All trees in the list are empty
    }

    /// <summary>
    /// Spawns a visual apple prefab at the tree canopy position for the flight animation.
    /// </summary>
    private GameObject CreateVisualApple(Tree tree)
    {
        if (tree.FruitData != null && tree.FruitData.FruitPrefab != null)
        {
            // Spawn the apple prefab at the tree canopy height (approx. 2 units above tree origin)
            return Instantiate(tree.FruitData.FruitPrefab, tree.transform.position + Vector3.up * 2f, Quaternion.identity);
        }
        return null;
    }

    /// <summary>
    /// Animates the apple flying in a parabolic trajectory (vertical throw arc) towards the character.
    /// </summary>
    private IEnumerator AnimateAppleFly(GameObject appleObj, PlayerInventory inventory, FruitData fruitData)
    {
        Vector3 startPos = appleObj.transform.position;
        Quaternion startRot = appleObj.transform.rotation;
        Vector3 startScale = appleObj.transform.localScale;

        float duration = 0.6f; // Flight duration in seconds
        float elapsed = 0f;
        float arcHeight = 3.0f; // Peak height of the parabolic arc

        // Detach the apple from the tree
        appleObj.transform.parent = null;

        // Disable collider so it doesn't interfere during flight
        Collider col = appleObj.GetComponent<Collider>();
        if (col != null)
        {
            col.enabled = false;
        }

        // Disable physics if present
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

            // Target the chest/center of the player
            Vector3 targetPos = inventory.transform.position + Vector3.up * 1f;

            // Parabolic motion: Lerp on horizontal, Sin wave on vertical
            Vector3 currentPos = Vector3.Lerp(startPos, targetPos, t);
            currentPos.y += Mathf.Sin(t * Mathf.PI) * arcHeight;

            appleObj.transform.position = currentPos;

            // Rotate the apple for extra visual detail
            appleObj.transform.rotation = startRot * Quaternion.Euler(t * 360f, t * 720f, 0f);

            // Scale down slightly as it approaches the player
            appleObj.transform.localScale = Vector3.Lerp(startScale, startScale * 0.4f, t);

            yield return null;
        }

        // Destruct and add to inventory
        if (appleObj != null)
        {
            Destroy(appleObj);
        }

        if (inventory != null)
        {
            inventory.AddFruit(fruitData, 1);
            Debug.Log($"[Hasat Bölgesi] {fruitData.FruitName} oyuncuya ulaştı ve envantere eklendi! Envanter: {inventory.CurrentCarryCount}/{inventory.MaxCapacity}");
        }

        reservedSpace = Mathf.Max(0, reservedSpace - 1);
    }
}
