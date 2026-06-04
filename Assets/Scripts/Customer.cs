using System.Collections.Generic;
using UnityEngine;

public class Customer : MonoBehaviour
{

    [SerializeField] private int minFruitTypes = 1;
    [SerializeField] private int maxFruitTypes = 3;
    [SerializeField] private int minQuantity = 1;
    [SerializeField] private int maxQuantity = 5;

    [SerializeField] private Canvas overheadCanvas;
    private RectTransform uiParentContainer;
    [SerializeField] private GameObject fruitPanelPrefab;


    [SerializeField] private List<FruitOrder> activeOrders = new List<FruitOrder>();
    private Dictionary<FruitData, FruitUIElement> fruitUIElements = new Dictionary<FruitData, FruitUIElement>();
    private int totalOrderPrice;

    public bool IsOrderSatisfied
    {
        get
        {
            if (activeOrders.Count == 0) return false;
            foreach (var o in activeOrders)
            {
                if (o.Amount > 0) return false;
            }
            return true;
        }
    }


    BillboardUI billboard;

    private void Awake()
    {

        billboard = overheadCanvas.GetComponent<BillboardUI>();
        uiParentContainer = overheadCanvas.GetComponent<RectTransform>();
    }

    private void Start()
    {
        GenerateRandomOrder();
    }

    void LateUpdate()
    {
        billboard.MainCode();
    }

    public void GenerateRandomOrder()
    {
        activeOrders.Clear();
        totalOrderPrice = 0;
        List<FruitData> activeFruits = GameManager.Instance.GetActiveFruits();
        int typesToRequest = Random.Range(minFruitTypes, activeFruits.Count + 1);
        List<FruitData> shuffled = new List<FruitData>(activeFruits);

        // Shuffle the list of active fruits to pick random types
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
            activeOrders.Add(new FruitOrder { Fruit = fruit, Amount = qty });
            totalOrderPrice += fruit.BasePrice * qty;

        }


        SetupRequestUI();
    }

    private void SetupRequestUI()
    {
        if (activeOrders.Count == 0 || overheadCanvas == null) return;

        overheadCanvas.gameObject.SetActive(true);

        // Clear old UI elements
        foreach (var ui in fruitUIElements.Values)
        {
            if (ui != null) Destroy(ui.gameObject);
        }
        fruitUIElements.Clear();

        // Create one FruitUIElement panel per fruit type in the order
        foreach (var o in activeOrders)
        {
            if (fruitPanelPrefab != null && uiParentContainer != null)
            {
                GameObject panelInstance = Instantiate(fruitPanelPrefab, uiParentContainer, false);
                if (panelInstance != null)
                {
                    FruitUIElement uiElement = panelInstance.GetComponent<FruitUIElement>();
                    fruitUIElements[o.Fruit] = uiElement;
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
            overheadCanvas.gameObject.SetActive(false);
            return;
        }

        foreach (var o in activeOrders)
        {
            FruitData fruit = o.Fruit;
            int remaining = o.Amount;

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
    public bool DeliverFruit(FruitData fruit, int amount)
    {
        if (fruit == null || amount <= 0) return false;

        FruitOrder matchingOrder = activeOrders.Find(o => o.Fruit == fruit);
        if (matchingOrder == null) return false;
        if (matchingOrder.Amount <= 0) return false;

        int accepted = Mathf.Min(amount, matchingOrder.Amount);
        matchingOrder.Amount -= accepted;


        UpdateRequestUI();

        if (IsOrderSatisfied)
        {
            OnOrderCompleted();
        }

        return true;
    }

    private void OnOrderCompleted()
    {

        GameManager.Instance.AddMoney(totalOrderPrice);



        CustomerQueueManager.Instance.OnCustomerServed(GetComponent<CustomerController>());

    }

    public void SetCanvasActive(bool active)
    {
        if (overheadCanvas != null)
        {
            overheadCanvas.gameObject.SetActive(active);
        }
    }

    /// <summary>
    /// Returns the current order dictionary (fruit -> remaining amount).
    /// </summary>
    public Dictionary<FruitData, int> GetOrder()
    {
        Dictionary<FruitData, int> dict = new Dictionary<FruitData, int>();
        foreach (var o in activeOrders)
        {
            if (o.Fruit != null)
            {
                dict[o.Fruit] = o.Amount;
            }
        }
        return dict;
    }
}
public class FruitOrder
{
    public FruitData Fruit;
    public int Amount;
}
