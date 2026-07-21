using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Tum ad zone'lari icin soyut taban sinif.
/// Slider dolma / collider / reklam acma mantigini icerir.
/// Alt siniflar yalnizca GrantReward() metodunu implement eder.
/// </summary>
public abstract class BaseAdZone : MonoBehaviour
{
    // ─── Inspector ───────────────────────────────────────────────────────────

    [Header("Zone Settings")]
    [SerializeField] protected float fillDuration = 3f;

    [Header("UI References")]
    [SerializeField] protected Canvas          worldCanvas;
    [SerializeField] protected Slider          progressSlider;
    [SerializeField] protected Slider          rewardCountdownSlider;
    [SerializeField] protected TextMeshProUGUI descriptionText;

    // ─── State ───────────────────────────────────────────────────────────────

    protected bool  isPlayerInside = false;
    protected bool  isAdShowing    = false;
    protected float currentFill    = 0f;

    private Coroutine fillCoroutine;

    // ─── Unity lifecycle ─────────────────────────────────────────────────────

    protected virtual void Awake()
    {
        if (worldCanvas == null)
            worldCanvas = GetComponentInChildren<Canvas>(true);

        if (progressSlider == null && worldCanvas != null)
            progressSlider = worldCanvas.GetComponentInChildren<Slider>(true);

        if (rewardCountdownSlider == null)
            rewardCountdownSlider = progressSlider;
        else if (rewardCountdownSlider != progressSlider)
            rewardCountdownSlider.gameObject.SetActive(false);

        ResetSlider();
    }

    protected virtual void Start()
    {
        SetDescriptionText();
    }

    // ─── Trigger callbacks ───────────────────────────────────────────────────

    protected virtual void OnTriggerEnter(Collider other)
    {
        if (isAdShowing) return;

        PlayerController player = other.GetComponentInParent<PlayerController>();
        if (player == null) return;

        isPlayerInside = true;

        if (fillCoroutine != null) StopCoroutine(fillCoroutine);
        fillCoroutine = StartCoroutine(FillRoutine());
    }

    protected virtual void OnTriggerExit(Collider other)
    {
        PlayerController player = other.GetComponentInParent<PlayerController>();
        if (player == null) return;

        isPlayerInside = false;

        if (fillCoroutine != null)
        {
            StopCoroutine(fillCoroutine);
            fillCoroutine = null;
        }

        if (!isAdShowing) ResetSlider();
    }

    // ─── Fill coroutine ──────────────────────────────────────────────────────

    private IEnumerator FillRoutine()
    {
        while (isPlayerInside && currentFill < 1f)
        {
            currentFill += Time.deltaTime / fillDuration;
            currentFill = Mathf.Clamp01(currentFill);

            if (progressSlider != null) progressSlider.value = currentFill;

            if (currentFill >= 1f)
            {
                TriggerAd();
                yield break;
            }

            yield return null;
        }
    }

    // ─── Ad trigger ──────────────────────────────────────────────────────────

    private void TriggerAd()
    {
        isAdShowing = true;

        if (RewardedAdsManager.Instance != null)
        {
            RewardedAdsManager.Instance.ShowRewarded(OnAdRewarded, OnAdClosed);
        }
        else
        {
            Debug.LogWarning($"[{GetType().Name}] RewardedAdsManager bulunamadi – test modu.");
            OnAdRewarded();
            OnAdClosed();
        }
    }

    // ─── Callbacks ───────────────────────────────────────────────────────────

    private void OnAdRewarded()
    {
        Debug.Log($"[{GetType().Name}] Reklam izlendi, odul veriliyor.");
        GrantReward();
    }

    private void OnAdClosed() => Destroy(gameObject);

    // ─── Abstract & Virtual API ──────────────────────────────────────────────

    /// <summary>Sureli ad zone'lar icin boost suresini doner.</summary>
    public virtual float GetBoostDuration() => 0f;

    /// <summary>Alt siniflar bu metodu override ederek odul mantigi yazar.</summary>
    protected abstract void GrantReward();

    /// <summary>Alt siniflar bu metodu override ederek UI metnini ayarlar.</summary>
    protected abstract void SetDescriptionText();

    // ─── Helpers ─────────────────────────────────────────────────────────────

    private void ResetSlider()
    {
        currentFill = 0f;
        if (progressSlider != null) progressSlider.value = 0f;
    }

#if UNITY_EDITOR
    protected virtual void OnDrawGizmos()
    {
        Gizmos.color = new Color(1f, 0.85f, 0f, 0.3f);
        Collider col = GetComponent<Collider>();
        if (col is SphereCollider sphere)
            Gizmos.DrawSphere(transform.position, sphere.radius * transform.lossyScale.x);
        else
            Gizmos.DrawWireSphere(transform.position, 1.5f);
    }
#endif
}
