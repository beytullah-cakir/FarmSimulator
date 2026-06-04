using System.Collections;
using UnityEngine;

public class Prop : MonoBehaviour
{
    [SerializeField] private FruitData fruitData;
    [SerializeField] private int currentAmount;
    [SerializeField] private int maxAmount;
    [SerializeField] private int currentPrice;
    [SerializeField] private float regrowthDuration = 5f;
    [SerializeField] private Canvas customCanvas;
    [SerializeField] private UnityEngine.UI.Slider customSlider;

    private bool isRegenerating = false;

    public FruitData FruitData => fruitData;
    public int CurrentAmount => currentAmount;
    public int MaxAmount => maxAmount;
    public int CurrentPrice => currentPrice;
    public bool IsRegenerating => isRegenerating;

    public void SetRegrowthDuration(float duration) => regrowthDuration = duration;

    private void Awake()
    {
        currentPrice = fruitData.BasePrice;
        if (currentAmount == 0 && maxAmount > 0)
            currentAmount = maxAmount;
    }

    private void Start()
    {
        if (customCanvas == null) customCanvas = GetComponentInChildren<Canvas>(true);
        if (customCanvas != null) customCanvas.gameObject.SetActive(false);
        if (customSlider == null) customSlider = GetComponentInChildren<UnityEngine.UI.Slider>(true);
    }

    public int Harvest(int amountToHarvest)
    {
        if (isRegenerating) return 0;

        int harvested = Mathf.Min(amountToHarvest, currentAmount);
        currentAmount -= harvested;

        if (currentAmount <= 0 && !isRegenerating)
            StartRegeneration();

        return harvested;
    }

    public void Regrow(int amountToRegrow) => currentAmount = Mathf.Min(currentAmount + amountToRegrow, maxAmount);

    public void UpdatePrice(int newPrice) => currentPrice = newPrice;

    public void StartRegeneration()
    {
        if (isRegenerating) return;
        StartCoroutine(RegenerationRoutine());
    }

    private IEnumerator RegenerationRoutine()
    {
        isRegenerating = true;
        currentAmount = 0;

        if (customCanvas != null) customCanvas.gameObject.SetActive(true);

        float elapsed = 0f;
        while (elapsed < regrowthDuration)
        {
            elapsed += Time.deltaTime;
            if (customSlider != null) customSlider.value = elapsed / regrowthDuration;
            yield return null;
        }

        Regrow(maxAmount);
        if (customCanvas != null) customCanvas.gameObject.SetActive(false);
        isRegenerating = false;
    }
}
