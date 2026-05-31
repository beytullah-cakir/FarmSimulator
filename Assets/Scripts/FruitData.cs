using UnityEngine;

[CreateAssetMenu(fileName = "NewFruitData", menuName = "Farm Simulator/Fruit Data")]
public class FruitData : ScriptableObject
{
    [SerializeField] private string fruitName;

    [SerializeField] private int basePrice = 10;

    [SerializeField] private GameObject fruitPrefab;

    [SerializeField] private Sprite fruitIcon;

    public string FruitName => fruitName;
    public int BasePrice => basePrice;
    public GameObject FruitPrefab => fruitPrefab;
    public Sprite FruitIcon => fruitIcon;

    public void SetBasePrice(int price)
    {
        basePrice = price;
    }
}
