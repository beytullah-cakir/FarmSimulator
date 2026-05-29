using System.Collections.Generic;
using UnityEngine;

public class Customer : MonoBehaviour
{
    [Header("Order Settings")]
    [Tooltip("List of possible fruits this customer can request. Assign your FruitData assets here.")]
    [SerializeField] private List<FruitData> possibleFruits;

    [Tooltip("Minimum quantity of fruit the customer will request.")]
    [SerializeField] private int minQuantity = 1;

    [Tooltip("Maximum quantity of fruit the customer will request.")]
    [SerializeField] private int maxQuantity = 5;

    [Header("UI Configuration")]
    [Tooltip("The overhead Canvas for the customer. If left null, it will search for a Canvas component in children.")]
    [SerializeField] private Canvas overheadCanvas;

    [Tooltip("The parent transform under the Canvas to spawn the request panel. If left null, the Canvas's root transform will be used.")]
    [SerializeField] private RectTransform uiParentContainer;

    [Tooltip("The panel prefab displaying fruit information (must have or be able to add FruitUIElement).")]
    [SerializeField] private GameObject fruitPanelPrefab;

    [Tooltip("A pre-existing FruitUIElement in the scene under the Canvas. If assigned, the script will update this directly instead of instantiating a prefab.")]
    [SerializeField] private FruitUIElement requestUIElement;

    [Header("Camera Facing (Billboard)")]
    [Tooltip("If true, the overhead Canvas will rotate to face the main camera at all times.")]
    [SerializeField] private bool billboardToCamera = true;

    // Current order state
    public FruitData RequestedFruit { get; private set; }
    public int RequestedAmount { get; private set; }
    public int RemainingAmount { get; private set; }
    public bool IsOrderSatisfied => RemainingAmount <= 0;

    private Camera mainCamera;

    private void Awake()
    {
        // 1. Auto-find Canvas if null
        if (overheadCanvas == null)
        {
            overheadCanvas = GetComponentInChildren<Canvas>(true); // Pass true to find inactive as well
        }

        // 2. Auto-find container if null
        if (uiParentContainer == null && overheadCanvas != null)
        {
            uiParentContainer = overheadCanvas.GetComponent<RectTransform>();
        }
    }

    private void Start()
    {
        mainCamera = Camera.main;

        // Initialize customer's random order
        GenerateRandomOrder();

        // Setup the UI panel above the customer's head
        SetupRequestUI();
    }

    private void LateUpdate()
    {
        // Handle billboarding so the customer's overhead UI always faces the camera
        if (billboardToCamera && mainCamera != null && overheadCanvas != null)
        {
            Vector3 cameraDirection = mainCamera.transform.forward;
            overheadCanvas.transform.rotation = Quaternion.LookRotation(cameraDirection, mainCamera.transform.up);
        }
    }

    /// <summary>
    /// Randomly selects a fruit from the possible list and determines a random quantity.
    /// </summary>
    public void GenerateRandomOrder()
    {
        List<FruitData> activeFruits = null;

        // 1. Query GameManager for active (unlocked) fruits if it exists
        if (GameManager.Instance != null)
        {
            activeFruits = GameManager.Instance.GetActiveFruits();
        }

        // 2. Fallback to inspector possibleFruits if GameManager is missing or has no active fruits
        if (activeFruits == null || activeFruits.Count == 0)
        {
            activeFruits = possibleFruits;
        }

        if (activeFruits == null || activeFruits.Count == 0)
        {
            Debug.LogWarning($"[Customer] {gameObject.name} has no active fruits available to request!");
            return;
        }

        // Select a random fruit from the active list
        int randomFruitIndex = Random.Range(0, activeFruits.Count);
        RequestedFruit = activeFruits[randomFruitIndex];

        // Select a random quantity
        RequestedAmount = Random.Range(minQuantity, maxQuantity + 1);
        RemainingAmount = RequestedAmount;

        Debug.Log($"[Müşteri] {gameObject.name} siparişi: {RequestedAmount} adet {RequestedFruit.FruitName}");
    }

    /// <summary>
    /// Prepares and displays the UI panel under the customer's Canvas.
    /// </summary>
    private void SetupRequestUI()
    {
        if (RequestedFruit == null || overheadCanvas == null)
        {
            return;
        }

        overheadCanvas.gameObject.SetActive(true);

        // If we don't have a pre-existing UI element, instantiate the prefab
        if (requestUIElement == null && fruitPanelPrefab != null && uiParentContainer != null)
        {
            GameObject panelInstance = Instantiate(fruitPanelPrefab, uiParentContainer, false);
            if (panelInstance != null)
            {
                requestUIElement = panelInstance.GetComponent<FruitUIElement>();
                if (requestUIElement == null)
                {
                    requestUIElement = panelInstance.AddComponent<FruitUIElement>();
                }
            }
        }

        // Update the UI element with the requested fruit and amount
        UpdateRequestUI();
    }

    /// <summary>
    /// Refreshes the overhead UI values. If the order is satisfied, hides the UI.
    /// </summary>
    public void UpdateRequestUI()
    {
        if (requestUIElement == null) return;

        if (IsOrderSatisfied)
        {
            // Hide the entire Canvas once the customer got everything they wanted
            if (overheadCanvas != null)
            {
                overheadCanvas.gameObject.SetActive(false);
            }
        }
        else
        {
            // Update the Sprite and Quantity Text on the panel
            requestUIElement.Setup(RequestedFruit.FruitIcon, RemainingAmount);
        }
    }

    /// <summary>
    /// Call this function when the player delivers a fruit to this customer.
    /// </summary>
    /// <param name="fruit">The fruit type being delivered.</param>
    /// <param name="amount">The quantity being delivered.</param>
    /// <returns>True if the fruit was accepted, false otherwise.</returns>
    public bool DeliverFruit(FruitData fruit, int amount)
    {
        if (IsOrderSatisfied || fruit != RequestedFruit || amount <= 0)
        {
            return false;
        }

        // Deduct remaining amount
        int acceptedAmount = Mathf.Min(amount, RemainingAmount);
        RemainingAmount -= acceptedAmount;

        Debug.Log($"[Müşteri] {gameObject.name} {acceptedAmount} adet {fruit.FruitName} teslim aldı. Kalan ihtiyaç: {RemainingAmount}");

        // Refresh UI
        UpdateRequestUI();

        if (IsOrderSatisfied)
        {
            OnOrderCompleted();
        }

        return true;
    }

    private void OnOrderCompleted()
    {
        Debug.Log($"[Müşteri] {gameObject.name} siparişi tamamlandı! Teşekkürler!");

        // Pay the player: Calculate price based on RequestedFruit's base price times the requested quantity
        if (RequestedFruit != null)
        {
            int totalPayment = RequestedFruit.BasePrice * RequestedAmount;
            if (GameManager.Instance != null)
            {
                GameManager.Instance.AddMoney(totalPayment);
            }
        }
        
        // Notify the queue manager to handle payment, departure, and queue shifting
        if (CustomerQueueManager.Instance != null)
        {
            CustomerQueueManager.Instance.OnCustomerServed(GetComponent<CustomerController>());
        }
    }
}
