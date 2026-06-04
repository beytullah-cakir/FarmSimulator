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
    private readonly int runHash = Animator.StringToHash("Run");

    private void Awake()
    {
        customer = GetComponent<Customer>();
        animator = GetComponentInChildren<Animator>();
    }

    private void Update()
    {
        if (currentState == CustomerState.Deactive) return;

        if (currentState == CustomerState.WalkingToQueue || currentState == CustomerState.Leaving)
            MoveTowardsDestination();
    }

    public void WalkTo(Vector3 destination, CustomerState nextState, System.Action onArrival = null)
    {
        targetDestination = new Vector3(destination.x, transform.position.y, destination.z);
        currentState = nextState;
        onArrivalCallback = onArrival;

        bool isMoving = currentState == CustomerState.WalkingToQueue || currentState == CustomerState.Leaving;
        SetWalkingAnimation(isMoving);

        if (currentState == CustomerState.WalkingToQueue)
            HideRequestUI();
    }

    private void MoveTowardsDestination()
    {
        Vector3 currentPos = transform.position;
        Vector3 targetPos = new Vector3(targetDestination.x, currentPos.y, targetDestination.z);
        Vector3 moveDirection = targetPos - currentPos;
        float distance = moveDirection.magnitude;

        if (distance <= arrivalDistance)
        {
            transform.position = targetPos;
            SetWalkingAnimation(false);
            OnReachedDestination();
            return;
        }

        transform.position = Vector3.MoveTowards(currentPos, targetPos, moveSpeed * Time.deltaTime);

        if (moveDirection.sqrMagnitude > 0.001f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(moveDirection.normalized);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
        }
    }

    private void OnReachedDestination()
    {
        if (currentState == CustomerState.WalkingToQueue)
        {
            onArrivalCallback?.Invoke();
        }
        else if (currentState == CustomerState.Leaving)
        {
            currentState = CustomerState.Deactive;
            onArrivalCallback?.Invoke();
            gameObject.SetActive(false);
        }
    }

    private void SetWalkingAnimation(bool isWalking)
    {
        if (animator != null) animator.SetBool(runHash, isWalking);
    }

    public void ShowRequestUI()
    {
        if (customer == null) return;
        customer.SetCanvasActive(true);
        customer.UpdateRequestUI();
    }

    public void HideRequestUI()
    {
        if (customer != null) customer.SetCanvasActive(false);
    }

    public void ResetCustomer()
    {
        currentState = CustomerState.Spawning;
        SetWalkingAnimation(false);
        if (customer != null) customer.GenerateRandomOrder();
    }

    public CustomerState GetCurrentState() => currentState;
    public Customer GetCustomerData() => customer;
}
