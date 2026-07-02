using System.Collections;
using UnityEngine;

public class StandPlaceZone : MonoBehaviour
{
    [SerializeField] private StandInventory standInventory;
    [SerializeField] private float placeInterval = 0.15f;
    [SerializeField] private float arcHeight = 2.5f;
    [SerializeField] private float flightDuration = 0.5f;

    private PlayerInventory activeInventory;
    private Coroutine placeCoroutine;
    private int reservedPlaceCount = 0;

    public bool IsPlayerPlacing
    {
        get
        {
            if (reservedPlaceCount > 0) return true;

            if (activeInventory != null && activeInventory.CurrentCarryCount > 0)
            {
                if (standInventory != null && standInventory.CurrentCount < standInventory.MaxCapacity)
                {
                    return true;
                }
            }

            return false;
        }
    }

    private void Start()
    {
        if (standInventory == null)
            standInventory = GetComponentInParent<StandInventory>();

        if (standInventory != null)
            standInventory.PlaceZone = this;
    }

    private void OnTriggerEnter(Collider other)
    {
        PlayerInventory inventory = other.GetComponentInParent<PlayerInventory>();
        if (inventory == null) return;

        activeInventory = inventory;
        if (placeCoroutine != null) StopCoroutine(placeCoroutine);
        placeCoroutine = StartCoroutine(PlaceRoutine());
    }

    private void OnTriggerExit(Collider other)
    {
        PlayerInventory inventory = other.GetComponentInParent<PlayerInventory>();
        if (inventory == null || activeInventory != inventory) return;

        activeInventory = null;
        if (placeCoroutine != null)
        {
            StopCoroutine(placeCoroutine);
            placeCoroutine = null;
        }
    }

    private IEnumerator PlaceRoutine()
    {
        while (activeInventory != null)
        {
            int currentSpaceAvailable = standInventory.MaxCapacity - standInventory.CurrentCount - reservedPlaceCount;

            if (activeInventory.CarriedItems.Count > 0 && currentSpaceAvailable > 0)
            {
                FruitData fruitToPlace = activeInventory.CarriedItems[0].fruit;

                if (activeInventory.RemoveFruit(fruitToPlace, 1))
                {
                    reservedPlaceCount++;

                    AudioManager.Instance?.PlaySFX(AudioManager.Instance.itemPickUp);

                    Vector3 spawnPos = activeInventory.transform.position + Vector3.up * 1f;
                    GameObject flyingFruit = null;

                    if (fruitToPlace.FruitPrefab != null)
                        flyingFruit = Instantiate(fruitToPlace.FruitPrefab, spawnPos, Quaternion.identity);

                    if (flyingFruit != null)
                        StartCoroutine(AnimateFruitToStand(flyingFruit, fruitToPlace));
                    else
                    {
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
        float elapsed = 0f;

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

            Vector3 targetPos = standInventory.transform.position + Vector3.up * 1f;
            Vector3 currentPos = Vector3.Lerp(startPos, targetPos, t);
            currentPos.y += Mathf.Sin(t * Mathf.PI) * arcHeight;

            fruitObj.transform.position = currentPos;
            fruitObj.transform.rotation = Quaternion.Slerp(startRot, standInventory.transform.rotation, t)
                * Quaternion.Euler(t * 360f, t * 360f, 0f);

            yield return null;
        }

        if (fruitObj != null) Destroy(fruitObj);

        if (standInventory != null)
            standInventory.AddFruit(fruitData, 1);

        reservedPlaceCount = Mathf.Max(0, reservedPlaceCount - 1);
    }
}
