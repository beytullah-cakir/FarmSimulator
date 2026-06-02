using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class LoadingScreenController : MonoBehaviour
{
    [Header("UI Component References")]
    [Tooltip("The UI Slider component representing the loading bar.")]
    [SerializeField] private Slider loadingSlider;

    [Header("Transition Settings")]
    [Tooltip("The duration of the loading screen in seconds.")]
    [SerializeField] private float loadingDuration = 3f;

    [Tooltip("The build index of the scene to load after the loading bar is full.")]
    [SerializeField] private int targetSceneIndex = 1;

    private void Start()
    {
        if (loadingSlider != null)
        {
            loadingSlider.value = 0f;
        }

        if (targetSceneIndex < 0)
        {
            Debug.LogWarning("[LoadingScreenController] Target scene index is less than 0. Please set a valid Build Index in the Inspector.");
        }

        StartCoroutine(StartLoadingSequence());
    }

    private IEnumerator StartLoadingSequence()
    {
        float elapsed = 0f;

        while (elapsed < loadingDuration)
        {
            // Calculate progress value from 0 to 1 using unscaled time to avoid freeze on pause
            elapsed += Time.unscaledDeltaTime;
            float progress = Mathf.Clamp01(elapsed / loadingDuration);

            // Update UI Slider
            if (loadingSlider != null)
            {
                loadingSlider.value = progress;
            }

            yield return null;
        }

        // Ensure UI is fully filled at the end
        if (loadingSlider != null)
        {
            loadingSlider.value = 1f;
        }

        // Delay slightly before loading the scene for smooth transition feel
        yield return new WaitForSecondsRealtime(0.2f);

        // Load the next scene
        if (targetSceneIndex >= 0)
        {
            SceneManager.LoadScene(targetSceneIndex);
        }
        else
        {
            Debug.LogError("[LoadingScreenController] Cannot load scene because targetSceneIndex is invalid.");
        }
    }
}
