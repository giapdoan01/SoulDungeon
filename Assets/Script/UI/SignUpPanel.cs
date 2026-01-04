using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SignUpPanel : MonoBehaviour
{
    [Header("Input Fields")]
    public TMP_InputField emailInput;
    public TMP_InputField usernameInput;
    public TMP_InputField passwordInput;

    [Header("Buttons")]
    public Button signupButton;
    public Button loginButton;

    [Header("UI Elements")]
    public TextMeshProUGUI errorText;
    // ❌ KHÔNG CÓ loading panel

    [Header("Panel References")]
    public GameObject loginPanel; // ✅ Reference đến Login Panel

    private void Start()
    {
        // Setup button listeners
        signupButton.onClick.AddListener(OnSignupButtonClicked);
        loginButton.onClick.AddListener(OnLoginButtonClicked);

        // Hide error text
        if (errorText != null)
            errorText.gameObject.SetActive(false);

        // Subscribe to auth events
        AuthManager.Instance.OnRegisterSuccess += OnRegisterSuccess;
        AuthManager.Instance.OnRegisterFailed += OnRegisterFailed;
    }

    private void OnDestroy()
    {
        // Unsubscribe from events
        if (AuthManager.Instance != null)
        {
            AuthManager.Instance.OnRegisterSuccess -= OnRegisterSuccess;
            AuthManager.Instance.OnRegisterFailed -= OnRegisterFailed;
        }
    }

    private void OnSignupButtonClicked()
    {
        string email = emailInput.text.Trim();
        string username = usernameInput.text.Trim();
        string password = passwordInput.text;

        // Validation
        if (string.IsNullOrEmpty(email))
        {
            ShowError("Email không được để trống");
            return;
        }

        if (!IsValidEmail(email))
        {
            ShowError("Email không hợp lệ");
            return;
        }

        if (string.IsNullOrEmpty(username))
        {
            ShowError("Username không được để trống");
            return;
        }

        if (username.Length < 3 || username.Length > 30)
        {
            ShowError("Username phải có 3-30 ký tự");
            return;
        }

        if (string.IsNullOrEmpty(password))
        {
            ShowError("Password không được để trống");
            return;
        }

        if (password.Length < 6)
        {
            ShowError("Password phải có ít nhất 6 ký tự");
            return;
        }

        // ✅ KHÔNG CÓ loading panel, gọi register trực tiếp
        HideError();

        // Disable inputs during register
        SetInputsInteractable(false);

        // Call register
        AuthManager.Instance.Register(username, email, password);
    }

    private void OnLoginButtonClicked()
    {
        // ✅ Chuyển sang Login Panel
        gameObject.SetActive(false);
        
        if (loginPanel != null)
        {
            loginPanel.SetActive(true);
        }
        
        Debug.Log("Switched to Login Panel");
    }

    private void OnRegisterSuccess(UserData user)
    {
        Debug.Log($"Register successful! Welcome {user.username}");
        
        // ✅ Đăng ký thành công → Chuyển sang Login Panel
        gameObject.SetActive(false);
        
        if (loginPanel != null)
        {
            loginPanel.SetActive(true);
        }
        
        // Note: AuthManager sẽ tự động load HomePage Scene sau khi register
        // Nếu muốn user phải login lại thì comment dòng trên
    }

    private void OnRegisterFailed(string errorMessage)
    {
        SetInputsInteractable(true);
        ShowError(errorMessage);
    }

    private void ShowError(string message)
    {
        if (errorText != null)
        {
            errorText.text = message;
            errorText.gameObject.SetActive(true);
        }
    }

    private void HideError()
    {
        if (errorText != null)
        {
            errorText.gameObject.SetActive(false);
        }
    }

    private void SetInputsInteractable(bool interactable)
    {
        emailInput.interactable = interactable;
        usernameInput.interactable = interactable;
        passwordInput.interactable = interactable;
        signupButton.interactable = interactable;
        loginButton.interactable = interactable;
    }

    private bool IsValidEmail(string email)
    {
        try
        {
            var addr = new System.Net.Mail.MailAddress(email);
            return addr.Address == email;
        }
        catch
        {
            return false;
        }
    }

    // Public method để LoginPanel có thể gọi
    public void ShowPanel()
    {
        gameObject.SetActive(true);
        HideError();
        ClearInputs();
    }

    private void ClearInputs()
    {
        emailInput.text = "";
        usernameInput.text = "";
        passwordInput.text = "";
    }
}