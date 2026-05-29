using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CustomerQueueManager : MonoBehaviour
{
    public static CustomerQueueManager Instance { get; private set; }

    [Header("Spawner & Object Pool")]
    [Tooltip("List of different Customer Prefabs. The script will pick randomly from this list to create visual variety in the queue.")]
    [SerializeField] private List<GameObject> customerPrefabs = new List<GameObject>();

    [Tooltip("Initial number of customer instances to pre-spawn in the pool.")]
    [SerializeField] private int initialPoolSize = 6;

    [Tooltip("Point in the shop where new customers appear/spawn.")]
    [SerializeField] private Transform spawnPoint;

    [Tooltip("Point in the shop where customers walk to leave before deactivating.")]
    [SerializeField] private Transform exitPoint;

    [Tooltip("Minimum time (in seconds) between customer arrivals.")]
    [SerializeField] private float minSpawnTime = 6f;

    [Tooltip("Maximum time (in seconds) between customer arrivals.")]
    [SerializeField] private float maxSpawnTime = 12f;

    [Header("Queue Dynamic Setup")]
    [Tooltip("The single point directly in front of the stand/register (1st spot in line).")]
    [SerializeField] private Transform firstQueueSpot;

    [Tooltip("Maximum number of customers allowed to wait in the queue at once.")]
    [SerializeField] private int maxQueueCapacity = 5;

    [Tooltip("The physical distance between each waiting customer in the line.")]
    [SerializeField] private float distanceBetweenSpots = 1.3f;

    [Header("Stand Inventory Integration")]
    [Tooltip("Reference to the StandInventory component of this stand. Müşteri buradaki envanterden meyve satın alacak.")]
    [SerializeField] private StandInventory standInventory;

    [Tooltip("How fast the customer at the register purchases items from the stand (interval in seconds).")]
    [SerializeField] private float purchaseCheckInterval = 0.2f;

    // State lists
    private List<GameObject> customerPool = new List<GameObject>();
    private List<CustomerController> activeQueue = new List<CustomerController>();

    private void Awake()
    {
        // Singleton initialization
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }

        // Try to automatically find StandInventory if not assigned
        if (standInventory == null)
        {
            standInventory = GetComponentInParent<StandInventory>();
            if (standInventory == null)
            {
                standInventory = GetComponentInChildren<StandInventory>();
            }
        }
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

    /// <summary>
    /// Populates the pool with deactive customer objects to optimize runtime memory.
    /// </summary>
    private void InitializeObjectPool()
    {
        if (customerPrefabs == null || customerPrefabs.Count == 0)
        {
            Debug.LogError("[QueueManager] Customer Prefabs list is empty! Please assign at least one prefab in the Inspector.");
            return;
        }

        for (int i = 0; i < initialPoolSize; i++)
        {
            // Pick a random customer prefab from the list to create variety
            int randIndex = Random.Range(0, customerPrefabs.Count);
            GameObject chosenPrefab = customerPrefabs[randIndex];

            if (chosenPrefab != null)
            {
                GameObject npc = Instantiate(chosenPrefab);
                npc.name = $"{chosenPrefab.name}_Pool_{i}";
                npc.SetActive(false);
                
                // Parent to this manager to keep Hierarchy clean
                npc.transform.SetParent(transform);
                
                customerPool.Add(npc);
            }
        }
    }

    /// <summary>
    /// Spawns customers at random intervals from the pool if queue space is available.
    /// </summary>
    private IEnumerator SpawnCustomerRoutine()
    {
        // Let the game load before spawning first customer
        yield return new WaitForSeconds(2f);

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

        if (npc != null)
        {
            // Set spawn position
            npc.transform.position = spawnPoint != null ? spawnPoint.position : transform.position;
            npc.transform.rotation = Quaternion.identity;
            npc.SetActive(true);

            CustomerController controller = npc.GetComponent<CustomerController>();
            if (controller != null)
            {
                // Reset customer state and generate new random fruit order
                controller.ResetCustomer();

                // Add to queue and get their target queue index
                activeQueue.Add(controller);
                int targetIndex = activeQueue.Count - 1;

                // Send them walking to their dynamically calculated queue position
                SendCustomerToQueuePosition(controller, targetIndex);
            }
        }
    }

    /// <summary>
    /// Looks for a disabled customer instance in the pool. If none exist, spawns a new one.
    /// </summary>
    private GameObject GetDeactivePoolCustomer()
    {
        foreach (var obj in customerPool)
        {
            if (obj != null && !obj.activeSelf)
            {
                return obj;
            }
        }

        // Dynamically expand pool if empty (fallback)
        if (customerPrefabs != null && customerPrefabs.Count > 0)
        {
            int randIndex = Random.Range(0, customerPrefabs.Count);
            GameObject chosenPrefab = customerPrefabs[randIndex];

            if (chosenPrefab != null)
            {
                GameObject npc = Instantiate(chosenPrefab);
                npc.name = $"{chosenPrefab.name}_Pool_Expanded_{customerPool.Count}";
                npc.SetActive(false);
                npc.transform.SetParent(transform);
                customerPool.Add(npc);
                return npc;
            }
        }

        return null;
    }

    /// <summary>
    /// Dynamically calculates the position of a queue spot relative to the firstQueueSpot's position and rotation.
    /// The queue grows backwards from the direction the first spot is facing.
    /// </summary>
    private Vector3 CalculateQueuePosition(int queueIndex)
    {
        if (firstQueueSpot == null)
        {
            return transform.position;
        }

        // Subtract the forward vector of the first spot so that subsequent customers line up behind it
        return firstQueueSpot.position - (firstQueueSpot.forward * distanceBetweenSpots * queueIndex);
    }

    /// <summary>
    /// Navigates the customer to their dynamically calculated queue position.
    /// </summary>
    private void SendCustomerToQueuePosition(CustomerController controller, int queueIndex)
    {
        if (firstQueueSpot == null) return;

        Vector3 targetPos = CalculateQueuePosition(queueIndex);

        // Command the customer to walk to their spot
        controller.WalkTo(targetPos, CustomerController.CustomerState.WalkingToQueue, () =>
        {
            // Arrived callback:
            if (queueIndex == 0)
            {
                controller.WalkTo(transform.position, CustomerController.CustomerState.AtRegister);
                controller.ShowRequestUI();
                Debug.Log($"[QueueManager] Müşteri {controller.name} kasaya ulaştı ve siparişi gösteriyor.");
            }
            else
            {
                controller.WalkTo(transform.position, CustomerController.CustomerState.WaitingInQueue);
                controller.HideRequestUI();
            }
        });
    }

    /// <summary>
    /// Call this when a customer's order has been fully satisfied.
    /// </summary>
    /// <param name="servedCustomer">The CustomerController that finished buying.</param>
    public void OnCustomerServed(CustomerController servedCustomer)
    {
        if (!activeQueue.Contains(servedCustomer)) return;

        // 1. Remove from active queue
        activeQueue.Remove(servedCustomer);

        // 2. Guide the served customer to walk towards the Exit Point
        Vector3 exitPos = exitPoint != null ? exitPoint.position : transform.position - Vector3.forward * 10f;
        servedCustomer.WalkTo(exitPos, CustomerController.CustomerState.Leaving, () =>
        {
            // Reset to pool parent upon arriving at the exit
            servedCustomer.transform.SetParent(transform);
        });

        // 3. Shift queue forward: update destinations for all remaining customers in line
        ShiftQueueForward();
    }

    /// <summary>
    /// Shifts all waiting customers in line forward by one spot and triggers next order.
    /// </summary>
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

    /// <summary>
    /// Repeatedly checks if the customer at the register can buy a fruit from the stand's inventory.
    /// </summary>
    private IEnumerator PurchaseTickRoutine()
    {
        while (true)
        {
            yield return new WaitForSeconds(purchaseCheckInterval);

            // Check if there is an active customer at the register
            if (standInventory != null && activeQueue.Count > 0 && activeQueue[0] != null)
            {
                CustomerController activeController = activeQueue[0];
                if (activeController.GetCurrentState() == CustomerController.CustomerState.AtRegister)
                {
                    Customer currentCustomer = activeController.GetCustomerData();
                    if (currentCustomer != null && !currentCustomer.IsOrderSatisfied)
                    {
                        FruitData requestedFruit = currentCustomer.RequestedFruit;

                        // Check if the stand inventory has the requested fruit in stock
                        var standItem = standInventory.StoredItems.Find(item => item.fruit == requestedFruit);
                        if (standItem != null && standItem.amount > 0)
                        {
                            // Deduct 1 fruit from the stand's inventory
                            bool removed = standInventory.RemoveFruit(requestedFruit, 1);
                            if (removed)
                            {
                                // Deliver it to the customer logically (updates their remaining count and UI immediately)
                                currentCustomer.DeliverFruit(requestedFruit, 1);

                                // SPAWN VISUAL FLYING FRUIT (Parabolic arc from Stand to Customer)
                                Vector3 spawnPos = standInventory.transform.position + Vector3.up * 1f;
                                if (requestedFruit.FruitPrefab != null)
                                {
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

        // Disable collider and physics during flight
        Collider col = fruitObj.GetComponent<Collider>();
        if (col != null) col.enabled = false;
        Rigidbody rb = fruitObj.GetComponent<Rigidbody>();
        if (rb != null) rb.isKinematic = true;

        while (elapsed < flightDuration)
        {
            if (fruitObj == null) yield break;

            if (customer == null)
            {
                Destroy(fruitObj);
                yield break;
            }

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

    // Draw helper lines in Unity Editor to visualize the queue spacing and direction!
    private void OnDrawGizmosSelected()
    {
        if (firstQueueSpot != null)
        {
            Gizmos.color = Color.cyan;
            for (int i = 0; i < maxQueueCapacity; i++)
            {
                Vector3 spotPos = CalculateQueuePosition(i);
                Gizmos.DrawWireSphere(spotPos, 0.4f);
                if (i > 0)
                {
                    Vector3 prevSpot = CalculateQueuePosition(i - 1);
                    Gizmos.DrawLine(prevSpot, spotPos);
                }
            }
        }
    }
}
