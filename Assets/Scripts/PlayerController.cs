using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

[RequireComponent(typeof(Rigidbody))]
public class PlayerController : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float rotationSpeed = 10f;
    [SerializeField] private Animator animator;
    [SerializeField] private Slider speedBoostSlider;
    [SerializeField] private TextMeshProUGUI speedBoostTimerText;

    private Rigidbody rb;
    private PlayerInputActions playerInputActions;
    private Vector3 inputDirection;
    private readonly int walkHash = Animator.StringToHash("Walking");
    private readonly int runHash = Animator.StringToHash("Running");

    private float   baseSpeed;           // Baslangic hizi
    private Coroutine speedBoostRoutine; // Aktif boost coroutine

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.constraints = RigidbodyConstraints.FreezeRotation;
        playerInputActions = new PlayerInputActions();

        if (animator == null)
            animator = GetComponentInChildren<Animator>();

        baseSpeed = moveSpeed; // Baslangic hizini kaydet
    }

    private void Start()
    {
        if (speedBoostSlider != null) speedBoostSlider.gameObject.SetActive(false);
        if (speedBoostTimerText != null) speedBoostTimerText.gameObject.SetActive(false);
    }

    private void OnEnable() => playerInputActions.Enable();
    private void OnDisable() => playerInputActions.Disable();

    private void Update()
    {
        Vector2 inputVector = playerInputActions.Player.Move.ReadValue<Vector2>();

        if (MobileJoystick.Instance != null && MobileJoystick.Instance.InputDirection.sqrMagnitude > 0.001f)
            inputVector = MobileJoystick.Instance.InputDirection;

        if (Camera.main != null)
        {
            Vector3 forward = Camera.main.transform.forward;
            Vector3 right = Camera.main.transform.right;
            forward.y = 0f;
            right.y = 0f;
            forward.Normalize();
            right.Normalize();
            inputDirection = (forward * inputVector.y + right * inputVector.x).normalized;
        }
        else
        {
            inputDirection = new Vector3(inputVector.x, 0f, inputVector.y).normalized;
        }

        MoveCharacter();
        UpdateAnimationState();
    }

    private void FixedUpdate()
    {
        rb.linearVelocity = new Vector3(0f, rb.linearVelocity.y, 0f);
    }

    private void MoveCharacter()
    {
        if (inputDirection.magnitude < 0.1f) return;

        Vector3 movement = inputDirection * moveSpeed * Time.deltaTime;
        transform.position += new Vector3(movement.x, 0f, movement.z);

        Quaternion targetRotation = Quaternion.LookRotation(inputDirection);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
    }

    private void UpdateAnimationState()
    {
        if (animator == null) return;

        bool isMoving = inputDirection.magnitude >= 0.1f;
        if (!isMoving)
        {
            animator.SetBool(walkHash, false);
            animator.SetBool(runHash, false);
        }
        else
        {
            bool hasSpeedBoost = speedBoostRoutine != null || moveSpeed > baseSpeed;
            animator.SetBool(walkHash, !hasSpeedBoost);
            animator.SetBool(runHash, hasSpeedBoost);
        }
    }

    public void SetInputActive(bool active)
    {
        enabled = active;
        if (!active)
        {
            inputDirection = Vector3.zero;
            if (rb != null) rb.linearVelocity = new Vector3(0f, rb.linearVelocity.y, 0f);
            if (animator != null)
            {
                animator.SetBool(walkHash, false);
                animator.SetBool(runHash, false);
            }
        }
    }

    /// <summary>
    /// Oyuncuya gecici hiz boostu uygular.
    /// Aktif boost varsa sure sifirlanarak yeniden baslar.
    /// </summary>
    public void ApplySpeedBoost(float multiplier, float duration)
    {
        if (speedBoostRoutine != null) StopCoroutine(speedBoostRoutine);
        speedBoostRoutine = StartCoroutine(SpeedBoostRoutine(multiplier, duration));
    }

    private IEnumerator SpeedBoostRoutine(float multiplier, float duration)
    {
        moveSpeed = baseSpeed * multiplier;
        Debug.Log($"[PlayerController] Hiz boostu aktif: {moveSpeed} ({duration}s)");

        float remaining = duration;
        while (remaining > 0f)
        {
            remaining -= Time.deltaTime;
            float fill = Mathf.Clamp01(remaining / duration);

            if (speedBoostSlider != null)
            {
                speedBoostSlider.gameObject.SetActive(true);
                speedBoostSlider.minValue = 0f;
                speedBoostSlider.maxValue = 1f;
                speedBoostSlider.value = fill;
            }

            if (speedBoostTimerText != null)
            {
                speedBoostTimerText.gameObject.SetActive(true);
                speedBoostTimerText.text = $"{Mathf.CeilToInt(remaining)}s";
            }

            if (UIManager.Instance != null)
            {
                UIManager.Instance.UpdateSpeedBoostSlider(remaining, duration);
            }

            yield return null;
        }

        if (speedBoostSlider != null) speedBoostSlider.gameObject.SetActive(false);
        if (speedBoostTimerText != null) speedBoostTimerText.gameObject.SetActive(false);

        if (UIManager.Instance != null)
        {
            UIManager.Instance.UpdateSpeedBoostSlider(0f, duration);
        }

        moveSpeed = baseSpeed;
        speedBoostRoutine = null;
        Debug.Log("[PlayerController] Hiz boostu sona erdi.");
    }
}
