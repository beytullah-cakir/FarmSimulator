using UnityEngine;

[CreateAssetMenu(fileName = "NewFruitData", menuName = "Farm Simulator/Fruit Data")]
public class FruitData : ScriptableObject
{
 
    [SerializeField] private string fruitName = "Apple";

    [SerializeField] private int basePrice = 10;

    [SerializeField] private GameObject fruitPrefab;

    public string FruitName => fruitName;
    public int BasePrice => basePrice;
    public GameObject FruitPrefab => fruitPrefab;
}
