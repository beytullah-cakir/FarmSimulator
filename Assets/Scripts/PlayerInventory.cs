using System.Collections.Generic;
using UnityEngine;

public class PlayerInventory : MonoBehaviour
{
    [SerializeField] private int maxCapacity = 10;
    [SerializeField] private List<CarriedItem> carriedItems = new List<CarriedItem>();
    [SerializeField] private int currentCarryCount = 0;
    [SerializeField] private Canvas maxCanvas;

    public int MaxCapacity => maxCapacity;
    public int CurrentCarryCount => currentCarryCount;
    public List<CarriedItem> CarriedItems => carriedItems;

    public event System.Action OnInventoryChanged;

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

    private void Start()
    {
        if (maxCanvas != null)
            maxCanvas.gameObject.SetActive(false);
    }

    public bool CanCarryMore() => currentCarryCount < maxCapacity;

    public int GetSpaceAvailable() => Mathf.Max(0, maxCapacity - currentCarryCount);

    public int AddFruit(FruitData fruit, int amount)
    {
        if (amount <= 0 || fruit == null) return 0;

        int amountToAdd = Mathf.Min(amount, GetSpaceAvailable());
        if (amountToAdd <= 0) return 0;

        CarriedItem existingItem = carriedItems.Find(item => item.fruit == fruit);
        if (existingItem != null)
            existingItem.amount += amountToAdd;
        else
            carriedItems.Add(new CarriedItem(fruit, amountToAdd));

        currentCarryCount += amountToAdd;
        OnInventoryChanged?.Invoke();
        UpdateMaxCanvas();
        return amountToAdd;
    }

    public bool RemoveFruit(FruitData fruit, int amount)
    {
        if (fruit == null || amount <= 0) return false;

        CarriedItem item = carriedItems.Find(i => i.fruit == fruit);
        if (item == null || item.amount < amount) return false;

        item.amount -= amount;
        currentCarryCount -= amount;

        if (item.amount <= 0) carriedItems.Remove(item);

        OnInventoryChanged?.Invoke();
        UpdateMaxCanvas();
        return true;
    }

    public void ClearInventory()
    {
        carriedItems.Clear();
        currentCarryCount = 0;
        OnInventoryChanged?.Invoke();
        UpdateMaxCanvas();
    }

    public void UpgradeCapacity(int capacityIncrease) => maxCapacity += capacityIncrease;

    private void UpdateMaxCanvas()
    {
        if (maxCanvas != null)
            maxCanvas.gameObject.SetActive(!CanCarryMore());
    }
}
