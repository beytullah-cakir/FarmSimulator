using UnityEngine;
using Unity.Services.LevelPlay;

public class RewardedAdsManager : MonoBehaviour
{
    private const string REWARDED_AD_UNIT_ID = "p6e7bovbwqnok23w";

    private LevelPlayRewardedAd rewardedAd;

    public static RewardedAdsManager Instance;

    private void Awake()
    {
        Instance = this;
    }

    // private void Start()
    // {
    //     rewardedAd = new LevelPlayRewardedAd(REWARDED_AD_UNIT_ID);

    //     rewardedAd.OnAdLoaded += OnAdLoaded;
    //     rewardedAd.OnAdLoadFailed += OnAdLoadFailed;
    //     rewardedAd.OnAdDisplayed += OnAdDisplayed;
    //     rewardedAd.OnAdDisplayFailed += OnAdDisplayFailed;
    //     rewardedAd.OnAdRewarded += OnAdRewarded;
    //     rewardedAd.OnAdClosed += OnAdClosed;

    //     LoadRewarded();
    // }

    public void Initialize()
    {
        rewardedAd = new LevelPlayRewardedAd(REWARDED_AD_UNIT_ID);

        rewardedAd.OnAdLoaded += OnAdLoaded;
        rewardedAd.OnAdLoadFailed += OnAdLoadFailed;
        rewardedAd.OnAdDisplayed += OnAdDisplayed;
        rewardedAd.OnAdDisplayFailed += OnAdDisplayFailed;
        rewardedAd.OnAdRewarded += OnAdRewarded;
        rewardedAd.OnAdClosed += OnAdClosed;
        Debug.Log("Rewarded Initialize");
        LoadRewarded();
    }

    public void LoadRewarded()
    {
        Debug.Log("Loading Rewarded...");
        rewardedAd.LoadAd();
    }

    public void ShowRewarded()
    {
        if (rewardedAd.IsAdReady())
        {
            rewardedAd.ShowAd();
        }
        else
        {
            Debug.Log("Rewarded ad is not ready.");
        }
    }

    private void OnAdLoaded(LevelPlayAdInfo info)
    {
        Debug.Log("Rewarded Loaded");
        Debug.Log(info.ToString());
    }

    private void OnAdLoadFailed(LevelPlayAdError error)
    {
        Debug.LogError("❌ Load Failed : " + error.ErrorMessage);
    }

    private void OnAdDisplayed(LevelPlayAdInfo info)
    {
        Debug.Log($"Network: {info.AdNetwork}");
        Debug.Log($"Instance: {info.InstanceName}");
    }

    private void OnAdDisplayFailed(LevelPlayAdInfo info, LevelPlayAdError error)
    {
        Debug.LogError(error.ErrorMessage);
    }

    private void OnAdRewarded(LevelPlayAdInfo info, LevelPlayReward reward)
    {
        Debug.Log("🎉 User earned reward!");

        // Buraya coin verme kodunu yazacağız.
        // CoinManager.Instance.AddCoin(50);
    }

    private void OnAdClosed(LevelPlayAdInfo info)
    {
        Debug.Log("Rewarded Closed");

        // Bir sonraki reklamı hazırla
        rewardedAd.LoadAd();
    }

    private void OnDestroy()
    {
        rewardedAd?.DestroyAd();
    }
}