using Mirror;
using UnityEngine;
using Unity.Cinemachine;

public class PlayerController : NetworkBehaviour
{
    [Header("Settings")]
    [SerializeField] private float moveSpeed = 5f;

    private Rigidbody2D rb;
    private Animator    animator;
    private Vector2     moveInput;

    // ══════════════════════════════════════════════════════════════
    // LIFECYCLE
    // ══════════════════════════════════════════════════════════════

    void Awake()
    {
        rb              = GetComponent<Rigidbody2D>();
        animator        = GetComponent<Animator>();
        rb.gravityScale = 0f;
    }

    public override void OnStartLocalPlayer()
    {
        Debug.Log("[PlayerController] LocalPlayer started.");

        // ── Cinemachine 3.x — Tracking Target ────────────────────
        var vcam = FindFirstObjectByType<CinemachineCamera>();
        if (vcam != null)
        {
            // Cinemachine 3.x dùng Target thay vì Follow/LookAt trực tiếp
            vcam.Target.TrackingTarget = transform;
            vcam.Target.LookAtTarget   = transform;
            Debug.Log("[PlayerController] VCam Tracking Target → localPlayer.");
        }
        else
        {
            Debug.LogWarning("[PlayerController] Không tìm thấy CinemachineCamera!");
        }
        // ─────────────────────────────────────────────────────────
    }

    // ══════════════════════════════════════════════════════════════
    // INPUT & MOVEMENT
    // ══════════════════════════════════════════════════════════════

    void Update()
    {
        if (!isLocalPlayer) return;

        moveInput = new Vector2(
            Input.GetAxisRaw("Horizontal"),
            Input.GetAxisRaw("Vertical")
        ).normalized;

        if (moveInput.x != 0)
            transform.localScale = new Vector3(
                moveInput.x > 0 ? 1 : -1, 1, 1
            );

        if (animator != null)
            animator.SetBool("isMoving", moveInput != Vector2.zero);
    }

    void FixedUpdate()
    {
        if (!isLocalPlayer) return;
        rb.linearVelocity = moveInput * moveSpeed;
    }
}
