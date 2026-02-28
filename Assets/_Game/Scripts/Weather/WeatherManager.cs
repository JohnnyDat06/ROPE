using UnityEngine;
using System.Collections;

public class WeatherManager : MonoBehaviour
{
    [Header("--- Rain Settings ---")]
    public ParticleSystem rainParticleSystem;
    public Transform targetToFollow; // Player
    [Range(0f, 1f)] public float rainChance = 0.5f;
    public float rainHeightOffset = 15f;

    [Header("--- Lightning Settings (Sét Môi Trường) ---")]
    public GameObject lightningPrefab;
    public LayerMask obstacleLayer;
    public float randomStrikeRadius = 20f;

    [Tooltip("Thời gian giữa các lần kiểm tra sét đánh (Giây)")]
    public float lightningInterval = 8.0f;

    [Tooltip("Tỷ lệ sét đánh ra môi trường (0.3 = 30% cơ hội mỗi nhịp). Giảm xuống để sét thưa hơn.")]
    [Range(0f, 1f)] public float environmentStrikeChance = 0.3f;

    [Tooltip("Bán kính sát thương của sét môi trường (Xui xẻo đứng gần là chết)")]
    public float environmentStrikeDamageRadius = 2.0f;

    [Header("--- Lightning Warning Settings (Cảnh báo Sét) ---")]
    [Tooltip("Số lần xẹt điện TỐI THIỂU trước khi đánh")]
    public int minWarningTimes = 5;

    [Tooltip("Số lần xẹt điện TỐI ĐA trước khi đánh")]
    public int maxWarningTimes = 10;

    [Tooltip("Khoảng cách thời gian giữa mỗi tiếng xẹt (giây)")]
    public float warningInterval = 0.4f;

    [Header("--- 3D Audio Settings (Nguồn Âm Thanh) ---")]
    public AudioSource rainAudioSource;
    public AudioClip warningElectricSound;
    public AudioClip thunderStrikeSound;

    [Header("--- TÙY CHỈNH ÂM LƯỢNG ---")]
    [Range(0f, 1f)] public float rainVolume = 1.0f;
    [Range(0f, 1f)] public float warningVolume = 1.0f;
    [Range(0f, 1f)] public float thunderVolume = 1.0f;
    [Tooltip("Khoảng cách giữ 100% âm lượng sấm (Tăng lên để nghe sấm to hơn khi ở xa)")]
    public float soundMinDistance = 30f;

    [Header("--- Player Status ---")]
    [Range(0f, 1f)] public float currentStrikeChance = 0f;

    private bool _isRaining = false;
    private Coroutine _lightningRoutine;

    void Start()
    {
        if (rainAudioSource != null)
        {
            rainAudioSource.spatialBlend = 0.8f;
            rainAudioSource.dopplerLevel = 0f;
            rainAudioSource.rolloffMode = AudioRolloffMode.Linear;
            rainAudioSource.minDistance = soundMinDistance;
            rainAudioSource.maxDistance = 150f;
            rainAudioSource.volume = rainVolume;
        }

        if (Random.value <= rainChance) StartRain();
        else StopRain();
    }

    void Update()
    {
        if (_isRaining && targetToFollow != null && rainParticleSystem != null)
        {
            Vector3 targetPos = targetToFollow.position;
            Vector3 rainPosition = new Vector3(targetPos.x, targetPos.y + rainHeightOffset, targetPos.z);

            rainParticleSystem.transform.position = rainPosition;

            if (rainAudioSource != null)
            {
                rainAudioSource.transform.position = rainPosition;
                rainAudioSource.volume = rainVolume;
            }
        }
    }

    void StartRain()
    {
        _isRaining = true;

        if (rainParticleSystem)
        {
            rainParticleSystem.gameObject.SetActive(true);
            rainParticleSystem.Play();
        }

        if (rainAudioSource != null && !rainAudioSource.isPlaying) rainAudioSource.Play();

        if (_lightningRoutine != null) StopCoroutine(_lightningRoutine);
        _lightningRoutine = StartCoroutine(LightningRoutine());
    }

    void StopRain()
    {
        _isRaining = false;

        if (rainParticleSystem)
        {
            rainParticleSystem.Stop();
            rainParticleSystem.gameObject.SetActive(false);
        }

        if (rainAudioSource != null && rainAudioSource.isPlaying) rainAudioSource.Stop();

        if (_lightningRoutine != null) StopCoroutine(_lightningRoutine);
    }

    IEnumerator LightningRoutine()
    {
        while (_isRaining)
        {
            yield return new WaitForSeconds(Random.Range(lightningInterval * 0.8f, lightningInterval * 1.5f));

            bool struckPlayer = false;

            if (targetToFollow != null)
            {
                if (currentStrikeChance > 0 && !IsIndoors())
                {
                    if (Random.value <= currentStrikeChance)
                    {
                        int warningTimes = Random.Range(minWarningTimes, maxWarningTimes + 1);
                        bool warningCanceled = false;

                        for (int i = 0; i < warningTimes; i++)
                        {
                            if (currentStrikeChance <= 0 || IsIndoors())
                            {
                                warningCanceled = true;
                                Debug.Log("<color=green>Player đã vứt đồ sắt hoặc vào nhà! THOÁT NẠN.</color>");
                                break;
                            }

                            if (warningElectricSound != null)
                            {
                                Play3DSoundAtPosition(warningElectricSound, targetToFollow.position, warningVolume);
                            }

                            yield return new WaitForSeconds(warningInterval);
                        }

                        if (!warningCanceled && !IsIndoors())
                        {
                            Debug.Log("<color=red>SÉT ĐÁNH TRÚNG PLAYER!</color>");
                            SpawnLightningVFX(targetToFollow.position);

                            if (thunderStrikeSound != null)
                                Play3DSoundAtPosition(thunderStrikeSound, targetToFollow.position, thunderVolume);

                            if (DatScript.PlayerHealth.instance != null)
                                DatScript.PlayerHealth.instance.TakeDamage(9999f);

                            struckPlayer = true;
                        }
                    }
                }
            }

            if (!struckPlayer)
            {
                if (Random.value <= environmentStrikeChance)
                {
                    SpawnRandomEnvironmentLightning();
                }
            }
        }
    }

    void SpawnRandomEnvironmentLightning()
    {
        float randX = targetToFollow.position.x + Random.Range(-randomStrikeRadius, randomStrikeRadius);
        float randZ = targetToFollow.position.z + Random.Range(-randomStrikeRadius, randomStrikeRadius);

        Vector3 rayStartPos = new Vector3(randX, 100f, randZ);
        Vector3 strikePos;

        if (Physics.Raycast(rayStartPos, Vector3.down, out RaycastHit hit, 200f))
            strikePos = hit.point;
        else
            strikePos = new Vector3(randX, 0f, randZ);

        SpawnLightningVFX(strikePos);

        if (thunderStrikeSound != null)
        {
            Play3DSoundAtPosition(thunderStrikeSound, strikePos, thunderVolume * 0.9f);
        }

        if (targetToFollow != null)
        {
            float distanceFromStrike = Vector3.Distance(targetToFollow.position, strikePos);
            if (distanceFromStrike <= environmentStrikeDamageRadius)
            {
                Debug.Log("<color=red>ĐEN ĐỦI! Player bị sét môi trường đánh trúng (Vô tình đứng quá gần)!</color>");
                if (DatScript.PlayerHealth.instance != null)
                {
                    DatScript.PlayerHealth.instance.TakeDamage(9999f);
                }
            }
        }
    }

    bool IsIndoors()
    {
        return Physics.Raycast(targetToFollow.position + Vector3.up, Vector3.up, 50f, obstacleLayer);
    }

    void SpawnLightningVFX(Vector3 position)
    {
        if (lightningPrefab == null) return;
        GameObject lightning = Instantiate(lightningPrefab, position, Quaternion.identity);
        Destroy(lightning, 2.0f);
    }

    void Play3DSoundAtPosition(AudioClip clip, Vector3 position, float volume)
    {
        GameObject tempAudioObj = new GameObject("Temp3DAudio_" + clip.name);
        tempAudioObj.transform.position = position;

        AudioSource audioSource = tempAudioObj.AddComponent<AudioSource>();
        audioSource.clip = clip;
        audioSource.volume = volume;

        audioSource.spatialBlend = 1.0f;
        audioSource.minDistance = soundMinDistance;
        audioSource.maxDistance = 150f;
        audioSource.rolloffMode = AudioRolloffMode.Linear;
        audioSource.dopplerLevel = 0f;

        audioSource.Play();

        Destroy(tempAudioObj, clip.length + 0.1f);
    }
}