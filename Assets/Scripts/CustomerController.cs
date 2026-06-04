using UnityEngine;

[RequireComponent(typeof(Customer))]
public class CustomerController : MonoBehaviour
{
    public enum CustomerState
    {
        Spawning,
        WalkingToQueue,
        WaitingInQueue,
        AtRegister,
        Leaving,
        Deactive
    }

    [SerializeField] private float moveSpeed = 3.5f;

    [SerializeField] private float rotationSpeed = 10f;

    [SerializeField] private float arrivalDistance = 0.1f;

    [SerializeField] private Animator animator;

    private Customer customer;
    private Vector3 targetDestination;
    private CustomerState currentState = CustomerState.Deactive;
    private System.Action onArrivalCallback;

    // Animator hash for performance
    private readonly int runHash = Animator.StringToHash("Run");

    private void Awake()
    {
        customer = GetComponent<Customer>();


        animator = GetComponentInChildren<Animator>();
    }

    private void Update()
    {
        if (currentState == CustomerState.Deactive) return;

        // Perform movement towards target destination
        if (currentState == CustomerState.WalkingToQueue || currentState == CustomerState.Leaving)
        {
            MoveTowardsDestination();
        }
    }

    public void WalkTo(Vector3 destination, CustomerState nextState, System.Action onArrival = null)
    {
        targetDestination = new Vector3(destination.x, transform.position.y, destination.z); // Keep height consistent
        currentState = nextState;
        onArrivalCallback = onArrival;

        // Set walking animation (Run) to true ONLY if they are in a moving state
        bool isMoving = currentState == CustomerState.WalkingToQueue || currentState == CustomerState.Leaving;
        SetWalkingAnimation(isMoving);

        // If walking to queue but not first in line, ensure UI request balloon is hidden
        if (currentState == CustomerState.WalkingToQueue)
        {
            HideRequestUI();
        }
    }

    /// <summary>
    /// Handles frame-by-frame movement and rotation towards targetDestination.
    /// </summary>
    private void MoveTowardsDestination()
    {
        // 1. Calculate direction ignoring Y axis
        Vector3 currentPos = transform.position;
        Vector3 targetPos = new Vector3(targetDestination.x, currentPos.y, targetDestination.z);
        Vector3 moveDirection = (targetPos - currentPos);

        float distance = moveDirection.magnitude;

        // 2. Check if reached destination
        if (distance <= arrivalDistance)
        {
            // Lock to exact target position
            transform.position = targetPos;

            // Stop walking animation
            SetWalkingAnimation(false);

            // Execute arrival logic
            OnReachedDestination();
            return;
        }

        // 3. Move transform
        Vector3 nextPos = Vector3.MoveTowards(currentPos, targetPos, moveSpeed * Time.deltaTime);
        transform.position = nextPos;

        // 4. Smoothly rotate to face movement direction
        if (moveDirection.sqrMagnitude > 0.001f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(moveDirection.normalized);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
        }
    }

    /// <summary>
    /// Called when the customer successfully arrives at the targetDestination.
    /// </summary>
    private void OnReachedDestination()
    {
        if (currentState == CustomerState.WalkingToQueue)
        {
            // Execute callback (which usually updates queue lists in manager)
            onArrivalCallback?.Invoke();
        }
        else if (currentState == CustomerState.Leaving)
        {
            // Completed exiting, deactivate customer and return to pool
            currentState = CustomerState.Deactive;
            onArrivalCallback?.Invoke();
            gameObject.SetActive(false);
        }
    }

    private void SetWalkingAnimation(bool isWalking)
    {
        if (animator != null)
        {
            animator.SetBool(runHash, isWalking);
        }
    }

    public void ShowRequestUI()
    {
        if (customer != null)
        {
            customer.SetCanvasActive(true);
            customer.UpdateRequestUI();
        }
    }

    public void HideRequestUI()
    {
        if (customer != null)
        {
            customer.SetCanvasActive(false);
        }
    }

    /// <summary>
    /// Resets customer state for reuse from Object Pool.
    /// </summary>
    public void ResetCustomer()
    {
        currentState = CustomerState.Spawning;
        SetWalkingAnimation(false);

        if (customer != null)
        {
            // Generate a fresh random order for the new visit
            customer.GenerateRandomOrder();
        }
    }


    public CustomerState GetCurrentState() => currentState;
    public Customer GetCustomerData() => customer;
}
