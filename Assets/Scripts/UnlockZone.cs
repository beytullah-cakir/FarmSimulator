using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using DG.Tweening;

public class UnlockZone : MonoBehaviour
{
    [Header("Unlock Cost Settings")]
    [Tooltip("Total money required to unlock this area.")]
    [SerializeField] private int requiredMoney = 500;

    [Tooltip("How much money has already been paid towards unlocking this area.")]
    [SerializeField] private int currentInvestedMoney = 0;

    [Header("Activation / Deactivation Triggers")]
    [Tooltip("The GameObject to DESTROY once unlocked (e.g. a barrier, lock icon, or fence).")]
    [SerializeField] private GameObject objectToDeactivate;

    [Tooltip("The GameObject to ACTIVATE (show) once unlocked (e.g. a new field, stand, or orange tree).")]
    [SerializeField] private GameObject objectToActivate;

    [Tooltip("Optional: The FruitData to automatically UNLOCK/ACTIVATE in GameManager upon unlocking this area (so customers start ordering it).")]
    [SerializeField] private FruitData fruitToUnlock;

    [Header("Cinemachine Settings")]
    [Tooltip("The Cinemachine Camera GameObject used for focusing on the unlocked area. If assigned, priority blending is used.")]
    [SerializeField] private GameObject cutsceneCameraObject;

    [Tooltip("Time it takes for the Cinemachine camera to blend to the target and back.")]
    public float cameraBlendDuration = 1.5f;

    [Header("Transfer Settings")]
    [Tooltip("Time interval (in seconds) between each money transfer tick.")]
    [SerializeField] private float transferInterval = 0.05f;

    [Tooltip("How much money is deducted and transferred per tick.")]
    [SerializeField] private int transferAmountPerTick = 5;

    [Header("UI Reference")]
    [Tooltip("TextMeshProUGUI component that displays the remaining cost. If null, it will search in children.")]
    [SerializeField] private TextMeshProUGUI costText;

    [Tooltip("The overhead Canvas. If null, it will search in children.")]
    [SerializeField] private Canvas overheadCanvas;

    [Header("Visual Coin Effect (Optional)")]
    [Tooltip("Optional coin prefab that will spawn and fly parabolically from the player to the center of the zone.")]
    [SerializeField] private GameObject coinPrefab;


    private Coroutine transferCoroutine;
    private PlayerController activePlayer;
    private Camera mainCamera;
    private bool isUnlocked = false;

    private void Awake()
    {
        // Auto-find references in children if left null
        if (costText == null) costText = GetComponentInChildren<TextMeshProUGUI>(true);
        if (overheadCanvas == null) overheadCanvas = GetComponentInChildren<Canvas>(true);
    }

    private void Start()
    {
        mainCamera = Camera.main;

        // Perform initial check. If it was already unlocked, trigger instantly.
        if (currentInvestedMoney >= requiredMoney)
        {
            UnlockArea(true); // Instant unlock on start
        }
        else
        {
            UpdateCostUI();
        }
    }


    private void OnTriggerEnter(Collider other)
    {
        if (isUnlocked) return;

        // Check if the entering object is the player
        PlayerController player = other.GetComponent<PlayerController>();
        if (player != null)
        {
            activePlayer = player;
            
            // Start the money transfer routine
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

    /// <summary>
    /// Coroutine that continuously transfers money from the player to the unlock zone.
    /// </summary>
    private IEnumerator TransferMoneyRoutine()
    {
        while (activePlayer != null && currentInvestedMoney < requiredMoney)
        {
            if (GameManager.Instance != null)
            {
                int remainingCost = requiredMoney - currentInvestedMoney;
                
                // Calculate how much we can transfer in this tick
                int playerWallet = GameManager.Instance.PlayerMoney;
                int transferAmount = Mathf.Min(transferAmountPerTick, remainingCost, playerWallet);

                if (transferAmount > 0)
                {
                    // Deduct logically from player wallet
                    bool success = GameManager.Instance.RemoveMoney(transferAmount);
                    if (success)
                    {
                        currentInvestedMoney += transferAmount;
                        UpdateCostUI();

                        // Spawn flying visual coin if prefab is assigned
                        if (coinPrefab != null)
                        {
                            Vector3 spawnPos = activePlayer.transform.position + Vector3.up * 1f; // Player chest height
                            GameObject flyingCoin = Instantiate(coinPrefab, spawnPos, Quaternion.identity);
                            if (flyingCoin != null)
                            {
                                StartCoroutine(AnimateCoinFly(flyingCoin));
                            }
                        }
                    }
                }
            }

            // Check if fully funded
            if (currentInvestedMoney >= requiredMoney)
            {
                UnlockArea(false);
                yield break;
            }

            yield return new WaitForSeconds(transferInterval);
        }
    }

    /// <summary>
    /// Updates the TextMeshPro UI to show the remaining cost.
    /// </summary>
    private void UpdateCostUI()
    {
        if (costText != null)
        {
            int remainingCost = Mathf.Max(0, requiredMoney - currentInvestedMoney);
            costText.text = remainingCost.ToString();
        }
    }

    /// <summary>
    /// Executes the unlock triggers: deactivates the barrier, activates the new area, and disables this zone.
    /// </summary>
    /// <param name="instant">If true, skips sound/effects (used for loading unlocked states).</param>
    private void UnlockArea(bool instant)
    {
        isUnlocked = true;

        // Safety: If the script is a child of objectToDeactivate, detach it so it doesn't get destroyed early
        if (objectToDeactivate != null && transform.IsChildOf(objectToDeactivate.transform))
        {
            transform.SetParent(objectToDeactivate.transform.parent);
        }

        // Hide overhead cost canvas by disabling the components instead of deactivating the GameObject (prevents self-deactivation)
        if (overheadCanvas != null)
        {
            overheadCanvas.enabled = false;
        }
        if (costText != null)
        {
            costText.enabled = false;
        }

        // Dynamically unlock this fruit in GameManager so customers can now purchase/request it!
        if (fruitToUnlock != null && GameManager.Instance != null)
        {
            GameManager.Instance.SetFruitActive(fruitToUnlock, true);
        }

        if (instant)
        {
            // Instant unlock (skip animations, e.g. on loading game)
            if (objectToDeactivate != null)
            {
                Destroy(objectToDeactivate);
            }
            if (objectToActivate != null)
            {
                objectToActivate.SetActive(true);
            }
            
            // Instantly destroy this zone since it is already unlocked
            Destroy(gameObject);
        }
        else
        {
            // Focus Cinemachine Camera on the first child of the newly activated area
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
                    // Fallback to single camera system: Change target on all virtual cameras
                    SetCinemachineTarget(targetFocus);
                }
            }

            // Temporarily disable player movement and input
            if (activePlayer != null)
            {
                activePlayer.SetInputActive(false);
            }

            if (cutsceneCameraObject != null)
            {
                // Activate the cutscene camera to trigger the Cinemachine blend
                cutsceneCameraObject.SetActive(true);
            }

            // Juicy animated transition!
            StartCoroutine(AnimateUnlockTransition());
        }

        Debug.Log($"[UnlockZone] Bölge başarıyla açıldı! (Hedef: {(objectToActivate != null ? objectToActivate.name : "Yok")})");

        // Stop transfer coroutine and disable this collider/zone
        if (transferCoroutine != null)
        {
            StopCoroutine(transferCoroutine);
            transferCoroutine = null;
        }

        // Disable trigger collider so player doesn't interact with it again
        Collider col = GetComponent<Collider>();
        if (col != null)
        {
            col.enabled = false;
        }
    }

    /// <summary>
    /// Juicy animation that shrinks the children of the deactivated object to 0, 
    /// then activates the new object and grows its children from 0 to 1 with smooth easing.
    /// </summary>
    private IEnumerator AnimateUnlockTransition()
    {
        float shrinkDuration = 0.5f;
        float growDuration = 0.5f;

        // Wait for the camera to smoothly blend from the player to the target area
        yield return new WaitForSeconds(cameraBlendDuration);

        // --- PHASE 1: SHRINK CHILDREN OF objectToDeactivate ---
        if (objectToDeactivate != null)
        {
            // Gather all child transforms (fail-safe: use parent itself if no children exist)
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

            // Shrink all children smoothly using DOTween
            foreach (var child in shrinkChildren)
            {
                if (child != null)
                {
                    // Scale down to 0 with Ease.OutCubic
                    child.DOScale(Vector3.zero, shrinkDuration).SetEase(Ease.OutCubic);
                }
            }

            // Wait for the DOTween shrink animation to complete
            yield return new WaitForSeconds(shrinkDuration);

            // Destroy the old object completely
            Destroy(objectToDeactivate);
        }

        // --- PHASE 2: GROW CHILDREN OF objectToActivate ---
        if (objectToActivate != null)
        {
            // Gather all child transforms (fail-safe: use parent itself if no children exist)
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

            // Cache original scales and immediately shrink to zero so they start invisible
            List<Vector3> targetScales = new List<Vector3>();
            foreach (var child in growChildren)
            {
                targetScales.Add(child.localScale);
                child.localScale = Vector3.zero;
            }

            // Set the new area to active in the hierarchy
            objectToActivate.SetActive(true);

            // Grow all children smoothly to their original pre-designed scales using DOTween
            for (int i = 0; i < growChildren.Count; i++)
            {
                Transform child = growChildren[i];
                if (child != null)
                {
                    // Scale up to original scale with Ease.OutCubic
                    child.DOScale(targetScales[i], growDuration).SetEase(Ease.OutCubic);
                }
            }

            // Wait for the DOTween grow animation to complete
            yield return new WaitForSeconds(growDuration);
        }

        // --- PHASE 3: RETURN FOCUS TO PLAYER & RESTORE CONTROLS ---
        if (cutsceneCameraObject != null)
        {
            // Deactivate the cutscene camera to trigger the Cinemachine blend back to the player camera
            cutsceneCameraObject.SetActive(false);
        }
        else if (activePlayer != null)
        {
            // Fallback to single camera system: Change target back to player
            SetCinemachineTarget(activePlayer.transform);
        }

        // Wait for the camera to smoothly blend back from the target area to the player
        yield return new WaitForSeconds(cameraBlendDuration);

        if (activePlayer != null)
        {
            // Re-enable player movement and controls
            activePlayer.SetInputActive(true);
        }

        // DESTROY THE UNLOCK ZONE itself now that the active area is fully open and grown!
        Destroy(gameObject);
    }

    /// <summary>
    /// Animates a visual coin flying in a beautiful parabolic arc from player to the center of the unlock zone.
    /// </summary>
    private IEnumerator AnimateCoinFly(GameObject coinObj)
    {
        Vector3 startPos = coinObj.transform.position;
        Quaternion startRot = Random.rotation;
        Vector3 startScale = coinObj.transform.localScale;

        float duration = 0.5f;
        float arcHeight = 2.0f;
        float elapsed = 0f;

        // Disable physics/colliders on the coin
        Collider col = coinObj.GetComponent<Collider>();
        if (col != null) col.enabled = false;
        Rigidbody rb = coinObj.GetComponent<Rigidbody>();
        if (rb != null) rb.isKinematic = true;

        // Target is the center of this unlock zone (1 unit up)
        Vector3 targetPos = transform.position + Vector3.up * 1f;

        while (elapsed < duration)
        {
            if (coinObj == null) yield break;

            elapsed += Time.deltaTime;
            float t = elapsed / duration;

            // Parabolic path
            Vector3 currentPos = Vector3.Lerp(startPos, targetPos, t);
            currentPos.y += Mathf.Sin(t * Mathf.PI) * arcHeight;

            coinObj.transform.position = currentPos;
            
            // Spin coin
            coinObj.transform.rotation = startRot * Quaternion.Euler(t * 360f, t * 720f, 0f);

            // Scale down slightly as it reaches destination
            coinObj.transform.localScale = Vector3.Lerp(startScale, startScale * 0.4f, t);

            yield return null;
        }

        if (coinObj != null)
        {
            Destroy(coinObj);
        }
    }

    /// <summary>
    /// Uses safe reflection to find and update any Cinemachine virtual or physical camera target (Follow and LookAt) 
    /// without requiring direct assembly references or causing compilation errors.
    /// </summary>
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
                    // Attempt to set Follow property or field
                    var followProp = comp.GetType().GetProperty("Follow");
                    if (followProp != null) followProp.SetValue(comp, target);
                    else
                    {
                        var followField = comp.GetType().GetField("Follow");
                        if (followField != null) followField.SetValue(comp, target);
                    }

                    // Attempt to set LookAt property or field
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

    /// <summary>
    /// Set Follow and LookAt targets on a specific Cinemachine Camera using safe reflection.
    /// </summary>
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
                    // Set Follow
                    var followProp = comp.GetType().GetProperty("Follow");
                    if (followProp != null) followProp.SetValue(comp, target);
                    else
                    {
                        var followField = comp.GetType().GetField("Follow");
                        if (followField != null) followField.SetValue(comp, target);
                    }

                    // Set LookAt
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
