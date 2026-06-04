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

    private void Awake() => Instance = this;

    private void Start()
    {
        InitializeObjectPool();
        StartCoroutine(SpawnCustomerRoutine());
        StartCoroutine(PurchaseTickRoutine());
    }

    private void InitializeObjectPool()
    {
        for (int i = 0; i < initialPoolSize; i++)
        {
            GameObject npc = Instantiate(customerPrefabs[Random.Range(0, customerPrefabs.Count)]);
            npc.SetActive(false);
            npc.transform.SetParent(transform);
            customerPool.Add(npc);
        }
    }

    private IEnumerator SpawnCustomerRoutine()
    {
        while (true)
        {
            yield return new WaitForSeconds(Random.Range(minSpawnTime, maxSpawnTime));

            if (activeQueue.Count < maxQueueCapacity)
                SpawnCustomerFromPool();
        }
    }

    private void SpawnCustomerFromPool()
    {
        GameObject npc = GetDeactivePoolCustomer();

        npc.transform.position = spawnPoint.position;
        npc.transform.rotation = Quaternion.identity;
        npc.SetActive(true);

        CustomerController controller = npc.GetComponent<CustomerController>();
        controller.ResetCustomer();

        activeQueue.Add(controller);
        int targetIndex = activeQueue.Count - 1;
        SendCustomerToQueuePosition(controller, targetIndex);
    }

    private GameObject GetDeactivePoolCustomer()
    {
        foreach (var obj in customerPool)
        {
            if (obj != null && !obj.activeSelf) return obj;
        }

        GameObject npc = Instantiate(customerPrefabs[Random.Range(0, customerPrefabs.Count)]);
        npc.SetActive(false);
        npc.transform.SetParent(transform);
        customerPool.Add(npc);
        return npc;
    }

    private Vector3 CalculateQueuePosition(int queueIndex) =>
        firstQueueSpot.position - (firstQueueSpot.forward * distanceBetweenSpots * queueIndex);

    private void SendCustomerToQueuePosition(CustomerController controller, int queueIndex)
    {
        Vector3 targetPos = CalculateQueuePosition(queueIndex);

        controller.WalkTo(targetPos, CustomerController.CustomerState.WalkingToQueue, () =>
        {
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

        servedCustomer.WalkTo(exitPoint.position, CustomerController.CustomerState.Leaving, () =>
        {
            servedCustomer.transform.SetParent(transform);
        });

        ShiftQueueForward();
    }

    private void ShiftQueueForward()
    {
        for (int i = 0; i < activeQueue.Count; i++)
        {
            if (activeQueue[i] != null)
                SendCustomerToQueuePosition(activeQueue[i], i);
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
                continue;

            Customer currentCustomer = activeController.GetCustomerData();
            var orderSnapshot = new Dictionary<FruitData, int>(currentCustomer.GetOrder());

            foreach (var entry in orderSnapshot)
            {
                FruitData requestedFruit = entry.Key;
                int remaining = entry.Value;
                if (remaining <= 0) continue;

                var standItem = standInventory.StoredItems.Find(item => item.fruit == requestedFruit);
                if (standItem != null && standItem.amount > 0)
                {
                    if (standInventory.RemoveFruit(requestedFruit, 1))
                    {
                        currentCustomer.DeliverFruit(requestedFruit, 1);

                        if (requestedFruit.FruitPrefab != null)
                        {
                            Vector3 spawnPos = standInventory.transform.position + Vector3.up * 1f;
                            GameObject visualObj = Instantiate(requestedFruit.FruitPrefab, spawnPos, Quaternion.identity);
                            if (visualObj != null)
                                StartCoroutine(AnimateFruitToCustomerVisual(visualObj, currentCustomer));
                        }
                    }
                }
            }
        }
    }

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

            Vector3 targetPos = customer.transform.position + Vector3.up * 1f;
            Vector3 currentPos = Vector3.Lerp(startPos, targetPos, t);
            currentPos.y += Mathf.Sin(t * Mathf.PI) * arcHeight;

            fruitObj.transform.position = currentPos;
            fruitObj.transform.rotation = startRot * Quaternion.Euler(t * 360f, t * 720f, 0f);
            fruitObj.transform.localScale = Vector3.Lerp(startScale, startScale * 0.4f, t);

            yield return null;
        }

        if (fruitObj != null) Destroy(fruitObj);
    }
}
