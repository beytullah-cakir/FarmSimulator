using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class PlayerController : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float rotationSpeed = 10f;

    [Header("Animation Settings")]
    [SerializeField] private Animator animator;

    private Rigidbody rb;
    private PlayerInputActions playerInputActions;
    private Vector3 inputDirection;

    private readonly int runHash = Animator.StringToHash("Run");

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.constraints = RigidbodyConstraints.FreezeRotation;
        playerInputActions = new PlayerInputActions();

        if (animator == null)
        {
            animator = GetComponentInChildren<Animator>();
        }
    }

    private void OnEnable()
    {
        playerInputActions.Enable();
    }

    private void OnDisable()
    {
        playerInputActions.Disable();
    }

    private void Update()
    {
        Vector2 inputVector = playerInputActions.Player.Move.ReadValue<Vector2>();

        if (MobileJoystick.Instance != null && MobileJoystick.Instance.InputDirection.sqrMagnitude > 0.001f)
        {
            inputVector = MobileJoystick.Instance.InputDirection;
        }

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
        if (inputDirection.magnitude >= 0.1f)
        {
            Vector3 movement = inputDirection * moveSpeed * Time.deltaTime;
            transform.position += new Vector3(movement.x, 0f, movement.z);

            Quaternion targetRotation = Quaternion.LookRotation(inputDirection);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
        }
    }

    private void UpdateAnimationState()
    {
        if (animator == null) return;

        bool isRunning = inputDirection.magnitude >= 0.1f;
        animator.SetBool(runHash, isRunning);
    }

    public void SetInputActive(bool active)
    {
        enabled = active;
        if (!active)
        {
            inputDirection = Vector3.zero;
            if (rb != null)
            {
                rb.linearVelocity = new Vector3(0f, rb.linearVelocity.y, 0f);
            }
            if (animator != null)
            {
                animator.SetBool(runHash, false);
            }
        }
    }
}
