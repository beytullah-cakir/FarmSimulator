using UnityEngine;
using TMPro;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }

    [Header("Money UI References (TextMesh Pro Only)")]
    [Tooltip("The TextMeshProUGUI component to display the money. If left blank, it will try to find one in children.")]
    [SerializeField] private TextMeshProUGUI moneyText;

    [Header("Screen Panels")]
    [Tooltip("The main gameplay UI overlay panel.")]
    [SerializeField] private GameObject gameplayPanel;

    [Tooltip("The pause menu overlay panel.")]
    [SerializeField] private GameObject pausePanel;

    [Tooltip("The settings panel overlay.")]
    [SerializeField] private GameObject settingsPanel;

    private void Awake()
    {
        // Singleton initialization
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        // Auto-find TextMeshProUGUI component in children if null
        if (moneyText == null)
        {
            moneyText = GetComponentInChildren<TextMeshProUGUI>(true);
        }
    }

    private void Start()
    {
        // Subscribe to GameManager's money changes
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnMoneyChanged += UpdateMoneyDisplay;
            
            // Set initial money display
            UpdateMoneyDisplay(GameManager.Instance.PlayerMoney);
        }

        // Ensure panels start in their correct default state
        InitializeUI();
    }

    private void OnDestroy()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnMoneyChanged -= UpdateMoneyDisplay;
        }
    }

    /// <summary>
    /// Sets up default active/inactive state for all panels at game start.
    /// </summary>
    public void InitializeUI()
    {
        if (gameplayPanel != null) gameplayPanel.SetActive(true);
        if (pausePanel != null) pausePanel.SetActive(false);
        if (settingsPanel != null) settingsPanel.SetActive(false);
    }

    /// <summary>
    /// Opens or closes the pause panel, pausing/unpausing the game time scale.
    /// </summary>
    /// <param name="active">True to open pause menu, false to close.</param>
    public void TogglePauseMenu(bool active)
    {
        if (pausePanel != null)
        {
            pausePanel.SetActive(active);
            
            // Toggle timescale depending on pause state
            Time.timeScale = active ? 0f : 1f;
        }
    }

    /// <summary>
    /// Opens or closes the settings panel.
    /// </summary>
    /// <param name="active">True to open settings, false to close.</param>
    public void ToggleSettingsMenu(bool active)
    {
        if (settingsPanel != null)
        {
            settingsPanel.SetActive(active);
        }
    }

    /// <summary>
    /// Sets the visibility of the main gameplay overlay.
    /// </summary>
    public void SetGameplayPanelActive(bool active)
    {
        if (gameplayPanel != null)
        {
            gameplayPanel.SetActive(active);
        }
    }

    /// <summary>
    /// Updates the text display with the current money value using TextMeshPro.
    /// </summary>
    /// <param name="currentMoney">The new player money balance.</param>
    public void UpdateMoneyDisplay(int currentMoney)
    {
        // Update TextMeshPro Text
        if (moneyText != null)
        {
            moneyText.text = currentMoney.ToString();
        }
    }
}
