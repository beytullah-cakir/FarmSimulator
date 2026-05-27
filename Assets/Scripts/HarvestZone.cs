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
            // 1. Check if the player has capacity to carry more items
            if (activeInventory.CanCarryMore())
            {
                // 2. Find the first tree in the sequence (0, 1, 2, 3...) that currently has fruits
                Tree nextTree = FindNextTreeWithFruit();

                if (nextTree != null)
                {
                    // 3. Harvest exactly 1 fruit from this tree
                    int harvested = nextTree.Harvest(1);

                    if (harvested > 0)
                    {
                        // 4. Add the harvested fruit to the player's inventory
                        activeInventory.AddFruit(nextTree.FruitData, harvested);
                    }
                }
            }
            else
            {
                // Optional: Player is full. We can output a log or let the loop wait
                // Debug.Log("Carry capacity reached! Stand outside to stop warning.");
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
}
