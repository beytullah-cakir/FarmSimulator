using UnityEngine;

public class Tree : MonoBehaviour
{
    [SerializeField] private FruitData fruitData;

    [SerializeField] private int currentAmount;

    [SerializeField] private int maxAmount;
    [SerializeField] private int currentPrice;

    // Public properties to access tree data programmatically
    public FruitData FruitData => fruitData;
    public int CurrentAmount => currentAmount;
    public int MaxAmount => maxAmount;
    public int CurrentPrice => currentPrice;

    private void Awake()
    {
        currentPrice = fruitData.BasePrice;

    }


    public int Harvest(int amountToHarvest)
    {
        int harvested = Mathf.Min(amountToHarvest, currentAmount);
        currentAmount -= harvested;
        return harvested;
    }

    public void Regrow(int amountToRegrow)
    {
        currentAmount = Mathf.Min(currentAmount + amountToRegrow, maxAmount);
    }

    public void UpdatePrice(int newPrice)
    {
        currentPrice = newPrice;
    }
}
