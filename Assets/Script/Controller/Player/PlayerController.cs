using Mirror;
using UnityEngine;
using Unity.Cinemachine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Animator))]
public class PlayerController : NetworkBehaviour
{
    [Header("Settings")]
    [SerializeField] private float moveSpeed = 5f;

    private Rigidbody2D rb;
    private Animator animator;
    private Vector2 moveInput;

    // Input cache gửi lên server
    private Vector2 serverMoveInput;

    [SyncVar(hook = nameof(OnFacingChanged))]
    private bool facingRight = false;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        rb.gravityScale = 0f;
    }

    public override void OnStartLocalPlayer()
    {
        Debug.Log("[PlayerController] LocalPlayer started.");

        var vcam = FindFirstObjectByType<CinemachineCamera>();
        if (vcam != null)
        {
            vcam.Target.TrackingTarget = transform;
            vcam.Target.LookAtTarget = transform;
            Debug.Log("[PlayerController] VCam Tracking Target set.");
        }
        else
        {
            Debug.LogWarning("[PlayerController] CinemachineCamera not found!");
        }
    }

    public override void OnStartClient()
    {
        base.OnStartClient();
        ApplyFacing(facingRight);
    }

    void Update()
    {
        if (!isLocalPlayer) return;

        // 1) Đọc input local
        moveInput = new Vector2(
            Input.GetAxisRaw("Horizontal"),
            Input.GetAxisRaw("Vertical")
        ).normalized;

        // 2) Gửi input move lên server
        CmdSetMoveInput(moveInput);

        // 3) Tính hướng mặt và gửi lên server nếu đổi
        var cam = Camera.main;
        if (cam != null)
        {
            Vector3 mouseWorld = cam.ScreenToWorldPoint(Input.mousePosition);
            bool shouldFaceRight = mouseWorld.x >= transform.position.x;

            if (shouldFaceRight != facingRight)
                CmdSetFacing(shouldFaceRight);
        }

        // 4) Anim local (đỡ delay cảm giác điều khiển)
        if (animator != null)
            animator.SetFloat("Speed", moveInput.magnitude);
    }

    // Chỉ server chạy physics chính thức
    [ServerCallback]
    void FixedUpdate()
    {
        rb.linearVelocity = serverMoveInput * moveSpeed;

        // Nếu muốn anim đồng bộ theo server, dùng dòng dưới:
        // animator.SetFloat("Speed", serverMoveInput.magnitude);
    }

    [Command]
    private void CmdSetMoveInput(Vector2 input)
    {
        serverMoveInput = input;
    }

    [Command]
    private void CmdSetFacing(bool right)
    {
        facingRight = right;
    }

    private void OnFacingChanged(bool oldValue, bool newValue)
    {
        ApplyFacing(newValue);
    }

    private void ApplyFacing(bool right)
    {
        transform.localScale = new Vector3(right ? 1 : -1, 1, 1);
    }
}
