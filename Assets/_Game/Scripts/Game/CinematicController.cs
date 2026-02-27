using UnityEngine;
using UnityEngine.Video;
using UnityEngine.SceneManagement;

public class CinematicController : MonoBehaviour
{
    [Header("Cài đặt Video")]
    public VideoPlayer videoPlayer;
    
    [Header("Scene tiếp theo")]
    [Tooltip("Nhập tên Scene bạn muốn load sau khi xem xong video")]
    public string nextSceneName = "MainMenu"; 

    void Start()
    {
        // Kiểm tra xem đã gán VideoPlayer chưa
        if (videoPlayer == null)
        {
            videoPlayer = GetComponent<VideoPlayer>();
        }

        // Đăng ký sự kiện: Gọi hàm OnVideoEnd khi video chạy đến khung hình cuối cùng
        videoPlayer.loopPointReached += OnVideoEnd;
    }

    void Update()
    {
        // Cho phép người chơi nhấn Space hoặc Escape để bỏ qua video
        if (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Escape))
        {
            LoadNextScene();
        }
    }

    // Hàm này tự động được gọi khi video kết thúc
    void OnVideoEnd(VideoPlayer vp)
    {
        LoadNextScene();
    }

    void LoadNextScene()
    {
        // Chuyển scene
        SceneManager.LoadScene(nextSceneName);
    }
}