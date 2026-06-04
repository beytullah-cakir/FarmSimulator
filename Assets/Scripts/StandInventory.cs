using System.Collections.Generic;
using UnityEngine;

public class StandInventory : MonoBehaviour
{
    [SerializeField] private int maxCapacity = 50;
    [SerializeField] private List<StandItem> storedItems = new List<StandItem>();

    [System.Serializable]
    public class StandItem
    {
        public FruitData fruit;
        public int amount;

        public StandItem(FruitData fruit, int amount)
        {
            this.fruit = fruit;
            this.amount = amount;
        }
    }

    public int MaxCapacity => maxCapacity;
    public List<StandItem> StoredItems => storedItems;

    public int CurrentCount
    {
        get
        {
            int count = 0;
            foreach (var item in storedItems) count += item.amount;
            return count;
        }
    }

    public bool CanStoreMore() => CurrentCount < maxCapacity;

    public int AddFruit(FruitData fruit, int amount)
    {
        if (amount <= 0 || fruit == null) return 0;

        int amountToAdd = Mathf.Min(amount, Mathf.Max(0, maxCapacity - CurrentCount));
        if (amountToAdd <= 0) return 0;

        StandItem existingItem = storedItems.Find(item => item.fruit == fruit);
        if (existingItem != null)
            existingItem.amount += amountToAdd;
        else
            storedItems.Add(new StandItem(fruit, amountToAdd));

        return amountToAdd;
    }

    public bool RemoveFruit(FruitData fruit, int amount)
    {
        if (fruit == null || amount <= 0) return false;

        StandItem item = storedItems.Find(i => i.fruit == fruit);
        if (item == null || item.amount < amount) return false;

        item.amount -= amount;
        if (item.amount <= 0) storedItems.Remove(item);
        return true;
    }

    public void ClearStand() => storedItems.Clear();
}
