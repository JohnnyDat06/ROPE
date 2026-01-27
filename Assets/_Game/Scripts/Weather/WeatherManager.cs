using UnityEngine;
using System.Collections;

public class WeatherManager : MonoBehaviour
{
    [Header("--- Rain Settings ---")]
    public ParticleSystem rainParticleSystem;
    public Transform targetToFollow; // Player
    [Range(0f, 1f)] public float rainChance = 0.5f;
    public float rainHeightOffset = 15f;

    [Header("--- Lightning Settings (Sét) ---")]
    [Tooltip("Kéo Prefab VFX Sét từ Asset Store vào đây")]
    public GameObject lightningPrefab;

    [Tooltip("Layer của trần nhà/vật cản che mưa (để check trong nhà)")]
    public LayerMask obstacleLayer;

    [Tooltip("Khoảng cách tia sét đánh ngẫu nhiên xung quanh Player")]
    public float randomStrikeRadius = 20f;

    [Tooltip("Thời gian giữa các lần sét đánh (giây)")]
    public float lightningInterval = 3.0f;

    [Header("--- Player Status ---")]
    [Tooltip("Số lượng đồ sắt hiện tại (Update biến này từ hệ thống Inventory của bạn)")]
    public int ironItemCount = 0;

    private bool _isRaining = false;
    private Coroutine _lightningRoutine;

    void Start()
    {
        // Logic Random Mưa 50%
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
        // Logic Mưa đi theo người
        if (_isRaining && targetToFollow != null && rainParticleSystem != null)
        {
            Vector3 targetPos = targetToFollow.position;
            rainParticleSystem.transform.position = new Vector3(targetPos.x, targetPos.y + rainHeightOffset, targetPos.z);
        }
    }

    // --- CÁC HÀM XỬ LÝ MƯA ---

    void StartRain()
    {
        _isRaining = true;
        if (rainParticleSystem)
        {
            rainParticleSystem.gameObject.SetActive(true);
            rainParticleSystem.Play();
        }
        Debug.Log("<color=cyan>Weather: MƯA BẮT ĐẦU</color>");

        // Bắt đầu quy trình tạo sét
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
        Debug.Log("<color=yellow>Weather: TRỜI TẠNH</color>");

        // Dừng tạo sét
        if (_lightningRoutine != null) StopCoroutine(_lightningRoutine);
    }

    // --- LOGIC SÉT (THUNDER LOGIC) ---

    IEnumerator LightningRoutine()
    {
        while (_isRaining)
        {
            // Chờ một khoảng thời gian ngẫu nhiên giữa các lần đánh
            yield return new WaitForSeconds(Random.Range(lightningInterval * 0.5f, lightningInterval * 1.5f));

            // 1. XỬ LÝ SÉT ĐÁNH NGẪU NHIÊN MÔI TRƯỜNG (Y = 0)
            SpawnRandomEnvironmentLightning();

            // 2. XỬ LÝ SÉT ĐÁNH VÀO PLAYER (Do cầm đồ sắt)
            if (targetToFollow != null)
            {
                CheckPlayerLightningStrike();
            }
        }
    }

    void SpawnRandomEnvironmentLightning()
    {
        // Random vị trí X, Z xung quanh người chơi
        float randX = targetToFollow.position.x + Random.Range(-randomStrikeRadius, randomStrikeRadius);
        float randZ = targetToFollow.position.z + Random.Range(-randomStrikeRadius, randomStrikeRadius);

        // Yêu cầu: Đánh xuống Y = 0
        Vector3 strikePos = new Vector3(randX, 0f, randZ);

        SpawnLightningVFX(strikePos);
    }

    void CheckPlayerLightningStrike()
    {
        // Bước A: Kiểm tra xem có đang ở trong nhà không?
        if (IsIndoors())
        {
            // Debug.Log("Player đang trong nhà -> An toàn.");
            return;
        }

        // Bước B: Tính tỷ lệ bị đánh dựa trên đồ sắt
        // Tối đa 4 món * 10% = 40% (0.4)
        int effectiveItems = Mathf.Clamp(ironItemCount, 0, 4);
        float strikeChance = effectiveItems * 0.1f;

        // Nếu không có đồ sắt -> strikeChance = 0 -> Không bao giờ bị đánh trúng
        if (effectiveItems > 0)
        {
            // Bước C: Roll xúc xắc
            if (Random.value <= strikeChance)
            {
                Debug.Log($"<color=red>SÉT ĐÁNH TRÚNG PLAYER! (Tỷ lệ: {strikeChance * 100}%)</color>");

                // Đánh ngay tại vị trí Player
                SpawnLightningVFX(targetToFollow.position);

                // TODO: Gọi hàm trừ máu Player ở đây
                // targetToFollow.GetComponent<PlayerHealth>()?.TakeDamage(50);
            }
        }
    }

    // Hàm check xem Player có đang bị che chắn bởi mái nhà không
    bool IsIndoors()
    {
        // Bắn Raycast từ đầu Player thẳng lên trời
        // Vector3.up: Hướng lên
        // 50f: Độ dài tia (trần nhà cao quá 50m coi như ngoài trời)
        // obstacleLayer: Chỉ check va chạm với Layer được chỉ định (Ví dụ: Layer "Roof" hoặc "Default")
        return Physics.Raycast(targetToFollow.position + Vector3.up, Vector3.up, 50f, obstacleLayer);
    }

    void SpawnLightningVFX(Vector3 position)
    {
        if (lightningPrefab == null) return;

        // Tạo hiệu ứng sét
        GameObject lightning = Instantiate(lightningPrefab, position, Quaternion.identity);

        // Hủy sau 2 giây (để đỡ nặng máy)
        Destroy(lightning, 2.0f);
    }
}