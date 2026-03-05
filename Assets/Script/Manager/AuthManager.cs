// AuthManager.cs - Unity Authentication Manager (Updated)
using System;
using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;

#region Data Models
[Serializable]
public class LoginRequest
{
    public string email;
    public string password;
}

[Serializable]
public class RegisterRequest
{
    public string username;
    public string email;
    public string password;
}

[Serializable]
public class RefreshTokenRequest
{
    public string refreshToken;
}

[Serializable]
public class UserData
{
    public int id;
    public string username;
    public string email;
    public string created_at;
    public string last_login;
    public int login_count;
    public bool is_active;
    public bool email_verified;
}

[Serializable]
public class AuthResponse
{
    public bool success;
    public string message;
    public AuthData data;
}

[Serializable]
public class AuthData
{
    public string accessToken;
    public string refreshToken;
    public UserData user;
}

[Serializable]
public class RefreshTokenResponse
{
    public bool success;
    public string message;
    public RefreshTokenData data;
}

[Serializable]
public class RefreshTokenData
{
    public string accessToken;
}

[Serializable]
public class UserResponse
{
    public bool success;
    public string message;
    public UserData data;
}

[Serializable]
public class ErrorResponse
{
    public bool success;
    public string message;
}
#endregion

public class AuthManager : MonoBehaviour
{
    #region Singleton
    private static AuthManager _instance;
    public static AuthManager Instance
    {
        get
        {
            if (_instance == null)
            {
                GameObject go = new GameObject("AuthManager");
                _instance = go.AddComponent<AuthManager>();
                DontDestroyOnLoad(go);
            }
            return _instance;
        }
    }

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }
        _instance = this;
        DontDestroyOnLoad(gameObject);
    }
    #endregion

    #region Configuration
    [Header("API Configuration")]
    [SerializeField] private string apiUrl = "http://localhost:3000";
    
    [Header("Scene Configuration")]
    [SerializeField] private string homePageScene = "HomePageScene";
    [SerializeField] private string loginScene = "LoginScene";

    [Header("Auto Refresh Settings")]
    [SerializeField] private float tokenCheckInterval = 300f; // 5 minutes
    [SerializeField] private float tokenExpirationTime = 840f; // 14 minutes (refresh before 1 min)
    #endregion

    #region Private Fields
    private string accessToken;
    private string refreshToken;
    public UserData currentUser;
    private bool isAuthenticated = false;
    private float tokenTimestamp;
    private Coroutine autoRefreshCoroutine;
    #endregion

    #region Public Properties
    public bool IsAuthenticated => isAuthenticated;
    public UserData CurrentUser => currentUser;
    public string AccessToken => accessToken;
    #endregion

    #region Events
    public event Action<UserData> OnLoginSuccess;
    public event Action<string> OnLoginFailed;
    public event Action<UserData> OnRegisterSuccess;
    public event Action<string> OnRegisterFailed;
    public event Action OnLogout;
    public event Action OnTokenRefreshed;
    #endregion

    #region Unity Lifecycle
    private void Start()
    {
        // Auto login khi start game
        // StartCoroutine(AutoLogin());
    }

    private void OnDestroy()
    {
        StopAutoRefresh();
    }
    #endregion

    #region 1. LOGIN
    /// <summary>
    /// Đăng nhập với email và password
    /// </summary>
    public void Login(string email, string password, Action<bool, string> callback = null)
    {
        StartCoroutine(LoginCoroutine(email, password, callback));
    }

    private IEnumerator LoginCoroutine(string email, string password, Action<bool, string> callback)
    {
        Debug.Log($"🔐 Attempting login for: {email}");

        LoginRequest loginData = new LoginRequest
        {
            email = email,
            password = password
        };

        string jsonData = JsonUtility.ToJson(loginData);
        byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonData);

        using (UnityWebRequest request = new UnityWebRequest($"{apiUrl}/api/auth/login", "POST"))
        {
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");

            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                string responseText = request.downloadHandler.text;
                AuthResponse response = JsonUtility.FromJson<AuthResponse>(responseText);

                if (response.success && response.data != null)
                {
                    SaveAuthData(response.data);
                    StartAutoRefresh();

                    Debug.Log($"✅ Login successful: {currentUser.username}");
                    OnLoginSuccess?.Invoke(currentUser);
                    callback?.Invoke(true, "Login successful");

                    // Load home page scene
                    SceneManager.LoadScene(homePageScene);
                }
                else
                {
                    Debug.LogError($"❌ Login failed: {response.message}");
                    OnLoginFailed?.Invoke(response.message);
                    callback?.Invoke(false, response.message);
                }
            }
            else
            {
                string errorMsg = $"Network error: {request.error}";
                Debug.LogError($"❌ {errorMsg}");
                OnLoginFailed?.Invoke(errorMsg);
                callback?.Invoke(false, errorMsg);
            }
        }
    }
    #endregion

    #region 2. REGISTER
    /// <summary>
    /// Đăng ký tài khoản mới
    /// </summary>
    public void Register(string username, string email, string password, Action<bool, string> callback = null)
    {
        StartCoroutine(RegisterCoroutine(username, email, password, callback));
    }

    private IEnumerator RegisterCoroutine(string username, string email, string password, Action<bool, string> callback)
    {
        Debug.Log($"📝 Attempting register for: {email}");

        RegisterRequest registerData = new RegisterRequest
        {
            username = username,
            email = email,
            password = password
        };

        string jsonData = JsonUtility.ToJson(registerData);
        byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonData);

        using (UnityWebRequest request = new UnityWebRequest($"{apiUrl}/api/auth/register", "POST"))
        {
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");

            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                string responseText = request.downloadHandler.text;
                AuthResponse response = JsonUtility.FromJson<AuthResponse>(responseText);

                if (response.success && response.data != null)
                {
                    SaveAuthData(response.data);
                    StartAutoRefresh();

                    Debug.Log($"✅ Register successful: {currentUser.username}");
                    OnRegisterSuccess?.Invoke(currentUser);
                    callback?.Invoke(true, "Register successful");

                    // Load home page scene
                    SceneManager.LoadScene(homePageScene);
                }
                else
                {
                    Debug.LogError($"❌ Register failed: {response.message}");
                    OnRegisterFailed?.Invoke(response.message);
                    callback?.Invoke(false, response.message);
                }
            }
            else
            {
                string errorMsg = $"Network error: {request.error}";
                Debug.LogError($"❌ {errorMsg}");
                OnRegisterFailed?.Invoke(errorMsg);
                callback?.Invoke(false, errorMsg);
            }
        }
    }
    #endregion

    #region 3. AUTO LOGIN
    /// <summary>
    /// Tự động đăng nhập khi mở game (kiểm tra saved tokens)
    /// </summary>
    private IEnumerator AutoLogin()
    {
        Debug.Log("🔄 Attempting auto login...");

        // Lấy tokens từ PlayerPrefs
        string savedAccessToken = PlayerPrefs.GetString("accessToken", "");
        string savedRefreshToken = PlayerPrefs.GetString("refreshToken", "");
        string savedUserJson = PlayerPrefs.GetString("user", "");
        float savedTimestamp = PlayerPrefs.GetFloat("tokenTimestamp", 0f);

        if (string.IsNullOrEmpty(savedAccessToken) || string.IsNullOrEmpty(savedRefreshToken))
        {
            Debug.Log("⚠️ No saved tokens found");
            yield break;
        }

        // Restore tokens
        accessToken = savedAccessToken;
        refreshToken = savedRefreshToken;
        tokenTimestamp = savedTimestamp;

        if (!string.IsNullOrEmpty(savedUserJson))
        {
            currentUser = JsonUtility.FromJson<UserData>(savedUserJson);
        }

        // Kiểm tra token có hết hạn chưa
        float tokenAge = Time.time - tokenTimestamp;

        if (tokenAge > tokenExpirationTime)
        {
            Debug.Log("⚠️ Access token expired, refreshing...");
            yield return RefreshAccessToken();

            if (!isAuthenticated)
            {
                Debug.LogError("❌ Failed to refresh token");
                ClearAuthData();
                yield break;
            }
        }

        // Verify token bằng cách gọi API /me
        yield return VerifyToken();

        if (isAuthenticated)
        {
            StartAutoRefresh();
            Debug.Log($"✅ Auto login successful: {currentUser.username}");
            OnLoginSuccess?.Invoke(currentUser);

            // Load home page scene
            if (SceneManager.GetActiveScene().name != homePageScene)
            {
                SceneManager.LoadScene(homePageScene);
            }
        }
        else
        {
            Debug.LogError("❌ Auto login failed");
            ClearAuthData();
        }
    }
    #endregion

    #region 4. DUY TRÌ PHIÊN ĐĂNG NHẬP (Auto Refresh)
    /// <summary>
    /// Bắt đầu auto refresh token
    /// </summary>
    private void StartAutoRefresh()
    {
        StopAutoRefresh();
        autoRefreshCoroutine = StartCoroutine(AutoRefreshCoroutine());
        Debug.Log("🔄 Auto refresh started");
    }

    /// <summary>
    /// Dừng auto refresh
    /// </summary>
    private void StopAutoRefresh()
    {
        if (autoRefreshCoroutine != null)
        {
            StopCoroutine(autoRefreshCoroutine);
            autoRefreshCoroutine = null;
        }
    }

    /// <summary>
    /// Coroutine tự động refresh token khi sắp hết hạn
    /// </summary>
    private IEnumerator AutoRefreshCoroutine()
    {
        while (isAuthenticated)
        {
            yield return new WaitForSeconds(tokenCheckInterval);

            float tokenAge = Time.time - tokenTimestamp;

            // Nếu token sắp hết hạn (còn < 1 phút)
            if (tokenAge > tokenExpirationTime)
            {
                Debug.Log("⏰ Token expiring soon, refreshing...");
                yield return RefreshAccessToken();

                if (!isAuthenticated)
                {
                    Debug.LogError("❌ Failed to refresh token, logging out...");
                    Logout();
                    yield break;
                }
            }
        }
    }

    /// <summary>
    /// Refresh access token bằng refresh token
    /// </summary>
    private IEnumerator RefreshAccessToken()
    {
        Debug.Log("🔄 Refreshing access token...");

        RefreshTokenRequest refreshData = new RefreshTokenRequest
        {
            refreshToken = this.refreshToken
        };

        string jsonData = JsonUtility.ToJson(refreshData);
        byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonData);

        using (UnityWebRequest request = new UnityWebRequest($"{apiUrl}/api/auth/refresh-token", "POST"))
        {
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");

            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                string responseText = request.downloadHandler.text;
                RefreshTokenResponse response = JsonUtility.FromJson<RefreshTokenResponse>(responseText);

                if (response.success && response.data != null)
                {
                    // Update access token
                    accessToken = response.data.accessToken;
                    tokenTimestamp = Time.time;

                    // Save to PlayerPrefs
                    PlayerPrefs.SetString("accessToken", accessToken);
                    PlayerPrefs.SetFloat("tokenTimestamp", tokenTimestamp);
                    PlayerPrefs.Save();

                    isAuthenticated = true;
                    OnTokenRefreshed?.Invoke();

                    Debug.Log("✅ Token refreshed successfully");
                }
                else
                {
                    Debug.LogError($"❌ Refresh token failed: {response.message}");
                    isAuthenticated = false;
                }
            }
            else
            {
                Debug.LogError($"❌ Refresh token network error: {request.error}");
                isAuthenticated = false;
            }
        }
    }
    #endregion

    #region Helper Methods
    /// <summary>
    /// Lưu auth data vào memory và PlayerPrefs
    /// </summary>
    private void SaveAuthData(AuthData data)
    {
        accessToken = data.accessToken;
        refreshToken = data.refreshToken;
        currentUser = data.user;
        isAuthenticated = true;
        tokenTimestamp = Time.time;

        // Save to PlayerPrefs
        PlayerPrefs.SetString("accessToken", accessToken);
        PlayerPrefs.SetString("refreshToken", refreshToken);
        PlayerPrefs.SetString("user", JsonUtility.ToJson(currentUser));
        PlayerPrefs.SetFloat("tokenTimestamp", tokenTimestamp);
        PlayerPrefs.Save();

        Debug.Log("💾 Auth data saved");
    }

    /// <summary>
    /// Xóa auth data
    /// </summary>
    private void ClearAuthData()
    {
        accessToken = null;
        refreshToken = null;
        currentUser = null;
        isAuthenticated = false;
        tokenTimestamp = 0f;

        PlayerPrefs.DeleteKey("accessToken");
        PlayerPrefs.DeleteKey("refreshToken");
        PlayerPrefs.DeleteKey("user");
        PlayerPrefs.DeleteKey("tokenTimestamp");
        PlayerPrefs.Save();

        Debug.Log("🗑️ Auth data cleared");
    }

    /// <summary>
    /// Verify token bằng cách gọi API /me
    /// </summary>
    private IEnumerator VerifyToken()
    {
        Debug.Log("🔍 Verifying token...");

        using (UnityWebRequest request = UnityWebRequest.Get($"{apiUrl}/api/auth/me"))
        {
            request.SetRequestHeader("Authorization", $"Bearer {accessToken}");

            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                string responseText = request.downloadHandler.text;
                UserResponse response = JsonUtility.FromJson<UserResponse>(responseText);

                if (response.success && response.data != null)
                {
                    currentUser = response.data;
                    isAuthenticated = true;
                    Debug.Log("✅ Token verified");
                }
                else
                {
                    isAuthenticated = false;
                    Debug.LogError("❌ Token verification failed");
                }
            }
            else
            {
                isAuthenticated = false;
                Debug.LogError($"❌ Token verification error: {request.error}");
            }
        }
    }

    /// <summary>
    /// Đăng xuất (chỉ clear local data, không gọi API)
    /// </summary>
    public void Logout()
    {
        StopAutoRefresh();
        ClearAuthData();
        OnLogout?.Invoke();

        Debug.Log("👋 Logged out");

        // Load login scene
        SceneManager.LoadScene(loginScene);
    }

    /// <summary>
    /// Gọi API với authentication header
    /// </summary>
    public UnityWebRequest CreateAuthenticatedRequest(string endpoint, string method = "GET")
    {
        UnityWebRequest request = new UnityWebRequest($"{apiUrl}{endpoint}", method);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Authorization", $"Bearer {accessToken}");
        return request;
    }
    #endregion

    #region Public Utility Methods
    /// <summary>
    /// Kiểm tra xem có đang đăng nhập không
    /// </summary>
    public bool CheckAuthentication()
    {
        return isAuthenticated && !string.IsNullOrEmpty(accessToken);
    }

    /// <summary>
    /// Lấy thông tin user hiện tại
    /// </summary>
    public UserData GetCurrentUser()
    {
        return currentUser;
    }

    /// <summary>
    /// Force refresh token ngay lập tức
    /// </summary>
    public void ForceRefreshToken(Action<bool> callback = null)
    {
        StartCoroutine(ForceRefreshCoroutine(callback));
    }

    private IEnumerator ForceRefreshCoroutine(Action<bool> callback)
    {
        yield return RefreshAccessToken();
        callback?.Invoke(isAuthenticated);
    }
    #endregion
}