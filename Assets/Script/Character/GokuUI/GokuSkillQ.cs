using UnityEngine;

public class GokuSkillQ : MonoBehaviour
{
    [Header("Goku Skill Settings")]
    [SerializeField] private float speed = 15f;
    [SerializeField] private float maxDistance = 5f;
    
    [Header("Particle Reference")]
    [SerializeField] private ParticleSystem skillParticle;
    
    private Vector3 startPosition;

    public void Initialize(Vector2 direction, bool playerFacingRight)
    {
        startPosition = transform.position;

        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0, 0, angle);

        if (skillParticle != null)
        {
            skillParticle.transform.rotation = transform.rotation;
        }

        if (direction.x < 0)
        {
            Vector3 scale = transform.localScale;
            scale.y *= -1;
            transform.localScale = scale;
        }
        
        Debug.Log($"Goku Skill Q! Direction: {direction}, Angle: {angle}°");
    }

    private void Update()
    {
        transform.Translate(Vector2.right * speed * Time.deltaTime, Space.Self);

        if (Vector3.Distance(startPosition, transform.position) >= maxDistance)
        {
            Destroy(gameObject);
        }
    }
}
