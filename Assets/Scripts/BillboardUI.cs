using UnityEngine;

[DisallowMultipleComponent]
public class BillboardUI : MonoBehaviour
{
    [Header("Billboard Settings")]
    [SerializeField] private bool billboardToCamera = true;
    [SerializeField] private Transform targetTransform;

    private Camera mainCamera;

    public bool BillboardToCamera
    {
        get => billboardToCamera;
        set => billboardToCamera = value;
    }

    public Transform TargetTransform
    {
        get => targetTransform;
        set => targetTransform = value;
    }

    private void Awake()
    {
        if (targetTransform == null)
        {
            targetTransform = transform;
        }
    }

    private void Start()
    {
        mainCamera = Camera.main;
    }

    private void LateUpdate()
    {
        if (billboardToCamera && mainCamera != null && targetTransform != null)
        {
            Vector3 cameraDirection = mainCamera.transform.forward;
            targetTransform.rotation = Quaternion.LookRotation(cameraDirection, mainCamera.transform.up);
        }
    }
}
