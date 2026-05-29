using System.Collections.Generic;
using UnityEngine;

public class PlayerFruitUI : MonoBehaviour
{
    [Header("Inventory Reference")]
    [SerializeField] private PlayerInventory playerInventory;

    [Header("UI Canvas & Container")]
    [SerializeField] private RectTransform listContainer;

    [Header("Prefab")]
    [SerializeField] private GameObject fruitPanelPrefab;

    [Header("Camera Facing (Billboard)")]
    [SerializeField] private bool billboardToCamera = true;
    [SerializeField] private Canvas targetCanvas;

    private List<GameObject> activePanels = new List<GameObject>();

    private void Awake()
    {
        if (playerInventory == null)
        {
            playerInventory = GetComponentInParent<PlayerInventory>();
            if (playerInventory == null)
            {
                playerInventory = GetComponentInChildren<PlayerInventory>();
            }
        }

        if (playerInventory == null)
        {
            }

        if (targetCanvas == null)
        {
            targetCanvas = GetComponentInChildren<Canvas>();
            if (targetCanvas == null)
            {
                targetCanvas = GetComponentInParent<Canvas>();
            }
        }

        if (listContainer == null && targetCanvas != null)
        {
            listContainer = targetCanvas.GetComponent<RectTransform>();
        }

        if (billboardToCamera)
        {
            Transform billboardTarget = targetCanvas != null ? targetCanvas.transform : listContainer;
            if (billboardTarget != null)
            {
                BillboardUI billboard = billboardTarget.GetComponent<BillboardUI>();
                if (billboard == null)
                {
                    billboard = billboardTarget.gameObject.AddComponent<BillboardUI>();
                }
                billboard.BillboardToCamera = true;
            }
        }
    }

    private void Start()
    {
        if (playerInventory != null)
        {
            playerInventory.OnInventoryChanged += RefreshUI;
            RefreshUI();
        }
    }

    private void OnDestroy()
    {
        if (playerInventory != null)
        {
            playerInventory.OnInventoryChanged -= RefreshUI;
        }
    }

    public void RefreshUI()
    {
        if (playerInventory == null || listContainer == null || fruitPanelPrefab == null)
        {
            return;
        }

        ClearActivePanels();

        foreach (var carriedItem in playerInventory.CarriedItems)
        {
            if (carriedItem.fruit == null || carriedItem.amount <= 0) continue;

            GameObject panelInstance = Instantiate(fruitPanelPrefab, listContainer, false);
            
            if (panelInstance != null)
            {
                activePanels.Add(panelInstance);

                FruitUIElement uiElement = panelInstance.GetComponent<FruitUIElement>();
                if (uiElement == null)
                {
                    uiElement = panelInstance.AddComponent<FruitUIElement>();
                }

                uiElement.Setup(carriedItem.fruit.FruitIcon, carriedItem.amount);
            }
        }
    }

    private void ClearActivePanels()
    {
        foreach (var panel in activePanels)
        {
            if (panel != null)
            {
                Destroy(panel);
            }
        }
        activePanels.Clear();
    }
}
