using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Image))] // Đảm bảo có Image component
public class SetSizeImage : MonoBehaviour
{
    public static SetSizeImage Instance;
    private float fixedWidth = 50f;
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }
    public void SetSizeSprite(Image image, Sprite sprite)
    {
        RectTransform rectTransform = image.GetComponent<RectTransform>();
        if (sprite == null)
        {
            Debug.LogWarning("Sprite is null");
            return;
        }

        image.sprite = sprite;

        float aspectRatio = sprite.rect.height / sprite.rect.width;

        rectTransform.sizeDelta = new Vector2(fixedWidth, fixedWidth * aspectRatio);

    }
}
