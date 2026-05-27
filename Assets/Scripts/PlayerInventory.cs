using System.Collections.Generic;
using UnityEngine;

public class PlayerInventory : MonoBehaviour
{
    [Header("Capacity Settings")]
    [Tooltip("Maximum number of items the player can carry at once.")]
    [SerializeField] private int maxCapacity = 10;

    [Header("Inventory State")]
    [Tooltip("List of items currently being carried.")]
    [SerializeField] private List<CarriedItem> carriedItems = new List<CarriedItem>();

    [SerializeField] private int currentCarryCount = 0;

    // Public properties to read data safely
    public int MaxCapacity => maxCapacity;
    public int CurrentCarryCount => currentCarryCount;
    public List<CarriedItem> CarriedItems => carriedItems;

    [System.Serializable]
    public class CarriedItem
    {
        public FruitData fruit;
        public int amount;

        public CarriedItem(FruitData fruit, int amount)
        {
            this.fruit = fruit;
            this.amount = amount;
        }
    }

    public bool CanCarryMore()
    {
        return currentCarryCount < maxCapacity;
    }

    public int GetSpaceAvailable()
    {
        return Mathf.Max(0, maxCapacity - currentCarryCount);
    }

    public int AddFruit(FruitData fruit, int amount)
    {
        if (amount <= 0 || fruit == null) return 0;

        int spaceAvailable = GetSpaceAvailable();
        int amountToAdd = Mathf.Min(amount, spaceAvailable);

        if (amountToAdd > 0)
        {
            CarriedItem existingItem = carriedItems.Find(item => item.fruit == fruit);

            if (existingItem != null)
            {
                existingItem.amount += amountToAdd;
            }
            else
            {
                carriedItems.Add(new CarriedItem(fruit, amountToAdd));
            }

            currentCarryCount += amountToAdd;
        }

        return amountToAdd;
    }

    public bool RemoveFruit(FruitData fruit, int amount)
    {
        if (fruit == null || amount <= 0) return false;

        CarriedItem item = carriedItems.Find(i => i.fruit == fruit);

        if (item != null && item.amount >= amount)
        {
            item.amount -= amount;
            currentCarryCount -= amount;

            if (item.amount <= 0)
            {
                carriedItems.Remove(item);
            }
            return true;
        }

        return false;
    }

    public void ClearInventory()
    {
        carriedItems.Clear();
        currentCarryCount = 0;
    }

    public void UpgradeCapacity(int capacityIncrease)
    {
        maxCapacity += capacityIncrease;
    }
}
