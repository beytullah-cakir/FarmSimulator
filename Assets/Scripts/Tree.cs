using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Tree : MonoBehaviour
{
    [SerializeField] private FruitData fruitData;

    [SerializeField] private int currentAmount;

    [SerializeField] private int maxAmount;
    [SerializeField] private int currentPrice;

    [Header("Regeneration Settings")]
    [SerializeField] private float regrowthDuration = 5f;

    [Header("Regeneration UI References (Optional)")]
    [SerializeField] private Canvas customCanvas;
    [SerializeField] private Slider customSlider;

    private bool isRegenerating = false;

    // Public properties to access tree data programmatically
    public FruitData FruitData => fruitData;
    public int CurrentAmount => currentAmount;
    public int MaxAmount => maxAmount;
    public int CurrentPrice => currentPrice;
    public bool IsRegenerating => isRegenerating;

    public void SetRegrowthDuration(float duration)
    {
        regrowthDuration = duration;
    }

    private void Awake()
    {
        currentPrice = fruitData.BasePrice;
        if (currentAmount == 0 && maxAmount > 0)
        {
            currentAmount = maxAmount;
        }
    }

    private void Start()
    {
        // Find custom canvas if not assigned, and hide initially
        if (customCanvas == null)
        {
            customCanvas = GetComponentInChildren<Canvas>(true);
        }
        if (customCanvas != null)
        {
            customCanvas.gameObject.SetActive(false);
        }

        if (customSlider == null)
        {
            customSlider = GetComponentInChildren<Slider>(true);
        }
    }

    public int Harvest(int amountToHarvest)
    {
        if (isRegenerating) return 0;

        int harvested = Mathf.Min(amountToHarvest, currentAmount);
        currentAmount -= harvested;

        if (currentAmount <= 0 && !isRegenerating)
        {
            StartRegeneration();
        }

        return harvested;
    }

    public void Regrow(int amountToRegrow)
    {
        currentAmount = Mathf.Min(currentAmount + amountToRegrow, maxAmount);
    }

    public void UpdatePrice(int newPrice)
    {
        currentPrice = newPrice;
    }

    public void StartRegeneration()
    {
        if (isRegenerating) return;
        StartCoroutine(RegenerationRoutine());
    }

    private IEnumerator RegenerationRoutine()
    {
        isRegenerating = true;
        currentAmount = 0;

        // Show/enable the custom slider canvas
        ShowSliderCanvas(true);

        float elapsed = 0f;
        while (elapsed < regrowthDuration)
        {
            elapsed += Time.deltaTime;
            float progress = elapsed / regrowthDuration;
            UpdateSliderValue(progress);
            yield return null;
        }

        // Finish regeneration
        Regrow(maxAmount);
        ShowSliderCanvas(false);
        isRegenerating = false;
    }

    private void ShowSliderCanvas(bool show)
    {
        if (customCanvas == null)
        {
            customCanvas = GetComponentInChildren<Canvas>(true);
        }

        if (customSlider == null)
        {
            customSlider = GetComponentInChildren<Slider>(true);
        }

        if (customCanvas != null)
        {
            customCanvas.gameObject.SetActive(show);
        }
    }

    private void UpdateSliderValue(float value)
    {
        if (customSlider != null)
        {
            customSlider.value = value;
        }
    }
}
