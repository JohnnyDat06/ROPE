using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using UnityEngine.Video;
using UnityEngine.SceneManagement; // Bắt buộc phải có thư viện này để chuyển Scene

public class BossVideoCutscene : MonoBehaviour
{
    [Header("Liên kết Dữ liệu Boss")]
    public EnemyHealth bossHealth;

    [Header("Cài đặt Video Player")]
    [Tooltip("Thành phần Video Player dùng để phát video")]
    public VideoPlayer videoPlayer;
    [Tooltip("Màn hình UI để hiển thị video")]
    public RawImage videoScreen;

    [Header("Cutscene 60% HP")]
    public VideoClip video60Percent;
    public UnityEvent on60PercentStart;
    public UnityEvent on60PercentEnd;
    private bool _hasPlayed60Percent = false;

    [Header("Cutscene Khi Boss Chết")]
    public VideoClip videoDeath;
    [Tooltip("Thời gian chờ (giây) sau khi Boss chết mới hiện Video")]
    public float delayBeforeDeathVideo = 5f;
    public UnityEvent onDeathStart;
    public UnityEvent onDeathEnd;

    private bool _isDeathVideo = false;

    private void OnEnable()
    {
        if (bossHealth != null)
        {
            bossHealth.OnTakeDamage += CheckHealthForCutscene;
            bossHealth.OnDeath += HandleBossDeath;
        }

        if (videoPlayer != null)
        {
           
            videoPlayer.loopPointReached += OnVideoFinished;
        }
    }

    private void OnDisable()
    {
        if (bossHealth != null)
        {
            bossHealth.OnTakeDamage -= CheckHealthForCutscene;
            bossHealth.OnDeath -= HandleBossDeath;
        }

        if (videoPlayer != null)
        {
            videoPlayer.loopPointReached -= OnVideoFinished;
        }
    }

    private void CheckHealthForCutscene(int damage)
    {
        if (_hasPlayed60Percent || _isDeathVideo || bossHealth.curentHealth <= 0) return;

        float hpPercentage = (float)bossHealth.curentHealth / bossHealth.maxHealth;

        if (hpPercentage <= 0.6f)
        {
            _hasPlayed60Percent = true;

            Time.timeScale = 0f;
            PlayVideo(video60Percent);
            on60PercentStart?.Invoke();
        }
    }

    private void HandleBossDeath(Vector3 deathPosition)
    {
        _isDeathVideo = true;
        StartCoroutine(WaitAndPlayDeathVideo());
    }

    private IEnumerator WaitAndPlayDeathVideo()
    {
        Debug.Log($"Boss đã chết! Chờ {delayBeforeDeathVideo} giây trước khi chiếu Cutscene...");
        yield return new WaitForSecondsRealtime(delayBeforeDeathVideo);

        Time.timeScale = 0f;
        PlayVideo(videoDeath);
        onDeathStart?.Invoke();
    }

    private void PlayVideo(VideoClip clip)
    {
        if (videoPlayer == null || clip == null) return;

        videoScreen.gameObject.SetActive(true);
        videoPlayer.clip = clip;
        videoPlayer.Play();
    }

    private void OnVideoFinished(VideoPlayer vp)
    {
        videoScreen.gameObject.SetActive(false);

        if (!_isDeathVideo)
        {
          
            Time.timeScale = 1f;
            on60PercentEnd?.Invoke();
            Debug.Log("Kết thúc Cutscene 60%, Game tiếp tục.");
        }
        else
        {
            
            onDeathEnd?.Invoke();
            Debug.Log("Video kết thúc. Đang chuyển về MainMenu...");      
            Time.timeScale = 1f;
            SceneManager.LoadScene("MainMenu");
        }
    }
}