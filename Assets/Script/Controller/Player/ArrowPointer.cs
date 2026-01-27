using UnityEngine;
using UnityEngine.InputSystem;

public class ArrowPointer : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform centerPoint; // Player transform
    [SerializeField] private Transform arrowTransform; // Arrow GameObject
    
    [Header("Settings")]
    [SerializeField] private float orbitRadius = 2f; // Khoảng cách quay quanh

    private Camera mainCamera;
    private Vector3 originalArrowScale; // Lưu scale gốc

    private void Awake()
    {
        mainCamera = Camera.main;
        
        if (arrowTransform != null)
        {
            originalArrowScale = arrowTransform.localScale;
        }
    }

    private void Update()
    {
        if (centerPoint == null || arrowTransform == null) return;

        // Lấy vị trí chuột trong world space
        Vector2 mouseWorldPos = GetMouseWorldPosition();

        // Tính hướng từ Player đến chuột
        Vector2 direction = (mouseWorldPos - (Vector2)centerPoint.position).normalized;

        // Tính góc xoay
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;

        // Vị trí arrow: Player + hướng * bán kính
        Vector3 arrowPosition = (Vector2)centerPoint.position + direction * orbitRadius;
        arrowPosition.z = centerPoint.position.z;

        // Cập nhật vị trí
        arrowTransform.position = arrowPosition;
        
        // Cập nhật xoay (WORLD ROTATION, không bị ảnh hưởng bởi parent)
        arrowTransform.rotation = Quaternion.Euler(0, 0, angle);
        
        // Fix scale nếu Arrow là child của Player bị flip
        FixArrowScale();
    }

    private void FixArrowScale()
    {
        // Nếu Arrow là child của Player, cần fix scale khi Player flip
        if (arrowTransform.parent == centerPoint)
        {
            Vector3 fixedScale = originalArrowScale;
            
            // Nếu Player bị flip (scale.x âm), đảo ngược scale của Arrow
            if (centerPoint.localScale.x < 0)
            {
                fixedScale.x = -originalArrowScale.x;
            }
            
            arrowTransform.localScale = fixedScale;
        }
    }

    private Vector2 GetMouseWorldPosition()
    {
        Vector2 mouseScreenPos = Mouse.current.position.ReadValue();
        Vector3 worldPos = mainCamera.ScreenToWorldPoint(mouseScreenPos);
        worldPos.z = 0;
        return worldPos;
    }

    // Debug
    private void OnDrawGizmos()
    {
        if (centerPoint == null) return;

        Gizmos.color = Color.yellow;
        DrawCircle(centerPoint.position, orbitRadius, 50);

        if (Application.isPlaying && arrowTransform != null)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(centerPoint.position, arrowTransform.position);
            Gizmos.DrawWireSphere(arrowTransform.position, 0.2f);
        }
    }

    private void DrawCircle(Vector3 center, float radius, int segments)
    {
        float angleStep = 360f / segments;
        Vector3 prevPoint = center + new Vector3(radius, 0, 0);

        for (int i = 1; i <= segments; i++)
        {
            float angle = i * angleStep * Mathf.Deg2Rad;
            Vector3 newPoint = center + new Vector3(Mathf.Cos(angle) * radius, Mathf.Sin(angle) * radius, 0);
            Gizmos.DrawLine(prevPoint, newPoint);
            prevPoint = newPoint;
        }
    }
}
