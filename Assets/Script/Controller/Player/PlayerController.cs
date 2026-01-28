using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerController : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private Animator animator;

    private Rigidbody2D rb;
    private InputSystem_Actions inputActions;
    private Vector2 moveInput;
    private Camera mainCamera;
    private bool facingRight = false;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        inputActions = new InputSystem_Actions();
        mainCamera = Camera.main;
        
        rb.gravityScale = 0f; 
        rb.constraints = RigidbodyConstraints2D.FreezeRotation;
    }

    private void OnEnable()
    {
        inputActions.Player.Enable();
    }

    private void OnDisable()
    {
        inputActions.Player.Disable();
    }

    private void Update()
    {
        moveInput = inputActions.Player.Move.ReadValue<Vector2>();
        
        HandleFlipTowardsMouse();
        UpdateAnimation();
    }

    private void FixedUpdate()
    {
        Vector2 movement = moveInput.normalized * moveSpeed;
        rb.linearVelocity = movement;
    }

    private void HandleFlipTowardsMouse()
    {
        Vector2 mousePosition = GetMouseWorldPosition();
        float mouseX = mousePosition.x - transform.position.x;

        if (mouseX > 0 && !facingRight)
        {
            Flip();
        }
        else if (mouseX < 0 && facingRight)
        {
            Flip();
        }
    }

    private Vector2 GetMouseWorldPosition()
    {
        Vector2 mouseScreenPosition = Mouse.current.position.ReadValue();
        Vector3 worldPosition = mainCamera.ScreenToWorldPoint(mouseScreenPosition);
        worldPosition.z = 0;
        return worldPosition;
    }

    private void Flip()
    {
        facingRight = !facingRight;
        Vector3 scale = transform.localScale;
        scale.x *= -1;
        transform.localScale = scale;
    }

    private void UpdateAnimation()
    {
        float speed = moveInput.magnitude;
        animator.SetFloat("Speed", speed);
    }

    // Getter để Factory có thể check facing
    public bool IsFacingRight() => facingRight;
}