using UnityEngine;

public class WeatherManager : MonoBehaviour
{
    [Header("--- Settings ---")]
    [Tooltip("Kéo Prefab hoặc Object chứa Particle System mưa vào đây")]
    public ParticleSystem rainParticleSystem;

    [Tooltip("Kéo Transform của Player hoặc Main Camera vào đây")]
    public Transform targetToFollow;

    [Tooltip("Tỷ lệ mưa (0.0 đến 1.0). 0.5 = 50%")]
    [Range(0f, 1f)] public float rainChance = 0.5f;

    [Tooltip("Độ cao của đám mây mưa so với Player")]
    public float rainHeightOffset = 15f;

    private bool _isRaining = false;

    void Start()
    {
        // 1. Logic Random 50%
        // Random.value trả về số từ 0.0 đến 1.0
        if (Random.value <= rainChance)
        {
            StartRain();
        }
        else
        {
            StopRain();
        }
    }

    void Update()
    {
        // 2. Logic Mưa đi theo người (Optimization Trick)
        if (_isRaining && targetToFollow != null && rainParticleSystem != null)
        {
            // Chỉ lấy vị trí X và Z của Player, giữ nguyên độ cao Y của mưa
            Vector3 targetPos = targetToFollow.position;

            // Cập nhật vị trí của Emitter
            // Chúng ta cộng thêm Offset vào Y để mưa luôn rơi từ trên cao xuống
            rainParticleSystem.transform.position = new Vector3(targetPos.x, targetPos.y + rainHeightOffset, targetPos.z);
        }
    }

    void StartRain()
    {
        _isRaining = true;
        rainParticleSystem.gameObject.SetActive(true);
        rainParticleSystem.Play();
        Debug.Log("<color=cyan>Weather: TRỜI MƯA (50% Chance Success)</color>");
    }

    void StopRain()
    {
        _isRaining = false;
        // Tắt object hoặc Stop particle tùy ý
        rainParticleSystem.Stop();
        rainParticleSystem.gameObject.SetActive(false);
        Debug.Log("<color=yellow>Weather: TRỜI TẠNH</color>");
    }
}