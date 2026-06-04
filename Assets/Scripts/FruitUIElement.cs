using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class FruitUIElement : MonoBehaviour
{
    [SerializeField] private Image fruitIconImage;
    [SerializeField] private TextMeshProUGUI quantityText;
    public void Setup(Sprite icon, int amount)
    {
        fruitIconImage.sprite = icon;
        fruitIconImage.color = Color.white;
        quantityText.text = $"x{amount}";
    }
}
