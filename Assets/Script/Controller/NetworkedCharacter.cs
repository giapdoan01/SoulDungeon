// NetworkedCharacter.cs
// Gắn vào mỗi character trong GameScene.
// - Local player  : đọc input → gửi move lên server, áp dụng locally ngay
// - Remote player : đọc state từ GameRoomManager.State và nội suy vị trí
//
// Cách dùng:
//   1. Spawn character → gọi Init(sessionId, isLocalPlayer)
//   2. Script tự xử lý còn lại
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class NetworkedCharacter : MonoBehaviour
{
    // ── Config ─────────────────────────────────────────────────────
    [Header("Movement")]
    [SerializeField] private float _moveSpeed    = 5f;
    [SerializeField] private float _lerpSpeed    = 15f;   // nội suy remote player
    [SerializeField] private float _sendInterval = 0.05f; // 20Hz

    // ── State ──────────────────────────────────────────────────────
    private string _sessionId;
    private bool   _isLocalPlayer;

    private Rigidbody2D _rb;

    // Local: tracking gửi
    private float _sendTimer;
    private float _lastSentX, _lastSentY, _lastSentFacing;

    // Remote: target position nhận từ server
    private Vector2 _targetPos;
    private float   _targetFacing;

    // ── Init ───────────────────────────────────────────────────────
    public void Init(string sessionId, bool isLocalPlayer)
    {
        _sessionId     = sessionId;
        _isLocalPlayer = isLocalPlayer;
        _rb            = GetComponent<Rigidbody2D>();

        if (isLocalPlayer)
        {
            // Gửi characterIndex ngay khi vào (dùng index mặc định 0; thay bằng selection UI sau)
            GameRoomManager.Instance?.SendSetCharacter(0);
        }
        else
        {
            // Remote: đọc vị trí ban đầu từ state
            var state = GameRoomManager.Instance?.State;
            if (state != null && state.players.ContainsKey(_sessionId))
            {
                var p = state.players[_sessionId];
                transform.position = new Vector3(p.x, p.y, 0f);
                _targetPos    = transform.position;
                _targetFacing = p.facing;
            }
        }
    }

    // ── Update ─────────────────────────────────────────────────────
    private void Update()
    {
        if (_isLocalPlayer)
            HandleLocalMovement();
        else
            HandleRemoteMovement();
    }

    // ── Local Player ───────────────────────────────────────────────
    private void HandleLocalMovement()
    {
        // Đọc input WASD / Arrow
        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");
        Vector2 dir = new Vector2(h, v).normalized;

        _rb.velocity = dir * _moveSpeed;

        // Tính facing (0 = phải, 180 = trái)
        float facing = _lastSentFacing;
        if (h > 0)       facing = 0f;
        else if (h < 0)  facing = 180f;
        else if (v > 0)  facing = 90f;
        else if (v < 0)  facing = 270f;

        // Flip sprite theo facing
        ApplyFacingVisual(facing);

        // Gửi state lên server theo interval
        _sendTimer -= Time.deltaTime;
        if (_sendTimer <= 0f)
        {
            _sendTimer = _sendInterval;
            float px = transform.position.x;
            float py = transform.position.y;

            // Chỉ gửi khi có thay đổi
            if (px != _lastSentX || py != _lastSentY || facing != _lastSentFacing)
            {
                GameRoomManager.Instance?.SendMove(px, py, facing);
                _lastSentX      = px;
                _lastSentY      = py;
                _lastSentFacing = facing;
            }
        }
    }

    // ── Remote Player ──────────────────────────────────────────────
    private void HandleRemoteMovement()
    {
        // Poll state mỗi frame (state patch đến qua Colyseus schema sync)
        var state = GameRoomManager.Instance?.State;
        if (state == null || !state.players.ContainsKey(_sessionId)) return;

        var p = state.players[_sessionId];
        _targetPos    = new Vector2(p.x, p.y);
        _targetFacing = p.facing;

        // Nội suy mượt đến vị trí target
        transform.position = Vector2.Lerp(transform.position, _targetPos, Time.deltaTime * _lerpSpeed);

        // Flip sprite
        ApplyFacingVisual(_targetFacing);
    }

    // ── Visual ─────────────────────────────────────────────────────
    private void ApplyFacingVisual(float facing)
    {
        // facing 0 = phải → scale.x dương; 180 = trái → scale.x âm
        Vector3 s = transform.localScale;
        s.x = (facing > 90f && facing < 270f) ? -Mathf.Abs(s.x) : Mathf.Abs(s.x);
        transform.localScale = s;
    }

    private void OnDestroy()
    {
        if (_isLocalPlayer && _rb != null)
            _rb.velocity = Vector2.zero;
    }
}
