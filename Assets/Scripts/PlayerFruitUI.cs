using System.Collections.Generic;
using UnityEngine;

public class PlayerFruitUI : MonoBehaviour
{
    [Header("Inventory Reference")]
    [Tooltip("Reference to the PlayerInventory component. If left null, it will be automatically fetched from this GameObject or parent.")]
    [SerializeField] private PlayerInventory playerInventory;

    [Header("UI Canvas & Container")]
    [Tooltip("The parent container where fruit panels will be spawned. If left null, the script will automatically use the Canvas component under the player.")]
    [SerializeField] private RectTransform listContainer;

    [Header("Prefab")]
    [Tooltip("The panel prefab showing fruit information. It should ideally have the FruitUIElement script attached.")]
    [SerializeField] private GameObject fruitPanelPrefab;

    [Header("Camera Facing (Billboard)")]
    [Tooltip("If true, the list container or Canvas will rotate to face the main camera at all times.")]
    [SerializeField] private bool billboardToCamera = true;

    [Tooltip("The Canvas object to face the camera. If null, it will be found automatically in children or parent.")]
    [SerializeField] private Canvas targetCanvas;

    private List<GameObject> activePanels = new List<GameObject>();
    private Camera mainCamera;

    private void Awake()
    {
        // 1. Automatically fetch the PlayerInventory if not assigned
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
            Debug.LogWarning("[PlayerFruitUI] PlayerInventory component not found. Please assign it in the Inspector.");
        }

        // 2. Automatically find targetCanvas if not assigned
        if (targetCanvas == null)
        {
            targetCanvas = GetComponentInChildren<Canvas>();
            if (targetCanvas == null)
            {
                targetCanvas = GetComponentInParent<Canvas>();
            }
        }

        // 3. Fallback: If listContainer is null, directly use the Canvas as the list container
        if (listContainer == null && targetCanvas != null)
        {
            listContainer = targetCanvas.GetComponent<RectTransform>();
        }
    }

    private void Start()
    {
        mainCamera = Camera.main;

        if (playerInventory != null)
        {
            // Subscribe to inventory changes
            playerInventory.OnInventoryChanged += RefreshUI;

            // Perform initial UI refresh
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

    private void LateUpdate()
    {
        // Handle billboarding so the UI always faces the camera
        if (billboardToCamera && mainCamera != null)
        {
            Transform faceTransform = null;
            if (targetCanvas != null)
            {
                faceTransform = targetCanvas.transform;
            }
            else if (listContainer != null)
            {
                faceTransform = listContainer;
            }

            if (faceTransform != null)
            {
                // Face the camera but keep it vertically upright
                Vector3 cameraDirection = mainCamera.transform.forward;
                faceTransform.rotation = Quaternion.LookRotation(cameraDirection, mainCamera.transform.up);
            }
        }
    }

    /// <summary>
    /// Rebuilds the list of fruit panels based on the current inventory contents.
    /// </summary>
    public void RefreshUI()
    {
        if (playerInventory == null || listContainer == null || fruitPanelPrefab == null)
        {
            return;
        }

        // 1. Clear existing panels
        ClearActivePanels();

        // 2. For each unique fruit in the inventory, instantiate a new panel
        foreach (var carriedItem in playerInventory.CarriedItems)
        {
            if (carriedItem.fruit == null || carriedItem.amount <= 0) continue;

            // Instantiate under the list container/Canvas
            GameObject panelInstance = Instantiate(fruitPanelPrefab, listContainer, false);
            
            if (panelInstance != null)
            {
                // Add to active panels list for clean-up later
                activePanels.Add(panelInstance);

                // Get or add the FruitUIElement component to setup the panel
                FruitUIElement uiElement = panelInstance.GetComponent<FruitUIElement>();
                if (uiElement == null)
                {
                    uiElement = panelInstance.AddComponent<FruitUIElement>();
                }

                // Populate with sprite and quantity
                uiElement.Setup(carriedItem.fruit.FruitIcon, carriedItem.amount);
            }
        }
    }

    /// <summary>
    /// Destroys all active instantiated panels.
    /// </summary>
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
