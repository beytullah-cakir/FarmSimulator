using UnityEngine;

[CreateAssetMenu(fileName = "NewFruitData", menuName = "Farm Simulator/Fruit Data")]
public class FruitData : ScriptableObject
{
 
    [SerializeField] private string fruitName;

    [SerializeField] private int basePrice = 10;

    [SerializeField] private GameObject fruitPrefab;

    [SerializeField] private Sprite fruitIcon;

    [SerializeField] private int level = 1;

    public string FruitName => fruitName;
    public int BasePrice => basePrice;
    public GameObject FruitPrefab => fruitPrefab;
    public Sprite FruitIcon => fruitIcon;
    public int Level => level;

    public void SetBasePrice(int price)
    {
        basePrice = price;
    }

    public void SetLevel(int newLevel)
    {
        level = newLevel;
    }

    public void UpgradeLevel()
    {
        level++;
    }
}
