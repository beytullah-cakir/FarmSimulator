using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using DG.Tweening;

public class UnlockZone : MonoBehaviour
{
    [Header("Unlock Cost Settings")]
    [SerializeField] private int requiredMoney = 500;

    [SerializeField] private int currentInvestedMoney = 0;

    [Header("Activation / Deactivation Triggers")]
    [SerializeField] private GameObject objectToDeactivate;

    [SerializeField] private GameObject objectToActivate;

    [SerializeField] private FruitData fruitToUnlock;

    [Header("Cinemachine Settings")]
    [SerializeField] private GameObject cutsceneCameraObject;

    public float cameraBlendDuration = 1.5f;

    [Header("Transfer Settings")]
    [SerializeField] private float transferInterval = 0.05f;

    [SerializeField] private int transferAmountPerTick = 5;

    [Header("UI Reference")]
    [SerializeField] private TextMeshProUGUI costText;

    [SerializeField] private Canvas overheadCanvas;

    [Header("Visual Coin Effect (Optional)")]
    [SerializeField] private GameObject coinPrefab;

    private Coroutine transferCoroutine;
    private PlayerController activePlayer;
    private bool isUnlocked = false;

    private void Awake()
    {

        if (costText == null) costText = GetComponentInChildren<TextMeshProUGUI>(true);
        if (overheadCanvas == null) overheadCanvas = GetComponentInChildren<Canvas>(true);
    }

    private void Start()
    {

        if (currentInvestedMoney >= requiredMoney)
        {
            UnlockArea(true);
        }
        else
        {
            UpdateCostUI();
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (isUnlocked) return;

        PlayerController player = other.GetComponent<PlayerController>();
        if (player != null)
        {
            activePlayer = player;
            

            if (transferCoroutine != null)
            {
                StopCoroutine(transferCoroutine);
            }
            transferCoroutine = StartCoroutine(TransferMoneyRoutine());
        }
    }

    private void OnTriggerExit(Collider other)
    {
        PlayerController player = other.GetComponent<PlayerController>();
        if (player != null && activePlayer == player)
        {
            activePlayer = null;
            if (transferCoroutine != null)
            {
                StopCoroutine(transferCoroutine);
                transferCoroutine = null;
            }
        }
    }

    private IEnumerator TransferMoneyRoutine()
    {
        while (activePlayer != null && currentInvestedMoney < requiredMoney)
        {
            if (GameManager.Instance != null)
            {
                int remainingCost = requiredMoney - currentInvestedMoney;
                

                int playerWallet = GameManager.Instance.PlayerMoney;
                int transferAmount = Mathf.Min(transferAmountPerTick, remainingCost, playerWallet);

                if (transferAmount > 0)
                {

                    bool success = GameManager.Instance.RemoveMoney(transferAmount);
                    if (success)
                    {
                        currentInvestedMoney += transferAmount;
                        UpdateCostUI();

                        if (coinPrefab != null)
                        {
                            Vector3 spawnPos = activePlayer.transform.position + Vector3.up * 1f;
                            GameObject flyingCoin = Instantiate(coinPrefab, spawnPos, Quaternion.identity);
                            if (flyingCoin != null)
                            {
                                StartCoroutine(AnimateCoinFly(flyingCoin));
                            }
                        }
                    }
                }
            }

            if (currentInvestedMoney >= requiredMoney)
            {
                UnlockArea(false);
                yield break;
            }

            yield return new WaitForSeconds(transferInterval);
        }
    }

    private void UpdateCostUI()
    {
        if (costText != null)
        {
            int remainingCost = Mathf.Max(0, requiredMoney - currentInvestedMoney);
            costText.text = remainingCost.ToString();
        }
    }

    private void UnlockArea(bool instant)
    {
        isUnlocked = true;

        if (objectToDeactivate != null && transform.IsChildOf(objectToDeactivate.transform))
        {
            transform.SetParent(objectToDeactivate.transform.parent);
        }

        if (overheadCanvas != null)
        {
            overheadCanvas.enabled = false;
        }
        if (costText != null)
        {
            costText.enabled = false;
        }

        if (fruitToUnlock != null && GameManager.Instance != null)
        {
            GameManager.Instance.SetFruitActive(fruitToUnlock, true);
        }

        if (instant)
        {

            if (objectToDeactivate != null)
            {
                Destroy(objectToDeactivate);
            }
            if (objectToActivate != null)
            {
                objectToActivate.SetActive(true);
            }
            

            Destroy(gameObject);
        }
        else
        {

            Transform targetFocus = null;
            if (objectToActivate != null)
            {
                if (objectToActivate.transform.childCount > 0)
                {
                    targetFocus = objectToActivate.transform.GetChild(0);
                }
                else
                {
                    targetFocus = objectToActivate.transform;
                }
            }

            if (targetFocus != null)
            {
                if (cutsceneCameraObject != null)
                {
                    SetCameraTarget(cutsceneCameraObject, targetFocus);
                }
                else
                {

                    SetCinemachineTarget(targetFocus);
                }
            }

            if (activePlayer != null)
            {
                activePlayer.SetInputActive(false);
            }

            if (cutsceneCameraObject != null)
            {

                cutsceneCameraObject.SetActive(true);
            }

            StartCoroutine(AnimateUnlockTransition());
        }

        if (transferCoroutine != null)
        {
            StopCoroutine(transferCoroutine);
            transferCoroutine = null;
        }

        Collider col = GetComponent<Collider>();
        if (col != null)
        {
            col.enabled = false;
        }
    }

    private IEnumerator AnimateUnlockTransition()
    {
        float shrinkDuration = 0.5f;
        float growDuration = 0.5f;

        yield return new WaitForSeconds(cameraBlendDuration);

        if (objectToDeactivate != null)
        {

            List<Transform> shrinkChildren = new List<Transform>();
            if (objectToDeactivate.transform.childCount > 0)
            {
                foreach (Transform child in objectToDeactivate.transform)
                {
                    shrinkChildren.Add(child);
                }
            }
            else
            {
                shrinkChildren.Add(objectToDeactivate.transform);
            }

            foreach (var child in shrinkChildren)
            {
                if (child != null)
                {

                    child.DOScale(Vector3.zero, shrinkDuration).SetEase(Ease.OutCubic);
                }
            }

            yield return new WaitForSeconds(shrinkDuration);

            Destroy(objectToDeactivate);
        }

        if (objectToActivate != null)
        {

            List<Transform> growChildren = new List<Transform>();
            if (objectToActivate.transform.childCount > 0)
            {
                foreach (Transform child in objectToActivate.transform)
                {
                    growChildren.Add(child);
                }
            }
            else
            {
                growChildren.Add(objectToActivate.transform);
            }

            List<Vector3> targetScales = new List<Vector3>();
            foreach (var child in growChildren)
            {
                targetScales.Add(child.localScale);
                child.localScale = Vector3.zero;
            }

            objectToActivate.SetActive(true);

            for (int i = 0; i < growChildren.Count; i++)
            {
                Transform child = growChildren[i];
                if (child != null)
                {

                    child.DOScale(targetScales[i], growDuration).SetEase(Ease.OutCubic);
                }
            }

            yield return new WaitForSeconds(growDuration);
        }

        if (cutsceneCameraObject != null)
        {

            cutsceneCameraObject.SetActive(false);
        }
        else if (activePlayer != null)
        {

            SetCinemachineTarget(activePlayer.transform);
        }

        yield return new WaitForSeconds(cameraBlendDuration);

        if (activePlayer != null)
        {

            activePlayer.SetInputActive(true);
        }

        Destroy(gameObject);
    }

    private IEnumerator AnimateCoinFly(GameObject coinObj)
    {
        Vector3 startPos = coinObj.transform.position;
        Quaternion startRot = Random.rotation;
        Vector3 startScale = coinObj.transform.localScale;

        float duration = 0.5f;
        float arcHeight = 2.0f;
        float elapsed = 0f;

        Collider col = coinObj.GetComponent<Collider>();
        if (col != null) col.enabled = false;
        Rigidbody rb = coinObj.GetComponent<Rigidbody>();
        if (rb != null) rb.isKinematic = true;

        Vector3 targetPos = transform.position + Vector3.up * 1f;

        while (elapsed < duration)
        {
            if (coinObj == null) yield break;

            elapsed += Time.deltaTime;
            float t = elapsed / duration;

            Vector3 currentPos = Vector3.Lerp(startPos, targetPos, t);
            currentPos.y += Mathf.Sin(t * Mathf.PI) * arcHeight;

            coinObj.transform.position = currentPos;
            

            coinObj.transform.rotation = startRot * Quaternion.Euler(t * 360f, t * 720f, 0f);

            coinObj.transform.localScale = Vector3.Lerp(startScale, startScale * 0.4f, t);

            yield return null;
        }

        if (coinObj != null)
        {
            Destroy(coinObj);
        }
    }

    private void SetCinemachineTarget(Transform target)
    {
        MonoBehaviour[] allComponents = FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None);
        foreach (var comp in allComponents)
        {
            if (comp != null)
            {
                string typeName = comp.GetType().Name;
                if (typeName == "CinemachineVirtualCamera" || typeName == "CinemachineCamera" || typeName.Contains("Cinemachine"))
                {

                    var followProp = comp.GetType().GetProperty("Follow");
                    if (followProp != null) followProp.SetValue(comp, target);
                    else
                    {
                        var followField = comp.GetType().GetField("Follow");
                        if (followField != null) followField.SetValue(comp, target);
                    }

                    var lookAtProp = comp.GetType().GetProperty("LookAt");
                    if (lookAtProp != null) lookAtProp.SetValue(comp, target);
                    else
                    {
                        var lookAtField = comp.GetType().GetField("LookAt");
                        if (lookAtField != null) lookAtField.SetValue(comp, target);
                    }
                }
            }
        }
    }

    private void SetCameraTarget(GameObject cameraObj, Transform target)
    {
        if (cameraObj == null) return;

        MonoBehaviour[] components = cameraObj.GetComponents<MonoBehaviour>();
        foreach (var comp in components)
        {
            if (comp != null)
            {
                string typeName = comp.GetType().Name;
                if (typeName == "CinemachineVirtualCamera" || typeName == "CinemachineCamera" || typeName.Contains("Cinemachine"))
                {

                    var followProp = comp.GetType().GetProperty("Follow");
                    if (followProp != null) followProp.SetValue(comp, target);
                    else
                    {
                        var followField = comp.GetType().GetField("Follow");
                        if (followField != null) followField.SetValue(comp, target);
                    }

                    var lookAtProp = comp.GetType().GetProperty("LookAt");
                    if (lookAtProp != null) lookAtProp.SetValue(comp, target);
                    else
                    {
                        var lookAtField = comp.GetType().GetField("LookAt");
                        if (lookAtField != null) lookAtField.SetValue(comp, target);
                    }
                }
            }
        }
    }

}
