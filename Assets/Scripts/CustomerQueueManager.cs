using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CustomerQueueManager : MonoBehaviour
{
    public static CustomerQueueManager Instance { get; private set; }

    [SerializeField] private List<GameObject> customerPrefabs = new List<GameObject>();

    [SerializeField] private int initialPoolSize = 6;

    [SerializeField] private Transform spawnPoint;

    [SerializeField] private Transform exitPoint;

    [SerializeField] private float minSpawnTime = 6f;

    [SerializeField] private float maxSpawnTime = 12f;

    [SerializeField] private Transform firstQueueSpot;

    [SerializeField] private int maxQueueCapacity = 5;

    [SerializeField] private float distanceBetweenSpots = 1.3f;

    [SerializeField] private StandInventory standInventory;

    [SerializeField] private float purchaseCheckInterval = 0.2f;
    private List<GameObject> customerPool = new List<GameObject>();
    private List<CustomerController> activeQueue = new List<CustomerController>();

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        // 1. Pre-populate Object Pool
        InitializeObjectPool();

        // 2. Start dynamic spawning coroutine
        StartCoroutine(SpawnCustomerRoutine());

        // 3. Start purchase check routine
        StartCoroutine(PurchaseTickRoutine());
    }

    private void InitializeObjectPool()
    {


        for (int i = 0; i < initialPoolSize; i++)
        {
            // Pick a random customer prefab from the list to create variety
            int randIndex = Random.Range(0, customerPrefabs.Count);
            GameObject chosenPrefab = customerPrefabs[randIndex];


            GameObject npc = Instantiate(chosenPrefab);
            npc.SetActive(false);

            npc.transform.SetParent(transform);

            customerPool.Add(npc);

        }
    }

    private IEnumerator SpawnCustomerRoutine()
    {

        while (true)
        {
            // Wait for a random interval
            float spawnDelay = Random.Range(minSpawnTime, maxSpawnTime);
            yield return new WaitForSeconds(spawnDelay);

            // Only spawn if our active queue hasn't reached maximum capacity
            if (activeQueue.Count < maxQueueCapacity)
            {
                SpawnCustomerFromPool();
            }
        }
    }

    /// <summary>
    /// Fetches a customer from the pool, places them at spawn point, and guides them into the queue.
    /// </summary>
    private void SpawnCustomerFromPool()
    {
        GameObject npc = GetDeactivePoolCustomer();


        // Set spawn position
        npc.transform.position = spawnPoint.position;
        npc.transform.rotation = Quaternion.identity;
        npc.SetActive(true);

        CustomerController controller = npc.GetComponent<CustomerController>();

        controller.ResetCustomer();

        // Add to queue and get their target queue index
        activeQueue.Add(controller);
        int targetIndex = activeQueue.Count - 1;

        // Send them walking to their dynamically calculated queue position
        SendCustomerToQueuePosition(controller, targetIndex);


    }

    private GameObject GetDeactivePoolCustomer()
    {
        foreach (var obj in customerPool)
        {
            if (obj != null && !obj.activeSelf)
            {
                return obj;
            }
        }
        int randIndex = Random.Range(0, customerPrefabs.Count);
        GameObject chosenPrefab = customerPrefabs[randIndex];


        GameObject npc = Instantiate(chosenPrefab);
        npc.SetActive(false);
        npc.transform.SetParent(transform);
        customerPool.Add(npc);
        return npc;


    }

    private Vector3 CalculateQueuePosition(int queueIndex) => firstQueueSpot.position - (firstQueueSpot.forward * distanceBetweenSpots * queueIndex);



    private void SendCustomerToQueuePosition(CustomerController controller, int queueIndex)
    {

        Vector3 targetPos = CalculateQueuePosition(queueIndex);

        // Command the customer to walk to their spot
        controller.WalkTo(targetPos, CustomerController.CustomerState.WalkingToQueue, () =>
        {
            // Arrived callback:
            if (queueIndex == 0)
            {
                controller.WalkTo(transform.position, CustomerController.CustomerState.AtRegister);
                controller.ShowRequestUI();
            }
            else
            {
                controller.WalkTo(transform.position, CustomerController.CustomerState.WaitingInQueue);
                controller.HideRequestUI();
            }
        });
    }
    public void OnCustomerServed(CustomerController servedCustomer)
    {
        if (!activeQueue.Contains(servedCustomer)) return;

        activeQueue.Remove(servedCustomer);

        Vector3 exitPos = exitPoint.position;
        servedCustomer.WalkTo(exitPos, CustomerController.CustomerState.Leaving, () =>
        {
            servedCustomer.transform.SetParent(transform);
        });

        ShiftQueueForward();
    }

    private void ShiftQueueForward()
    {
        for (int i = 0; i < activeQueue.Count; i++)
        {
            CustomerController controller = activeQueue[i];
            if (controller != null)
            {
                // Send them to their new closer position
                SendCustomerToQueuePosition(controller, i);
            }
        }
    }

    private IEnumerator PurchaseTickRoutine()
    {
        while (true)
        {
            yield return new WaitForSeconds(purchaseCheckInterval);


            if (activeQueue.Count == 0) continue;

            CustomerController activeController = activeQueue[0];

            if (activeController == null || activeController.GetCurrentState() != CustomerController.CustomerState.AtRegister)
            {
                continue;
            }

            Customer currentCustomer = activeController.GetCustomerData();

            // Iterate over ALL fruits in the multi-fruit order
            var orderSnapshot = new Dictionary<FruitData, int>(currentCustomer.GetOrder());
            foreach (var entry in orderSnapshot)
            {
                FruitData requestedFruit = entry.Key;
                int remaining = entry.Value;
                if (remaining <= 0) continue;

                // Check if the stand inventory has this fruit in stock
                var standItem = standInventory.StoredItems.Find(item => item.fruit == requestedFruit);
                if (standItem != null && standItem.amount > 0)
                {
                    bool removed = standInventory.RemoveFruit(requestedFruit, 1);
                    if (removed)
                    {
                        currentCustomer.DeliverFruit(requestedFruit, 1);

                        // Spawn visual flying fruit from Stand to Customer
                        if (requestedFruit.FruitPrefab != null)
                        {
                            Vector3 spawnPos = standInventory.transform.position + Vector3.up * 1f;
                            GameObject visualObj = Instantiate(requestedFruit.FruitPrefab, spawnPos, Quaternion.identity);
                            if (visualObj != null)
                            {
                                StartCoroutine(AnimateFruitToCustomerVisual(visualObj, currentCustomer));
                            }
                        }
                    }
                }
            }
        }
    }

    /// <summary>
    /// Animates a visual fruit flying in a parabolic trajectory towards the customer's chest.
    /// </summary>
    private IEnumerator AnimateFruitToCustomerVisual(GameObject fruitObj, Customer customer)
    {
        Vector3 startPos = fruitObj.transform.position;
        Quaternion startRot = Random.rotation;
        Vector3 startScale = fruitObj.transform.localScale;

        float flightDuration = 0.5f;
        float arcHeight = 2.0f;
        float elapsed = 0f;


        while (elapsed < flightDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / flightDuration;

            // Target position: customer chest (approx 1 unit up from customer origin)
            Vector3 targetPos = customer.transform.position + Vector3.up * 1f;

            // Parabolic movement
            Vector3 currentPos = Vector3.Lerp(startPos, targetPos, t);
            currentPos.y += Mathf.Sin(t * Mathf.PI) * arcHeight;

            fruitObj.transform.position = currentPos;

            // Spin fruit
            fruitObj.transform.rotation = startRot * Quaternion.Euler(t * 360f, t * 720f, 0f);

            // Scale down slightly as it approaches customer
            fruitObj.transform.localScale = Vector3.Lerp(startScale, startScale * 0.4f, t);

            yield return null;
        }

        if (fruitObj != null)
        {
            Destroy(fruitObj);
        }
    }
}
