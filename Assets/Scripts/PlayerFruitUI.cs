using UnityEngine;

public class PlayerFruitUI : MonoBehaviour
{
    [SerializeField] private PlayerInventory playerInventory;

    private void Start()
    {
        if (playerInventory != null)
            playerInventory.OnInventoryChanged += UpdateUI;
    }

    private void OnDestroy()
    {
        if (playerInventory != null)
            playerInventory.OnInventoryChanged -= UpdateUI;
    }

    private void UpdateUI()
    {
        // Meyve listesi UI güncellemesi
    }
}
