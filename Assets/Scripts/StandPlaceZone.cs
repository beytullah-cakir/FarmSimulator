using System.Collections;
using UnityEngine;

public class StandPlaceZone : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private StandInventory standInventory;

    [Header("Placement Settings")]
    [Tooltip("Time interval (in seconds) between transferring each individual item.")]
    [SerializeField] private float placeInterval = 0.15f;
    [Tooltip("Peak height of the flight arc.")]
    [SerializeField] private float arcHeight = 2.5f;
    [Tooltip("Duration of the fruit flight animation.")]
    [SerializeField] private float flightDuration = 0.5f;

    private PlayerInventory activeInventory;
    private Coroutine placeCoroutine;
    private int reservedPlaceCount = 0;

    private void Start()
    {
        if (standInventory == null)
        {
            standInventory = GetComponentInParent<StandInventory>();
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        PlayerInventory inventory = other.GetComponent<PlayerInventory>();
        if (inventory != null)
        {
            activeInventory = inventory;
            if (placeCoroutine != null)
            {
                StopCoroutine(placeCoroutine);
            }
            placeCoroutine = StartCoroutine(PlaceRoutine());
        }
    }

    private void OnTriggerExit(Collider other)
    {
        PlayerInventory inventory = other.GetComponent<PlayerInventory>();
        if (inventory != null && activeInventory == inventory)
        {
            activeInventory = null;
            if (placeCoroutine != null)
            {
                StopCoroutine(placeCoroutine);
                placeCoroutine = null;
            }
        }
    }

    private IEnumerator PlaceRoutine()
    {
        while (activeInventory != null)
        {
            // Check if player has items and stand has capacity (taking reserved mid-air items into account)
            int currentSpaceAvailable = standInventory.MaxCapacity - standInventory.CurrentCount - reservedPlaceCount;
            
            if (activeInventory.CarriedItems.Count > 0 && currentSpaceAvailable > 0)
            {
                // Grab the first type of fruit in the player's inventory
                FruitData fruitToPlace = activeInventory.CarriedItems[0].fruit;

                // Transfer exactly 1 fruit
                bool removed = activeInventory.RemoveFruit(fruitToPlace, 1);
                if (removed)
                {
                    // Track reservation
                    reservedPlaceCount++;

                    // Spawn flying visual fruit at player height
                    Vector3 spawnPos = activeInventory.transform.position + Vector3.up * 1f;
                    GameObject flyingFruit = null;

                    if (fruitToPlace.FruitPrefab != null)
                    {
                        flyingFruit = Instantiate(fruitToPlace.FruitPrefab, spawnPos, Quaternion.identity);
                    }

                    if (flyingFruit != null)
                    {
                        // Start flight animation
                        StartCoroutine(AnimateFruitToStand(flyingFruit, fruitToPlace));
                    }
                    else
                    {
                        // Fallback if visual prefab is missing
                        standInventory.AddFruit(fruitToPlace, 1);
                        reservedPlaceCount = Mathf.Max(0, reservedPlaceCount - 1);
                    }
                }
            }

            yield return new WaitForSeconds(placeInterval);
        }
    }

    private IEnumerator AnimateFruitToStand(GameObject fruitObj, FruitData fruitData)
    {
        Vector3 startPos = fruitObj.transform.position;
        Quaternion startRot = Random.rotation;
        Vector3 startScale = fruitObj.transform.localScale;

        float elapsed = 0f;

        // Disable collider and physics during flight
        Collider col = fruitObj.GetComponent<Collider>();
        if (col != null) col.enabled = false;
        Rigidbody rb = fruitObj.GetComponent<Rigidbody>();
        if (rb != null) rb.isKinematic = true;

        while (elapsed < flightDuration)
        {
            if (fruitObj == null)
            {
                reservedPlaceCount = Mathf.Max(0, reservedPlaceCount - 1);
                yield break;
            }

            elapsed += Time.deltaTime;
            float t = elapsed / flightDuration;

            // Target position is the stand center (1 unit up from stand origin)
            Vector3 targetPos = standInventory.transform.position + Vector3.up * 1f;

            // Parabolic motion: Lerp on horizontal, Sin wave on vertical
            Vector3 currentPos = Vector3.Lerp(startPos, targetPos, t);
            currentPos.y += Mathf.Sin(t * Mathf.PI) * arcHeight;

            fruitObj.transform.position = currentPos;

            // Spin and align rotation
            fruitObj.transform.rotation = Quaternion.Slerp(startRot, standInventory.transform.rotation, t) * Quaternion.Euler(t * 360f, t * 360f, 0f);

            // Scale down slightly as it approaches the stand
            fruitObj.transform.localScale = Vector3.Lerp(startScale, startScale * 0.4f, t);

            yield return null;
        }

        // Arrival
        if (fruitObj != null)
        {
            Destroy(fruitObj);
        }

        // Add logically to the stand inventory
        if (standInventory != null)
        {
            standInventory.AddFruit(fruitData, 1);
            Debug.Log($"[Stand] Meyve standa yerleştirildi! Stand Doluluğu: {standInventory.CurrentCount}/{standInventory.MaxCapacity}");
        }

        reservedPlaceCount = Mathf.Max(0, reservedPlaceCount - 1);
    }
}
