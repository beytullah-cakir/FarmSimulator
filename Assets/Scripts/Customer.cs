using System.Collections.Generic;
using UnityEngine;

public class Customer : MonoBehaviour
{
    [Header("Order Settings")]
    [SerializeField] private List<FruitData> possibleFruits;
    [Tooltip("Min number of different fruit TYPES in one order.")]
    [SerializeField] private int minFruitTypes = 1;
    [Tooltip("Max number of different fruit TYPES in one order.")]
    [SerializeField] private int maxFruitTypes = 3;
    [Tooltip("Min quantity per fruit type.")]
    [SerializeField] private int minQuantity = 1;
    [Tooltip("Max quantity per fruit type.")]
    [SerializeField] private int maxQuantity = 5;

    [Header("UI Configuration")]
    [SerializeField] private Canvas overheadCanvas;
    [SerializeField] private RectTransform uiParentContainer;
    [SerializeField] private GameObject fruitPanelPrefab;

    [Header("Camera Facing (Billboard)")]
    [SerializeField] private bool billboardToCamera = true;

    // --- Order Data ---
    // Each entry: Key = FruitData, Value = remaining amount needed
    private Dictionary<FruitData, int> order = new Dictionary<FruitData, int>();
    // Tracks the original amounts for payment calculation
    private Dictionary<FruitData, int> originalOrder = new Dictionary<FruitData, int>();
    // UI elements per fruit type
    private Dictionary<FruitData, FruitUIElement> fruitUIElements = new Dictionary<FruitData, FruitUIElement>();

    public bool IsOrderSatisfied
    {
        get
        {
            foreach (var entry in order)
            {
                if (entry.Value > 0) return false;
            }
            return order.Count > 0;
        }
    }

    // Legacy single-fruit compatibility (for CustomerController etc.)
    public FruitData RequestedFruit { get; private set; }
    public int RequestedAmount { get; private set; }
    public int RemainingAmount { get; private set; }

    private void Awake()
    {
        if (overheadCanvas == null)
        {
            overheadCanvas = GetComponentInChildren<Canvas>(true);
        }

        if (uiParentContainer == null && overheadCanvas != null)
        {
            uiParentContainer = overheadCanvas.GetComponent<RectTransform>();
        }

        if (billboardToCamera && overheadCanvas != null)
        {
            BillboardUI billboard = overheadCanvas.GetComponent<BillboardUI>();
            if (billboard == null)
            {
                billboard = overheadCanvas.gameObject.AddComponent<BillboardUI>();
            }
            billboard.BillboardToCamera = true;
        }
    }

    private void Start()
    {
        GenerateRandomOrder();
    }

    public void GenerateRandomOrder()
    {
        order.Clear();
        originalOrder.Clear();

        // Active fruits from GameManager, fallback to inspector list
        List<FruitData> activeFruits = null;
        if (GameManager.Instance != null)
        {
            activeFruits = GameManager.Instance.GetActiveFruits();
        }

        if (activeFruits == null || activeFruits.Count == 0)
        {
            activeFruits = possibleFruits;
        }

        if (activeFruits == null || activeFruits.Count == 0) return;

        // How many different types does this customer want?
        int typesToRequest = Random.Range(minFruitTypes, Mathf.Min(maxFruitTypes, activeFruits.Count) + 1);

        // Shuffle a copy of the list to pick random types without repetition
        List<FruitData> shuffled = new List<FruitData>(activeFruits);
        for (int i = shuffled.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            FruitData temp = shuffled[i];
            shuffled[i] = shuffled[j];
            shuffled[j] = temp;
        }

        for (int i = 0; i < typesToRequest; i++)
        {
            FruitData fruit = shuffled[i];
            int qty = Random.Range(minQuantity, maxQuantity + 1);
            order[fruit] = qty;
            originalOrder[fruit] = qty;
        }

        // Keep legacy fields pointing to first item for compatibility
        if (typesToRequest > 0)
        {
            RequestedFruit = shuffled[0];
            RequestedAmount = order[RequestedFruit];
            RemainingAmount = RequestedAmount;
        }

        SetupRequestUI();
    }

    private void SetupRequestUI()
    {
        if (order.Count == 0 || overheadCanvas == null) return;

        overheadCanvas.gameObject.SetActive(true);

        // Clear old UI elements
        foreach (var ui in fruitUIElements.Values)
        {
            if (ui != null) Destroy(ui.gameObject);
        }
        fruitUIElements.Clear();

        // Create one FruitUIElement panel per fruit type in the order
        foreach (var entry in order)
        {
            if (fruitPanelPrefab != null && uiParentContainer != null)
            {
                GameObject panelInstance = Instantiate(fruitPanelPrefab, uiParentContainer, false);
                if (panelInstance != null)
                {
                    FruitUIElement uiElement = panelInstance.GetComponent<FruitUIElement>();
                    if (uiElement == null)
                    {
                        uiElement = panelInstance.AddComponent<FruitUIElement>();
                    }
                    fruitUIElements[entry.Key] = uiElement;
                }
            }
        }

        UpdateRequestUI();
    }

    public void UpdateRequestUI()
    {
        bool allDone = IsOrderSatisfied;

        if (allDone)
        {
            if (overheadCanvas != null)
            {
                overheadCanvas.gameObject.SetActive(false);
            }
            return;
        }

        foreach (var entry in order)
        {
            FruitData fruit = entry.Key;
            int remaining = entry.Value;

            if (fruitUIElements.TryGetValue(fruit, out FruitUIElement uiElement) && uiElement != null)
            {
                if (remaining <= 0)
                {
                    // Hide completed items
                    uiElement.gameObject.SetActive(false);
                }
                else
                {
                    uiElement.gameObject.SetActive(true);
                    uiElement.Setup(fruit.FruitIcon, remaining);
                }
            }
        }
    }

    /// <summary>
    /// Attempts to deliver an amount of a specific fruit. Returns how many were accepted.
    /// </summary>
    public bool DeliverFruit(FruitData fruit, int amount)
    {
        if (fruit == null || amount <= 0) return false;
        if (!order.ContainsKey(fruit)) return false;
        if (order[fruit] <= 0) return false;

        int accepted = Mathf.Min(amount, order[fruit]);
        order[fruit] -= accepted;

        // Keep legacy fields in sync
        if (fruit == RequestedFruit)
        {
            RemainingAmount = order[fruit];
        }

        UpdateRequestUI();

        if (IsOrderSatisfied)
        {
            OnOrderCompleted();
        }

        return true;
    }

    private void OnOrderCompleted()
    {
        int totalPayment = 0;
        foreach (var entry in originalOrder)
        {
            if (entry.Key != null)
            {
                totalPayment += entry.Key.BasePrice * entry.Value;
            }
        }

        if (GameManager.Instance != null && totalPayment > 0)
        {
            GameManager.Instance.AddMoney(totalPayment);
        }

        if (CustomerQueueManager.Instance != null)
        {
            CustomerQueueManager.Instance.OnCustomerServed(GetComponent<CustomerController>());
        }
    }

    /// <summary>
    /// Returns the current order dictionary (fruit -> remaining amount).
    /// </summary>
    public Dictionary<FruitData, int> GetOrder() => order;
}
