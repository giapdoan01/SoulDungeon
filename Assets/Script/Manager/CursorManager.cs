using UnityEngine;
using UnityEngine.InputSystem;

public class CustomCursor : MonoBehaviour
{
    [Header("Cursor Settings")]
    [SerializeField] private SpriteRenderer cursorRenderer;
    [SerializeField] private float cursorScale = 1f; // Điều chỉnh kích thước tùy ý!
    
    [Header("Cursor Sprites")]
    [SerializeField] private Sprite normalCursor;
    [SerializeField] private Sprite hoverCursor;
    [SerializeField] private Sprite clickCursor;
    
    [Header("Offset")]
    [SerializeField] private Vector2 cursorOffset = Vector2.zero; // Điều chỉnh vị trí hotspot

    private Camera mainCamera;

    private void Awake()
    {
        mainCamera = Camera.main;
        
        // Ẩn cursor hệ thống
        Cursor.visible = false;
        
        // Setup sprite renderer
        if (cursorRenderer == null)
        {
            cursorRenderer = GetComponent<SpriteRenderer>();
        }
        
        if (cursorRenderer != null)
        {
            cursorRenderer.sprite = normalCursor;
            cursorRenderer.sortingOrder = 1000; // Render trên cùng
            transform.localScale = Vector3.one * cursorScale;
        }
    }

    private void Update()
    {
        // Follow chuột
        Vector2 mousePos = Mouse.current.position.ReadValue();
        Vector3 worldPos = mainCamera.ScreenToWorldPoint(mousePos);
        worldPos.z = 0;
        
        // Apply offset
        worldPos += (Vector3)cursorOffset;
        
        transform.position = worldPos;
    }

    // Đổi sprite cursor
    public void SetNormalCursor()
    {
        if (cursorRenderer != null && normalCursor != null)
        {
            cursorRenderer.sprite = normalCursor;
        }
    }

    public void SetHoverCursor()
    {
        if (cursorRenderer != null && hoverCursor != null)
        {
            cursorRenderer.sprite = hoverCursor;
        }
    }

    public void SetClickCursor()
    {
        if (cursorRenderer != null && clickCursor != null)
        {
            cursorRenderer.sprite = clickCursor;
        }
    }

    // Thay đổi kích thước runtime
    public void SetCursorScale(float scale)
    {
        cursorScale = scale;
        transform.localScale = Vector3.one * cursorScale;
    }

    private void OnDisable()
    {
        // Hiện lại cursor hệ thống khi tắt
        Cursor.visible = true;
    }
}
