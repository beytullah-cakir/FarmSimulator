using System.Collections.Generic;
using UnityEngine;

public class StandInventory : MonoBehaviour
{
    [SerializeField] private bool unlimitedCapacity = true;
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

    [System.Serializable]
    public class FruitBoxMapping
    {
        public FruitData fruit;
        public GameObject boxObject;
    }

    [SerializeField] private List<FruitBoxMapping> fruitBoxes = new List<FruitBoxMapping>();

    public StandPlaceZone PlaceZone { get; set; }

    public bool UnlimitedCapacity => unlimitedCapacity;
    public int MaxCapacity => unlimitedCapacity ? int.MaxValue : maxCapacity;
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

    public bool CanStoreMore() => unlimitedCapacity || CurrentCount < maxCapacity;

    private void Start()
    {
        UpdateBoxVisuals();
    }

    public void UpdateBoxVisuals()
    {
        if (fruitBoxes == null) return;

        foreach (var mapping in fruitBoxes)
        {
            if (mapping == null || mapping.boxObject == null) continue;

            StandItem item = storedItems.Find(i => i.fruit == mapping.fruit);
            bool hasFruit = (item != null && item.amount > 0);

            mapping.boxObject.SetActive(hasFruit);
        }
    }

    public int AddFruit(FruitData fruit, int amount)
    {
        if (amount <= 0 || fruit == null) return 0;

        int amountToAdd = unlimitedCapacity ? amount : Mathf.Min(amount, Mathf.Max(0, maxCapacity - CurrentCount));
        if (amountToAdd <= 0) return 0;

        StandItem existingItem = storedItems.Find(item => item.fruit == fruit);
        if (existingItem != null)
            existingItem.amount += amountToAdd;
        else
            storedItems.Add(new StandItem(fruit, amountToAdd));

        UpdateBoxVisuals();

        return amountToAdd;
    }

    public bool RemoveFruit(FruitData fruit, int amount)
    {
        if (fruit == null || amount <= 0) return false;

        StandItem item = storedItems.Find(i => i.fruit == fruit);
        if (item == null || item.amount < amount) return false;

        item.amount -= amount;
        if (item.amount <= 0) storedItems.Remove(item);

        UpdateBoxVisuals();

        return true;
    }

    public void ClearStand()
    {
        storedItems.Clear();
        UpdateBoxVisuals();
    }
}
