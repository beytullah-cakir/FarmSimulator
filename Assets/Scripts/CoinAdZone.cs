using UnityEngine;

/// <summary>
/// Reklam izlenince belirlenen miktarda coin kazandirir.
/// </summary>
public class CoinAdZone : BaseAdZone
{
    [Header("Coin Reward")]
    [SerializeField] private int coinReward = 200;

    protected override void GrantReward()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.AddMoney(coinReward);

        Debug.Log($"[CoinAdZone] +{coinReward} coin verildi.");
    }

    protected override void SetDescriptionText()
    {
        if (descriptionText != null)
            descriptionText.text = $"+{FormatUtility.FormatNumber(coinReward)} Coin";
    }

#if UNITY_EDITOR
    protected override void OnDrawGizmos()
    {
        Gizmos.color = new Color(1f, 0.84f, 0f, 0.35f); // Altin sarisi
        base.OnDrawGizmos();
    }
#endif
}
