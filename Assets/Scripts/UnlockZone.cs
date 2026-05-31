using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;
using Unity.Cinemachine;

public class UnlockZone : MonoBehaviour
{
    [SerializeField] private int requiredMoney = 500;

    private int currentInvestedMoney = 0;

    [SerializeField] private GameObject objectToDeactivate;

    [SerializeField] private GameObject objectToActivate;

    [SerializeField] private FruitData fruitToUnlock;

    public GameObject cutsceneCameraObject;
    public GameObject mainCinemachineCamera;

    public float cameraBlendDuration;

    [SerializeField] private float transferInterval = 0.05f;

    [SerializeField] private int transferAmountPerTick = 5;

    // (UI references are now managed by UIManager)

    [SerializeField] private Canvas overheadCanvas;

    [Header("Para Animasyonu")]
    [Tooltip("Inspector'dan atanacak para prefab'ı.")]
    [SerializeField] private GameObject moneyPrefab;

    [SerializeField] private float moneyArcHeight = 3f;
    [SerializeField] private float moneyFlightDuration = 0.4f;

    private Coroutine transferCoroutine;
    private PlayerController activePlayer;

    private bool isPlayerInside = false;

    private void Awake()
    {
        if (overheadCanvas == null)
            overheadCanvas = GetComponentInChildren<Canvas>(true);
    }

    private void Start()
    {
        UpdateCostUI();
    }

    private void OnTriggerEnter(Collider other)
    {
        PlayerController player = other.GetComponent<PlayerController>();
        activePlayer = player;
        isPlayerInside = true;
        UpdateCostUI(); // Update UI instantly on entry
        transferCoroutine = StartCoroutine(TransferMoneyRoutine());
    }

    private void OnTriggerExit(Collider other)
    {
        activePlayer = null;
        isPlayerInside = false;
        if (transferCoroutine != null)
        {
            StopCoroutine(transferCoroutine);
            transferCoroutine = null;
        }
    }

    private IEnumerator TransferMoneyRoutine()
    {
        while (isPlayerInside && currentInvestedMoney < requiredMoney)
        {
            int remainingCost = requiredMoney - currentInvestedMoney;
            int playerWallet = GameManager.Instance.PlayerMoney;
            int transferAmount = Mathf.Min(transferAmountPerTick, remainingCost, playerWallet);

            if (transferAmount > 0)
            {
                GameManager.Instance.RemoveMoney(transferAmount);
                currentInvestedMoney += transferAmount;
                UpdateCostUI();

                // Para animasyonunu başlat (fire-and-forget)
                if (moneyPrefab != null && activePlayer != null)
                {
                    StartCoroutine(SpawnMoneyProjectile(activePlayer.transform.position));
                }
            }

            if (currentInvestedMoney >= requiredMoney)
            {
                UnlockArea();
                yield break;
            }

            yield return new WaitForSeconds(transferInterval);
        }
    }

    /// <summary>
    /// Para objesini oyuncudan UnlockZone merkezine parabolik yay ile fırlatır.
    /// </summary>
    private IEnumerator SpawnMoneyProjectile(Vector3 spawnWorldPos)
    {
        GameObject moneyObj = Instantiate(moneyPrefab, spawnWorldPos + Vector3.up * 1f, Quaternion.identity);

        // Fizik/collider'ı devre dışı bırak
        Collider col = moneyObj.GetComponent<Collider>();
        if (col != null) col.enabled = false;

        Rigidbody rb = moneyObj.GetComponent<Rigidbody>();
        if (rb != null) rb.isKinematic = true;

        Vector3 startPos = moneyObj.transform.position;
        Vector3 targetPos = transform.position + Vector3.up * 1f;

        float elapsed = 0f;

        while (elapsed < moneyFlightDuration)
        {
            if (moneyObj == null) yield break;

            elapsed += Time.deltaTime;
            float t = elapsed / moneyFlightDuration;

            // Parabolik hareket: yatay lerp + dikey sin eğrisi
            Vector3 pos = Vector3.Lerp(startPos, targetPos, t);
            pos.y += Mathf.Sin(t * Mathf.PI) * moneyArcHeight;

            moneyObj.transform.position = pos;

            // Hafif rotasyon efekti
            moneyObj.transform.rotation = Quaternion.Euler(0f, t * 360f, t * 180f);

            // Hedefe yaklaştıkça küçül
            float scale = Mathf.Lerp(1f, 0f, Mathf.Pow(t, 2f));
            moneyObj.transform.localScale = Vector3.one * scale;

            yield return null;
        }

        if (moneyObj != null)
            Destroy(moneyObj);
    }

    private void UpdateCostUI()
    {
        int remainingCost = Mathf.Max(0, requiredMoney - currentInvestedMoney);
        float fillAmount = (float)currentInvestedMoney / requiredMoney;

        if (UIManager.Instance != null)
        {
            UIManager.Instance.UpdateUnlockUI(fruitToUnlock, remainingCost, fillAmount);
        }
    }

    private void UnlockArea()
    {
        overheadCanvas.enabled = false;
        GameManager.Instance.SetFruitActive(fruitToUnlock, true);

        Transform targetFocus = objectToActivate.transform.GetChild(0);

        SetCameraTarget(targetFocus);
        activePlayer.SetInputActive(false);
        cutsceneCameraObject.SetActive(true);

        StartCoroutine(AnimateUnlockTransition());

        if (transferCoroutine != null)
        {
            StopCoroutine(transferCoroutine);
        }
        transferCoroutine = null;
    }

    public float growAndShrinkDuration = 1f;
    private IEnumerator AnimateUnlockTransition()
    {
        yield return new WaitForSeconds(cameraBlendDuration);

        // objectToDeactivate altındaki doğrudan çocuk objeleri sıfıra küçültüyoruz
        foreach (Transform child in objectToDeactivate.transform)
        {
            child.DOScale(Vector3.zero, growAndShrinkDuration).SetEase(Ease.OutCubic);
        }

        yield return new WaitForSeconds(growAndShrinkDuration);

        Destroy(objectToDeactivate);

        objectToActivate.SetActive(true);

        foreach (Transform child in objectToActivate.transform)
        {
            child.DOScale(Vector3.one, growAndShrinkDuration).SetEase(Ease.OutCubic);
        }

        yield return new WaitForSeconds(growAndShrinkDuration);

        cutsceneCameraObject.SetActive(false);

        yield return new WaitForSeconds(cameraBlendDuration);
        activePlayer.SetInputActive(true);
        Destroy(gameObject);
    }

    private void SetCameraTarget(Transform target)
    {
        CinemachineCamera comp = cutsceneCameraObject.GetComponent<CinemachineCamera>();
        comp.Follow = target;
        comp.LookAt = target;
    }
}
