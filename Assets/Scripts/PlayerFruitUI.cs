using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PlayerFruitUI : MonoBehaviour
{
    [SerializeField] private PlayerInventory playerInventory;
    [SerializeField] private Transform fruitListParent;
    [SerializeField] private GameObject fruitUIPrefab;

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
        foreach (Transform child in fruitListParent)
            Destroy(child.gameObject);

        foreach (var item in playerInventory.CarriedItems)
        {
            if (item.fruit == null || item.amount <= 0) continue;

            GameObject uiObj = Instantiate(fruitUIPrefab, fruitListParent);
            FruitUIElement element = uiObj.GetComponent<FruitUIElement>();
            if (element != null)
                element.Setup(item.fruit.FruitIcon, item.amount);
        }
    }
}
