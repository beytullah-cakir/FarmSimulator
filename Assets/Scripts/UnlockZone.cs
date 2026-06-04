using System.Collections;
using UnityEngine;
using DG.Tweening;
using Unity.Cinemachine;

public class UnlockZone : MonoBehaviour, ISaveable
{
    [SerializeField] private string saveID = "unlock_zone_default";
    [SerializeField] private int requiredMoney = 500;
    [SerializeField] private GameObject objectToDeactivate;
    [SerializeField] private GameObject objectToActivate;
    [SerializeField] private FruitData fruitToUnlock;
    [SerializeField] private GameObject cutsceneCameraObject;
    [SerializeField] private float cameraBlendDuration;
    [SerializeField] private float transferInterval = 0.05f;
    [SerializeField] private int transferAmountPerTick = 5;
    [SerializeField] private Canvas overheadCanvas;
    [SerializeField] private GameObject moneyPrefab;
    [SerializeField] private float moneyArcHeight = 3f;
    [SerializeField] private float growAndShrinkDuration = 1f;

    public string SaveID => saveID;

    private int currentInvestedMoney = 0;
    private float moneyFlightDuration = 0.4f;
    private Coroutine transferCoroutine;
    private PlayerController activePlayer;
    private bool isPlayerInside = false;
    private bool isLoadedAsUnlocked = false;

    private void Awake()
    {
        if (overheadCanvas == null)
            overheadCanvas = GetComponentInChildren<Canvas>(true);
    }

    private void Start()
    {
        if (isLoadedAsUnlocked)
        {
            InstantUnlock();
            return;
        }
        UpdateCostUI();
    }

    private void OnTriggerEnter(Collider other)
    {
        PlayerController player = other.GetComponent<PlayerController>();
        activePlayer = player;
        isPlayerInside = true;
        UpdateCostUI();
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
            int transferAmount = Mathf.Min(transferAmountPerTick, remainingCost, GameManager.Instance.PlayerMoney);

            if (transferAmount > 0)
            {
                GameManager.Instance.RemoveMoney(transferAmount);
                currentInvestedMoney += transferAmount;
                UpdateCostUI();

                AudioManager.Instance?.PlaySFX(AudioManager.Instance.transformCoin);

                if (moneyPrefab != null && activePlayer != null)
                    StartCoroutine(SpawnMoneyProjectile(activePlayer.transform.position));
            }

            if (currentInvestedMoney >= requiredMoney)
            {
                UnlockArea();
                yield break;
            }

            yield return new WaitForSeconds(transferInterval);
        }
    }

    private IEnumerator SpawnMoneyProjectile(Vector3 spawnWorldPos)
    {
        GameObject moneyObj = Instantiate(moneyPrefab, spawnWorldPos + Vector3.up * 1f, Quaternion.identity);

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

            Vector3 pos = Vector3.Lerp(startPos, targetPos, t);
            pos.y += Mathf.Sin(t * Mathf.PI) * moneyArcHeight;

            moneyObj.transform.position = pos;
            moneyObj.transform.rotation = Quaternion.Euler(0f, t * 360f, t * 180f);

            yield return null;
        }

        if (moneyObj != null) Destroy(moneyObj);
    }

    private void UpdateCostUI()
    {
        int remainingCost = Mathf.Max(0, requiredMoney - currentInvestedMoney);
        float fillAmount = (float)currentInvestedMoney / requiredMoney;

        if (UIManager.Instance != null)
            UIManager.Instance.UpdateUnlockUI(fruitToUnlock, remainingCost, fillAmount);
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

        if (transferCoroutine != null) StopCoroutine(transferCoroutine);
        transferCoroutine = null;

        AudioManager.Instance?.PlaySFX(AudioManager.Instance.unlockArea);
    }

    private IEnumerator AnimateUnlockTransition()
    {
        yield return new WaitForSeconds(cameraBlendDuration);

        foreach (Transform child in objectToDeactivate.transform)
            child.DOScale(Vector3.zero, growAndShrinkDuration).SetEase(Ease.OutCubic);

        yield return new WaitForSeconds(growAndShrinkDuration);
        Destroy(objectToDeactivate);

        objectToActivate.SetActive(true);

        foreach (Transform child in objectToActivate.transform)
            child.DOScale(Vector3.one, growAndShrinkDuration).SetEase(Ease.OutCubic);

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

    #region ISaveable

    public object CaptureState()
    {
        return new UnlockZoneSaveData
        {
            saveID = this.saveID,
            currentInvestedMoney = this.currentInvestedMoney,
            isUnlocked = (currentInvestedMoney >= requiredMoney)
        };
    }

    public void RestoreState(object state)
    {
        if (state is not UnlockZoneSaveData data) return;

        currentInvestedMoney = data.currentInvestedMoney;

        if (data.isUnlocked || currentInvestedMoney >= requiredMoney)
            isLoadedAsUnlocked = true;
        else
            UpdateCostUI();
    }

    private void InstantUnlock()
    {
        if (overheadCanvas != null) overheadCanvas.enabled = false;
        if (fruitToUnlock != null) GameManager.Instance.SetFruitActive(fruitToUnlock, true);
        if (objectToDeactivate != null) Destroy(objectToDeactivate);
        if (objectToActivate != null) objectToActivate.SetActive(true);
        Destroy(gameObject);
    }

    #endregion
}
