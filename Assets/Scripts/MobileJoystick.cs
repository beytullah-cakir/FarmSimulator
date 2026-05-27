using UnityEngine;
using UnityEngine.EventSystems;

public class MobileJoystick : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
{
    public static MobileJoystick Instance { get; private set; }

    [Header("Joystick UI Elements")]
    [Tooltip("The outer ring/background of the joystick (should be a child GameObject of the Canvas).")]
    [SerializeField] private RectTransform background;

    [Tooltip("The inner knob/handle of the joystick.")]
    [SerializeField] private RectTransform handle;

    [Header("Settings")]
    [Tooltip("Maximum distance the handle can be dragged away from the center (in pixels).")]
    [SerializeField] private float dragRange = 100f;

    private Canvas canvas;
    private Camera uiCamera;

    public Vector2 InputDirection { get; private set; } = Vector2.zero;

    private void Awake()
    {
        // Setup Singleton instance
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        canvas = GetComponentInParent<Canvas>();
        if (canvas == null)
        {
            Debug.LogError("MobileJoystick must be placed inside a Canvas to function correctly.");
        }
        else if (canvas.renderMode == RenderMode.ScreenSpaceCamera)
        {
            uiCamera = canvas.worldCamera;
        }

        // Ensure the GameObject has a Graphic component to capture touch raycasts
        UnityEngine.UI.Graphic graphic = GetComponent<UnityEngine.UI.Graphic>();
        if (graphic == null)
        {
            // Automatically add a transparent Image to capture touch events without visual footprint
            UnityEngine.UI.Image image = gameObject.AddComponent<UnityEngine.UI.Image>();
            image.color = new Color(0f, 0f, 0f, 0f); // Fully transparent
            image.raycastTarget = true;
            Debug.Log("MobileJoystick: Automatically added an invisible Image component to capture touches.");
        }
        else
        {
            graphic.raycastTarget = true;
        }

        // Hide the joystick visual initially
        if (background != null)
        {
            background.gameObject.SetActive(false);
        }
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (background == null || handle == null) return;

        // Position the background at the exact touch location using world coordinates
        // This is extremely robust and ignores background anchors/pivots
        Vector3 worldPoint;
        if (RectTransformUtility.ScreenPointToWorldPointInRectangle(
            background.parent as RectTransform,
            eventData.position,
            uiCamera,
            out worldPoint
        ))
        {
            background.position = worldPoint;
            background.gameObject.SetActive(true);
        }

        handle.anchoredPosition = Vector2.zero;
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (background == null || handle == null || !background.gameObject.activeSelf) return;

        // Convert pointer position to a local position relative to the joystick background center
        Vector2 localPoint;
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
            background,
            eventData.position,
            uiCamera,
            out localPoint
        ))
        {
            float distance = localPoint.magnitude;
            Vector2 direction = localPoint.normalized;

            // Clamp handle position within the dragRange boundary
            float clampedDistance = Mathf.Min(distance, dragRange);
            handle.anchoredPosition = direction * clampedDistance;

            // Update direction vector normalized between 0 and 1
            InputDirection = direction * (clampedDistance / dragRange);
        }
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        // Reset input and hide the joystick visual on release
        InputDirection = Vector2.zero;
        if (handle != null)
        {
            handle.anchoredPosition = Vector2.zero;
        }

        if (background != null)
        {
            background.gameObject.SetActive(false);
        }
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }
}
