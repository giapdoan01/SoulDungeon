using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class LoginPanel : MonoBehaviour
{
    [Header("Input Fields")]
    public TMP_InputField emailInput;
    public TMP_InputField passwordInput;

    [Header("Buttons")]
    public Button loginButton;
    public Button signupButton;

    [Header("UI Elements")]
    public TextMeshProUGUI errorText;
    public GameObject loadingPanel; 

    [Header("Panel References")]
    public GameObject signUpPanel; 

    private void Start()
    {
        loginButton.onClick.AddListener(OnLoginButtonClicked);
        signupButton.onClick.AddListener(OnSignupButtonClicked);

        if (errorText != null)
            errorText.gameObject.SetActive(false);

        if (loadingPanel != null)
            loadingPanel.SetActive(false);

        AuthManager.Instance.OnLoginSuccess += OnLoginSuccess;
        AuthManager.Instance.OnLoginFailed += OnLoginFailed;
    }

    private void OnDestroy()
    {
        if (AuthManager.Instance != null)
        {
            AuthManager.Instance.OnLoginSuccess -= OnLoginSuccess;
            AuthManager.Instance.OnLoginFailed -= OnLoginFailed;
        }
    }

    private void OnLoginButtonClicked()
    {
        string email = emailInput.text.Trim();
        string password = passwordInput.text;

        if (string.IsNullOrEmpty(email))
        {
            ShowError("Email không được để trống");
            return;
        }

        if (string.IsNullOrEmpty(password))
        {
            ShowError("Password không được để trống");
            return;
        }

        if (!IsValidEmail(email))
        {
            ShowError("Email không hợp lệ");
            return;
        }

        if (password.Length < 6)
        {
            ShowError("Password phải có ít nhất 6 ký tự");
            return;
        }

        ShowLoading(true);
        HideError();

        AuthManager.Instance.Login(email, password);
    }

    private void OnSignupButtonClicked()
    {
        gameObject.SetActive(false);
        
        if (signUpPanel != null)
        {
            signUpPanel.SetActive(true);
        }
        
        Debug.Log("Switched to SignUp Panel");
    }

    private void OnLoginSuccess(UserData user)
    {
        ShowLoading(false);
        Debug.Log($"Login successful! Welcome {user.username}");
    }

    private void OnLoginFailed(string errorMessage)
    {
        ShowLoading(false);
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

    private void ShowLoading(bool show)
    {
        if (loadingPanel != null)
        {
            loadingPanel.SetActive(show);
        }

        // Disable inputs during loading
        emailInput.interactable = !show;
        passwordInput.interactable = !show;
        loginButton.interactable = !show;
        signupButton.interactable = !show;
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

    //Public method để SignUpPanel có thể gọi
    public void ShowPanel()
    {
        gameObject.SetActive(true);
        HideError();
        ClearInputs();
    }

    private void ClearInputs()
    {
        emailInput.text = "";
        passwordInput.text = "";
    }
}