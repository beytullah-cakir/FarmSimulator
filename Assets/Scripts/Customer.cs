using System.Collections.Generic;
using UnityEngine;

public class Customer : MonoBehaviour
{
    [Header("Order Settings")]
    [SerializeField] private List<FruitData> possibleFruits;
    [SerializeField] private int minQuantity = 1;
    [SerializeField] private int maxQuantity = 5;

    [Header("UI Configuration")]
    [SerializeField] private Canvas overheadCanvas;
    [SerializeField] private RectTransform uiParentContainer;
    [SerializeField] private GameObject fruitPanelPrefab;
    [SerializeField] private FruitUIElement requestUIElement;

    [Header("Camera Facing (Billboard)")]
    [SerializeField] private bool billboardToCamera = true;

    public FruitData RequestedFruit { get; private set; }
    public int RequestedAmount { get; private set; }
    public int RemainingAmount { get; private set; }
    public bool IsOrderSatisfied => RemainingAmount <= 0;

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
        SetupRequestUI();
    }

    public void GenerateRandomOrder()
    {
        List<FruitData> activeFruits = null;

        if (GameManager.Instance != null)
        {
            activeFruits = GameManager.Instance.GetActiveFruits();
        }

        if (activeFruits == null || activeFruits.Count == 0)
        {
            activeFruits = possibleFruits;
        }

        if (activeFruits == null || activeFruits.Count == 0)
        {
            return;
        }

        int randomFruitIndex = Random.Range(0, activeFruits.Count);
        RequestedFruit = activeFruits[randomFruitIndex];

        RequestedAmount = Random.Range(minQuantity, maxQuantity + 1);
        RemainingAmount = RequestedAmount;

        }

    private void SetupRequestUI()
    {
        if (RequestedFruit == null || overheadCanvas == null)
        {
            return;
        }

        overheadCanvas.gameObject.SetActive(true);

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

        UpdateRequestUI();
    }

    public void UpdateRequestUI()
    {
        if (requestUIElement == null) return;

        if (IsOrderSatisfied)
        {
            if (overheadCanvas != null)
            {
                overheadCanvas.gameObject.SetActive(false);
            }
        }
        else
        {
            requestUIElement.Setup(RequestedFruit.FruitIcon, RemainingAmount);
        }
    }

    public bool DeliverFruit(FruitData fruit, int amount)
    {
        if (IsOrderSatisfied || fruit != RequestedFruit || amount <= 0)
        {
            return false;
        }

        int acceptedAmount = Mathf.Min(amount, RemainingAmount);
        RemainingAmount -= acceptedAmount;

        UpdateRequestUI();

        if (IsOrderSatisfied)
        {
            OnOrderCompleted();
        }

        return true;
    }

    private void OnOrderCompleted()
    {
        if (RequestedFruit != null)
        {
            int totalPayment = RequestedFruit.BasePrice * RequestedAmount;
            if (GameManager.Instance != null)
            {
                GameManager.Instance.AddMoney(totalPayment);
            }
        }
        
        if (CustomerQueueManager.Instance != null)
        {
            CustomerQueueManager.Instance.OnCustomerServed(GetComponent<CustomerController>());
        }
    }
}
