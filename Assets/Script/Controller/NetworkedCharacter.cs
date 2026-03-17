// NetworkedCharacter.cs
// Gan vao moi character prefab trong BattleScene.
//
// ┌─────────────────────────────────────────────────────────────────┐
// │  LUONG DONG BO ANIMATION                                        │
// │                                                                  │
// │  LOCAL player:                                                   │
// │    Doc input → tinh speed → set Animator.Speed NGAY             │
// │                           → gui speed len server                 │
// │                                                                  │
// │  REMOTE player:                                                  │
// │    Nhan "playerMoved" message → lay speed trong message          │
// │                               → set Animator.Speed              │
// │    (Animator tu quyet dinh IDLE hay MOVE)                        │
// │                                                                  │
// │  KEY INSIGHT:                                                    │
// │    Khong dong bo ten animation — dong bo THAM SO (speed)        │
// │    Animator tren moi may tu chay dung animation                  │
// └─────────────────────────────────────────────────────────────────┘
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Animator))]
public class NetworkedCharacter : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float _moveSpeed    = 5f;
    [SerializeField] private float _lerpSpeed    = 15f;
    [SerializeField] private float _sendInterval = 0.05f;

    // ── Components ─────────────────────────────────────────────────
    private Rigidbody2D _rb;
    private Animator    _anim;

    // Ten parameter trong Animator Controller (phai dat dung y chang)
    private static readonly int SpeedParam = Animator.StringToHash("Speed");
    //                                        ↑ dung Hash thay string de nhanh hon

    // ── Network State ──────────────────────────────────────────────
    private string _sessionId;
    private bool   _isLocalPlayer;

    // Local: theo doi gia tri cuoi da gui de tranh gui trung lap
    private float _sendTimer;
    private float _lastSentX, _lastSentY, _lastSentFacing, _lastSentSpeed;

    // Remote: target vi tri nhan tu server, dung de lerp
    private Vector2 _targetPos;
    private float   _targetFacing;
    private float   _targetSpeed;  // [ANIMATION] speed nhan tu server

    // ── Init ───────────────────────────────────────────────────────
    public void Init(string sessionId, bool isLocalPlayer)
    {
        _sessionId     = sessionId;
        _isLocalPlayer = isLocalPlayer;
        _rb            = GetComponent<Rigidbody2D>();
        _anim          = GetComponent<Animator>();

        _rb.bodyType = isLocalPlayer ? RigidbodyType2D.Dynamic : RigidbodyType2D.Kinematic;

        if (isLocalPlayer)
        {
            GameRoomManager.Instance?.SendSetCharacter(0);
        }
        else
        {
            // [ANIMATION SYNC - BUOC DANG KY]
            // Remote player lang nghe event OnPlayerMoved tu GameRoomManager.
            // Moi khi server broadcast "playerMoved", event nay se fire.
            var mgr = GameRoomManager.Instance;
            if (mgr != null) mgr.OnPlayerMoved += HandlePlayerMoved;
        }
    }

    private void OnDestroy()
    {
        if (_isLocalPlayer)
        {
            if (_rb != null) _rb.linearVelocity = Vector2.zero;
        }
        else
        {
            // Huy dang ky khi character bi destroy, tranh memory leak
            var mgr = GameRoomManager.Instance;
            if (mgr != null) mgr.OnPlayerMoved -= HandlePlayerMoved;
        }
    }

    // ── Update ─────────────────────────────────────────────────────
    private void Update()
    {
        if (_isLocalPlayer)
            HandleLocalInput();
        else
            LerpRemotePosition();
    }

    // ── LOCAL PLAYER ───────────────────────────────────────────────
    private void HandleLocalInput()
    {
        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");
        Vector2 inputDir = new(h, v);

        _rb.linearVelocity = inputDir.normalized * _moveSpeed;

        // [ANIMATION - LOCAL]
        // speed = do lon cua input: 0 khi dung, 1 khi di chuyen
        // Khong dung normalized vi diagonal van la magnitude 1 sau normalize
        float speed = Mathf.Clamp01(inputDir.magnitude);

        // Ap dung Animator NGAY, khong can cho server tra loi
        // → local player thay animation mua khong co do tre
        _anim.SetFloat(SpeedParam, speed);

        // Facing theo vi tri chuot
        Vector3 mouseWorld = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        Vector2 toMouse    = (Vector2)(mouseWorld - transform.position);
        float facing       = Mathf.Atan2(toMouse.y, toMouse.x) * Mathf.Rad2Deg;
        if (facing < 0f) facing += 360f;

        ApplyFacingVisual(facing);

        // Gui len server theo interval
        _sendTimer -= Time.deltaTime;
        if (_sendTimer > 0f) return;
        _sendTimer = _sendInterval;

        float px = transform.position.x;
        float py = transform.position.y;

        // Chi gui khi co thay doi (bao gom ca speed — anim co the thay doi khi dung lai)
        bool changed = px != _lastSentX || py != _lastSentY
                    || facing != _lastSentFacing || speed != _lastSentSpeed;
        if (!changed) return;

        // [ANIMATION SYNC - BUOC GUI]
        // Gui speed len server cung voi vi tri va facing.
        // Server se relay speed nay den tat ca remote clients.
        var mgr = GameRoomManager.Instance;
        if (mgr != null) mgr.SendMove(px, py, facing, speed);

        _lastSentX      = px;
        _lastSentY      = py;
        _lastSentFacing = facing;
        _lastSentSpeed  = speed;
    }

    // ── REMOTE PLAYER ──────────────────────────────────────────────

    // [ANIMATION SYNC - BUOC NHAN]
    // Ham nay duoc goi khi GameRoomManager nhan duoc message "playerMoved" tu server.
    // Server gui message nay cho tat ca client TRU nguoi gui.
    private void HandlePlayerMoved(PlayerMovedMsg msg)
    {
        // Loc: moi NetworkedCharacter lang nghe tat ca message,
        // nhung chi xu ly message cua player tuong ung voi minh
        if (msg.sessionId != _sessionId) return;

        _targetPos    = new Vector2(msg.x, msg.y);
        _targetFacing = msg.facing;

        // [ANIMATION SYNC - AP DUNG]
        // Nhan speed tu message, set Animator de chay dung animation.
        // Animator Controller tu quyet dinh: Speed > 0.1 → MOVE, else → IDLE
        _targetSpeed  = msg.speed;
        _anim.SetFloat(SpeedParam, _targetSpeed);
    }

    // Chay moi frame: lerp mo den vi tri target nhan tu server
    private void LerpRemotePosition()
    {
        transform.position = Vector2.Lerp(transform.position, _targetPos, Time.deltaTime * _lerpSpeed);
        ApplyFacingVisual(_targetFacing);
    }

    // ── Visual ─────────────────────────────────────────────────────
    private void ApplyFacingVisual(float facing)
    {
        Vector3 s = transform.localScale;
        s.x = (facing > 90f && facing < 270f) ? Mathf.Abs(s.x) : -Mathf.Abs(s.x);
        transform.localScale = s;
    }
}
