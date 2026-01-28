using UnityEngine;

public class GokuSkillRC : MonoBehaviour
{
    [Header("Skill Settings")]
    [SerializeField] private float duration = 2f; // Thời gian tồn tại
    
    [Header("Visual Settings")]
    [SerializeField] private ParticleSystem skillParticle;
    [SerializeField] private SpriteRenderer spriteRenderer;
    
    private float timer = 0f;

    public void Initialize(Vector2 direction, bool playerFacingRight)
    {
        // Xoay skill về hướng chuột (để visual đẹp)
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0, 0, angle);
        
        // Xoay particle nếu có
        if (skillParticle != null)
        {
            skillParticle.transform.rotation = transform.rotation;
            skillParticle.Play(); // Bắt đầu phát particle
        }
        
        // Flip sprite nếu bắn sang trái
        if (direction.x < 0 && spriteRenderer != null)
        {
            Vector3 scale = transform.localScale;
            scale.y *= -1;
            transform.localScale = scale;
        }
        
        Debug.Log($"Goku Skill RC spawned! Duration: {duration}s, Angle: {angle}°");
    }

    private void Update()
    {
        timer += Time.deltaTime;
        
        // Hết thời gian → tự hủy
        if (timer >= duration)
        {
            Destroy(gameObject);
        }
    }
    
    // Optional: Fade out effect trước khi mất
    private void OnDestroy()
    {
        Debug.Log("Skill RC destroyed!");
    }
}
