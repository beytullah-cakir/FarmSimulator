using UnityEngine;
using Unity.Services.LevelPlay;

public class LevelPlayManager : MonoBehaviour
{
    [SerializeField]
    private string appKey = "27014818d";

    private void Start()
    {
        Debug.Log("LevelPlay initializing...");

        LevelPlay.OnInitSuccess += OnInitSuccess;
        LevelPlay.OnInitFailed += OnInitFailed;

        LevelPlay.Init(appKey);
    }

    private void OnInitSuccess(LevelPlayConfiguration config)
    {
        Debug.Log("LevelPlay initialized successfully");

        RewardedAdsManager.Instance.Initialize();
    }

    private void OnInitFailed(LevelPlayInitError error)
    {
        Debug.LogError($"❌ Initialization failed: {error.ErrorMessage}");
    }

    private void OnDestroy()
    {
        LevelPlay.OnInitSuccess -= OnInitSuccess;
        LevelPlay.OnInitFailed -= OnInitFailed;
    }
}