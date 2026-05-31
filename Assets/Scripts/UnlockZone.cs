using System.Collections;
using System.Collections.Generic;
using UnityEngine;
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

    [SerializeField] private TextMeshProUGUI costText;

    [SerializeField] private Canvas overheadCanvas;

    private Coroutine transferCoroutine;
    private PlayerController activePlayer;

    private bool isPlayerInside = false;

    private void Awake()
    {
        costText = GetComponentInChildren<TextMeshProUGUI>(true);
        overheadCanvas = GetComponentInChildren<Canvas>(true);
        UpdateCostUI();
    }


    private void OnTriggerEnter(Collider other)
    {

        PlayerController player = other.GetComponent<PlayerController>();
        activePlayer = player;
        isPlayerInside = true;
        transferCoroutine = StartCoroutine(TransferMoneyRoutine());
    }

    private void OnTriggerExit(Collider other)
    {

        activePlayer = null;
        isPlayerInside = false;
        StopCoroutine(transferCoroutine);
        transferCoroutine = null;
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
            }

            if (currentInvestedMoney >= requiredMoney)
            {
                UnlockArea();
                yield break;
            }

            yield return new WaitForSeconds(transferInterval);
        }
    }

    private void UpdateCostUI()
    {
        int remainingCost = Mathf.Max(0, requiredMoney - currentInvestedMoney);
        costText.text = remainingCost.ToString();
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
