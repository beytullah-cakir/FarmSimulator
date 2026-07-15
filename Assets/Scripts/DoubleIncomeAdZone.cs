using UnityEngine;

/// <summary>
/// Reklam izlenince musterilerden alinan geliri gecici olarak 2 katina cikartir.
/// </summary>
public class DoubleIncomeAdZone : BaseAdZone
{
    [Header("Double Income")]
    [SerializeField] private float incomeMultiplier = 2f;
    [SerializeField] private float boostDuration    = 60f; // Sn cinsinden sure

    protected override void GrantReward()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.ApplyIncomeMultiplier(incomeMultiplier, boostDuration);

        Debug.Log($"[DoubleIncomeAdZone] {incomeMultiplier}x gelir carpani {boostDuration} sn uygulanacak.");
    }

    protected override void SetDescriptionText()
    {
        if (descriptionText != null)
            descriptionText.text = $"{incomeMultiplier}x Gelir  ({boostDuration}s)";
    }

#if UNITY_EDITOR
    protected override void OnDrawGizmos()
    {
        Gizmos.color = new Color(0.2f, 0.9f, 0.4f, 0.35f); // Yesil
        base.OnDrawGizmos();
    }
#endif
}
