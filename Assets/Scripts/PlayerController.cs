using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class PlayerController : MonoBehaviour
{
    [Header("Movement Settings")]
    [Tooltip("Movement speed of the character.")]
    [SerializeField] private float moveSpeed = 5f;

    [Tooltip("How fast the character turns to face the movement direction.")]
    [SerializeField] private float rotationSpeed = 10f;

    [Header("Animation Settings")]
    [Tooltip("Animator component for character animations. If null, will try to find in children.")]
    [SerializeField] private Animator animator;

    private Rigidbody rb;
    private PlayerInputActions playerInputActions;
    private Vector3 inputDirection;

    // Animator Boolean Parameter Hash for performance
    private readonly int runHash = Animator.StringToHash("Run");

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        
        // Freeze all rotations on the Rigidbody so physical collisions do not spin or tilt the character
        rb.constraints = RigidbodyConstraints.FreezeRotation;

        playerInputActions = new PlayerInputActions();

        // Automatically search for Animator in children if not assigned (common for 3D FBX models)
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

        // If virtual touch joystick is active and receives touch input, override PC keyboard/mouse controls
        if (MobileJoystick.Instance != null && MobileJoystick.Instance.InputDirection.sqrMagnitude > 0.001f)
        {
            inputVector = MobileJoystick.Instance.InputDirection;
        }

        inputDirection = new Vector3(inputVector.x, 0f, inputVector.y).normalized;

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

            // Binds rotation to -inputDirection to respect model import orientation
            Quaternion targetRotation = Quaternion.LookRotation(-inputDirection);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
        }
    }

    private void UpdateAnimationState()
    {
        if (animator == null) return;

        // Set the Animator Boolean "Run" parameter based on character movement
        bool isRunning = inputDirection.magnitude >= 0.1f;
        animator.SetBool(runHash, isRunning);
    }

    /// <summary>
    /// Safely enables or disables player input/movement, resetting velocity and running animations.
    /// </summary>
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
