using UnityEngine;

/// <summary>
/// Reklam izlenince oyuncunun hizini gecici olarak arttirir.
/// </summary>
public class SpeedBoostAdZone : BaseAdZone
{
    [Header("Speed Boost")]
    [SerializeField] private float speedMultiplier = 2f;   // Hiz carpani (ornek: 2x)
    [SerializeField] private float boostDuration   = 30f;  // Sn cinsinden sure

    protected override void GrantReward()
    {
        PlayerController player = FindAnyObjectByType<PlayerController>();
        if (player != null)
            player.ApplySpeedBoost(speedMultiplier, boostDuration);

        Debug.Log($"[SpeedBoostAdZone] {speedMultiplier}x hiz boostu {boostDuration} sn uygulanacak.");
    }

    protected override void SetDescriptionText()
    {
        if (descriptionText != null)
            descriptionText.text = $"{speedMultiplier}x Hiz  ({boostDuration}s)";
    }

#if UNITY_EDITOR
    protected override void OnDrawGizmos()
    {
        Gizmos.color = new Color(0.2f, 0.6f, 1f, 0.35f); // Mavi
        base.OnDrawGizmos();
    }
#endif
}
