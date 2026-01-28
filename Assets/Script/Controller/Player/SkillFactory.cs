using UnityEngine;
using UnityEngine.InputSystem;

public class SkillFactory : MonoBehaviour
{
    [Header("Skill Prefabs")]
    [SerializeField] private GameObject skillQPrefab;
    [SerializeField] private GameObject skillRCPrefab;  // ← THÊM MỚI
    
    [Header("Spawn Settings")]
    [SerializeField] private Transform skillSpawnPoint;
    
    [Header("Settings")]
    [SerializeField] private Camera mainCamera;
    [SerializeField] private float qCooldown = 1f;
    [SerializeField] private float rcCooldown = 3f;  // ← THÊM MỚI
    
    private Transform owner;
    private float qTimer = 0f;
    private float rcTimer = 0f;  // ← THÊM MỚI

    private void Awake()
    {
        owner = transform;
        
        if (mainCamera == null)
            mainCamera = Camera.main;
            
        if (skillSpawnPoint == null)
            skillSpawnPoint = owner;
    }

    private void Update()
    {
        // Cooldown timers
        if (qTimer > 0) 
            qTimer -= Time.deltaTime;
        
        if (rcTimer > 0)  // ← THÊM MỚI
            rcTimer -= Time.deltaTime;

        // Q Key
        if (Keyboard.current.qKey.wasPressedThisFrame && qTimer <= 0)
        {
            CastSkillQ();
        }
        
        // Right Click  ← THÊM MỚI
        if (Mouse.current.rightButton.wasPressedThisFrame && rcTimer <= 0)
        {
            CastSkillRC();
        }
    }

    public void CastSkillQ()
    {
        if (skillQPrefab == null)
        {
            Debug.LogError($"{owner.name} chưa gán Skill Q Prefab!");
            return;
        }

        Vector2 direction = GetDirectionToCursor();
        bool facingRight = owner.localScale.x > 0;

        GameObject skillObj = Instantiate(skillQPrefab, skillSpawnPoint.position, Quaternion.identity);
        
        InitializeSkill(skillObj, direction, facingRight);

        qTimer = qCooldown;
    }
    
    // ← THÊM MỚI: Cast Skill RC
    public void CastSkillRC()
    {
        if (skillRCPrefab == null)
        {
            Debug.LogError($"{owner.name} chưa gán Skill RC Prefab!");
            return;
        }

        Vector2 direction = GetDirectionToCursor();
        bool facingRight = owner.localScale.x > 0;

        // Spawn tại vị trí spawn point
        GameObject skillObj = Instantiate(skillRCPrefab, skillSpawnPoint.position, Quaternion.identity);
        
        InitializeSkill(skillObj, direction, facingRight);

        rcTimer = rcCooldown;
        
        Debug.Log("Right Click Skill Cast!");
    }
    
    // ← REFACTOR: Tách logic Initialize chung
    private void InitializeSkill(GameObject skillObj, Vector2 direction, bool facingRight)
    {
        MonoBehaviour[] components = skillObj.GetComponents<MonoBehaviour>();
        foreach (var component in components)
        {
            var initMethod = component.GetType().GetMethod("Initialize", 
                new[] { typeof(Vector2), typeof(bool) });
            
            if (initMethod != null)
            {
                initMethod.Invoke(component, new object[] { direction, facingRight });
                break;
            }
        }
    }

    private Vector2 GetDirectionToCursor()
    {
        Vector2 mousePos = Mouse.current.position.ReadValue();
        Vector3 worldPos = mainCamera.ScreenToWorldPoint(mousePos);
        worldPos.z = 0;
        
        return (worldPos - owner.position).normalized;
    }
}
