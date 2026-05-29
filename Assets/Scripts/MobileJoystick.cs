using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class JoystickTouchZone : Image
{
    private MobileJoystick joystick;

    public void Setup(MobileJoystick owner)
    {
        joystick = owner;
        color = new Color(0f, 0f, 0f, 0f);
        raycastTarget = true;
    }

    public override bool IsRaycastLocationValid(Vector2 screenPoint, Camera eventCamera)
    {
        if (EventSystem.current == null) return true;

        raycastTarget = false;

        PointerEventData eventData = new PointerEventData(EventSystem.current);
        eventData.position = screenPoint;

        List<RaycastResult> results = new List<RaycastResult>();
        EventSystem.current.RaycastAll(eventData, results);

        raycastTarget = true;

        foreach (var result in results)
        {
            if (result.gameObject != gameObject &&
                (joystick == null || (!result.gameObject.transform.IsChildOf(joystick.transform) && result.gameObject != joystick.gameObject)))
            {
                return false;
            }
        }
        return true;
    }
}

public class MobileJoystick : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
{
    public static MobileJoystick Instance { get; private set; }

    [Header("Joystick UI Elements")]
    [SerializeField] private RectTransform background;
    [SerializeField] private RectTransform handle;

    [Header("Settings")]
    [SerializeField] private float dragRange = 100f;

    private Canvas canvas;
    private Camera uiCamera;

    public Vector2 InputDirection { get; private set; } = Vector2.zero;

    private void Awake()
    {
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

        Graphic graphic = GetComponent<Graphic>();
        if (graphic != null && !(graphic is JoystickTouchZone))
        {
            if (graphic is Image)
            {
                Color oldColor = ((Image)graphic).color;
                Sprite oldSprite = ((Image)graphic).sprite;
                DestroyImmediate(graphic);

                JoystickTouchZone touchZone = gameObject.AddComponent<JoystickTouchZone>();
                touchZone.Setup(this);
                touchZone.color = oldColor;
                touchZone.sprite = oldSprite;
            }
        }
        else if (graphic == null)
        {
            JoystickTouchZone touchZone = gameObject.AddComponent<JoystickTouchZone>();
            touchZone.Setup(this);
        }

        if (background != null)
        {
            background.gameObject.SetActive(false);
        }
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (background == null || handle == null) return;

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

            float clampedDistance = Mathf.Min(distance, dragRange);
            handle.anchoredPosition = direction * clampedDistance;

            InputDirection = direction * (clampedDistance / dragRange);
        }
    }

    public void OnPointerUp(PointerEventData eventData)
    {
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
