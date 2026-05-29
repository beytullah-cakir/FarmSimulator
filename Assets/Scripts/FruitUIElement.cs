using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class FruitUIElement : MonoBehaviour
{
    [Header("UI Component References")]
    [Tooltip("The Image component to display the fruit icon. If left blank, the script will attempt to find one in the children.")]
    [SerializeField] private Image fruitIconImage;

    [Tooltip("The TextMeshProUGUI component to display the fruit quantity. If left blank, the script will attempt to find one in the children.")]
    [SerializeField] private TextMeshProUGUI quantityText;

    /// <summary>
    /// Updates the UI elements of this panel with the given fruit's icon and quantity.
    /// </summary>
    /// <param name="icon">The Sprite of the fruit.</param>
    /// <param name="amount">The quantity of the fruit.</param>
    public void Setup(Sprite icon, int amount)
    {
        // Fallback: Find Image component in children if not assigned
        if (fruitIconImage == null)
        {
            fruitIconImage = GetComponentInChildren<Image>();
        }

        // Fallback: Find TextMeshProUGUI component in children if not assigned
        if (quantityText == null)
        {
            quantityText = GetComponentInChildren<TextMeshProUGUI>();
        }

        // Set the sprite if image and sprite are valid
        if (fruitIconImage != null)
        {
            if (icon != null)
            {
                fruitIconImage.sprite = icon;
                fruitIconImage.color = Color.white; // Ensure fully visible
            }
            else
            {
                fruitIconImage.color = new Color(1f, 1f, 1f, 0f); // Hide/fade if no icon
            }
        }

        // Set the quantity text
        if (quantityText != null)
        {
            quantityText.text = $"x{amount}";
        }
    }
}
