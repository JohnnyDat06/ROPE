using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;
using System.Linq;

public class MenuManager : MonoBehaviour
{
    public static MenuManager Instance { get; private set; }

    [Header("Setting Scene")]
    public string[] gameSceneNames;
    public string mainMenuSceneName;
    
    public GameObject pauseMenuPanel;

    [Header("Game UI (ẩn khi pause)")]
    [Tooltip("Các UI gameplay cần ẩn khi pause (crosshair, HUD, ...)")]
    public GameObject[] gameUIElements;

    private bool isPaused = false;
    private PlayerInput playerInput;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            //DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // Reset cached PlayerInput khi chuyển scene
        playerInput = null;

        // Nếu scene vừa load là game scene → khoá chuột
        if (gameSceneNames != null && gameSceneNames.Contains(scene.name))
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
        else
        {
            // Main menu hoặc scene khác → mở khoá chuột
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }

    private void Start()
    {
        if (pauseMenuPanel != null)
        {
            pauseMenuPanel.SetActive(false);
        }
    }

    private void Update()
    {
        string activeScene = SceneManager.GetActiveScene().name;
        if (gameSceneNames != null && gameSceneNames.Contains(activeScene))
        {
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                if (isPaused)
                {
                    ResumeGame();
                }
                else
                {
                    PauseGame();
                }
            }
        }
    }

    /// <summary>
    /// Tìm PlayerInput trên Player nếu chưa có reference
    /// </summary>
    private PlayerInput FindPlayerInput()
    {
        if (playerInput == null)
        {
            playerInput = FindAnyObjectByType<PlayerInput>();
        }
        return playerInput;
    }
    
    public void PlayGame(int sceneIndex = 0)
    {
        if (gameSceneNames == null || gameSceneNames.Length == 0)
        {
            Debug.LogError("gameSceneNames rỗng!");
            return;
        }

        sceneIndex = Mathf.Clamp(sceneIndex, 0, gameSceneNames.Length - 1);
        Time.timeScale = 1f;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        SceneManager.LoadScene(gameSceneNames[sceneIndex]);
    }


    public void PlayGame(string sceneName)
    {
        if (gameSceneNames != null && gameSceneNames.Contains(sceneName))
        {
            Time.timeScale = 1f;
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
            SceneManager.LoadScene(sceneName);
        }
        else
        {
            Debug.LogError($"Scene '{sceneName}' không có trong danh sách gameSceneNames!");
        }
    }

    public void QuitGame()
    {
        Debug.Log("Đang thoát game...");
        Application.Quit();
    }
    

    private void PauseGame()
    {
        if (pauseMenuPanel != null)
        {
            pauseMenuPanel.SetActive(true);
        }

        // Dừng thời gian
        Time.timeScale = 0f;
        isPaused = true;

        // Tắt input của player → camera không di chuyển, player không điều khiển được
        PlayerInput pi = FindPlayerInput();
        if (pi != null)
        {
            pi.enabled = false;
        }

        // Ẩn các UI gameplay (crosshair, HUD, ...)
        SetGameUIActive(false);

        // Mở khoá chuột để thao tác UI
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void ResumeGame()
    {
        if (pauseMenuPanel != null)
        {
            pauseMenuPanel.SetActive(false);
        }

        // Trả lại thời gian
        Time.timeScale = 1f;
        isPaused = false;

        // Bật lại input của player
        PlayerInput pi = FindPlayerInput();
        if (pi != null)
        {
            pi.enabled = true;
        }

        // Hiện lại các UI gameplay
        SetGameUIActive(true);

        // Khoá chuột lại như lúc chơi
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    public void LoadMainMenu()
    {
        Time.timeScale = 1f;
        isPaused = false;
        
        if (pauseMenuPanel != null) 
        {
            pauseMenuPanel.SetActive(false);
        }

        // Mở khoá chuột cho main menu
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        
        SceneManager.LoadScene(mainMenuSceneName);
    }

    /// <summary>
    /// Bật / tắt tất cả UI gameplay (crosshair, ammo, health bar, ...)
    /// </summary>
    private void SetGameUIActive(bool active)
    {
        if (gameUIElements == null) return;
        foreach (GameObject uiElement in gameUIElements)
        {
            if (uiElement != null)
            {
                uiElement.SetActive(active);
            }
        }
    }
}