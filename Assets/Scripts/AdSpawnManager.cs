using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AdSpawnManager : MonoBehaviour
{
    public static AdSpawnManager Instance { get; private set; }

    [Header("Spawn Timings")]
    [SerializeField] private float minInitialDelay = 5f;
    [SerializeField] private float maxInitialDelay = 10f;
    [SerializeField] private float minRespawnTime  = 15f;
    [SerializeField] private float maxRespawnTime  = 30f;

    [Header("References")]
    public List<GameObject> spawnPoints = new();
    public List<GameObject> adPrefabs   = new();

    private GameObject currentSpawnedAd;
    private Coroutine  spawnRoutine;

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else if (Instance != this)
            Destroy(gameObject);
    }

    private void Start()
    {
        float initialDelay = Random.Range(minInitialDelay, maxInitialDelay);
        spawnRoutine = StartCoroutine(SpawnSequenceRoutine(initialDelay));
    }

    private IEnumerator SpawnSequenceRoutine(float delay)
    {
        yield return new WaitForSeconds(delay);

        SpawnRandomAd();

        // Reklam izlenip yok edilene kadar bekle
        while (currentSpawnedAd != null)
        {
            yield return null;
        }

        // Reklam izlendi/silindi -> Rastgele sure sonra yeniden dogur
        float respawnDelay = Random.Range(minRespawnTime, maxRespawnTime);
        Debug.Log($"[AdSpawnManager] Reklam izlendi/silindi. Yeni reklam {respawnDelay:F1}s sonra dogacak.");

        spawnRoutine = StartCoroutine(SpawnSequenceRoutine(respawnDelay));
    }

    public GameObject SpawnRandomAd()
    {
        if (adPrefabs == null || adPrefabs.Count == 0)
        {
            Debug.LogWarning("[AdSpawnManager] adPrefabs listesi bos!");
            return null;
        }

        if (spawnPoints == null || spawnPoints.Count == 0)
        {
            Debug.LogWarning("[AdSpawnManager] spawnPoints listesi bos!");
            return null;
        }

        GameObject spawnPointObj = RandomSpawnPoint();
        GameObject adPrefab = RandomAdPrefab();

        if (adPrefab == null || spawnPointObj == null) return null;

        Vector3 spawnPosition = spawnPointObj.transform.position;
        Quaternion spawnRotation = spawnPointObj.transform.rotation;

        currentSpawnedAd = Instantiate(adPrefab, spawnPosition, spawnRotation);
        Debug.Log($"[AdSpawnManager] Yeni reklam prefab'i olusturuldu: {adPrefab.name} ({spawnPointObj.name} konumunda)");

        return currentSpawnedAd;
    }

    public GameObject RandomSpawnPoint()
    {
        if (spawnPoints == null || spawnPoints.Count == 0) return null;
        return spawnPoints[Random.Range(0, spawnPoints.Count)];
    }

    public GameObject RandomAdPrefab()
    {
        if (adPrefabs == null || adPrefabs.Count == 0) return null;
        return adPrefabs[Random.Range(0, adPrefabs.Count)];
    }

    /// <summary>
    /// Reklam izlendiginde veya manuel silindiginde tetiklemek icin opsiyonel metod.
    /// </summary>
    public void NotifyAdWatched()
    {
        if (currentSpawnedAd != null)
        {
            Destroy(currentSpawnedAd);
            currentSpawnedAd = null;
        }
    }
}
