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
                return false;
        }
        return true;
    }
}

public class MobileJoystick : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
{
    public static MobileJoystick Instance { get; private set; }

    [SerializeField] private RectTransform background;
    [SerializeField] private RectTransform handle;
    [SerializeField] private float dragRange = 100f;

    private Canvas canvas;
    private Camera uiCamera;

    public Vector2 InputDirection { get; private set; } = Vector2.zero;

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
        {
            Destroy(gameObject);
            return;
        }

        canvas = GetComponentInParent<Canvas>();
        if (canvas != null && canvas.renderMode == RenderMode.ScreenSpaceCamera)
            uiCamera = canvas.worldCamera;

        Graphic graphic = GetComponent<Graphic>();
        if (graphic != null && graphic is not JoystickTouchZone)
        {
            if (graphic is Image img)
            {
                Color oldColor = img.color;
                Sprite oldSprite = img.sprite;
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

        if (background != null) background.gameObject.SetActive(false);
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (background == null || handle == null) return;

        if (RectTransformUtility.ScreenPointToWorldPointInRectangle(
            background.parent as RectTransform, eventData.position, uiCamera, out Vector3 worldPoint))
        {
            background.position = worldPoint;
            background.gameObject.SetActive(true);
        }

        handle.anchoredPosition = Vector2.zero;
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (background == null || handle == null || !background.gameObject.activeSelf) return;

        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
            background, eventData.position, uiCamera, out Vector2 localPoint))
        {
            Vector2 direction = localPoint.normalized;
            float clampedDistance = Mathf.Min(localPoint.magnitude, dragRange);
            handle.anchoredPosition = direction * clampedDistance;
            InputDirection = direction * (clampedDistance / dragRange);
        }
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        InputDirection = Vector2.zero;
        if (handle != null) handle.anchoredPosition = Vector2.zero;
        if (background != null) background.gameObject.SetActive(false);
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }
}
