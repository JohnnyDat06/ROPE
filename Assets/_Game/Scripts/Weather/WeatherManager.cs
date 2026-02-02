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

    [Header("--- Player Status (Đồng bộ từ Inventory) ---")]
    [Tooltip("Tỷ lệ % bị sét đánh hiện tại (Được tính toán từ Inventory)")]
    [Range(0f, 1f)] public float currentStrikeChance = 0f;

    private bool _isRaining = false;
    private Coroutine _lightningRoutine;

    void Start()
    {
        if (Random.value <= rainChance) StartRain();
        else StopRain();
    }

    void Update()
    {
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

        if (_lightningRoutine != null) StopCoroutine(_lightningRoutine);
    }

    // --- LOGIC SÉT MỚI (RIÊNG BIỆT) ---

    IEnumerator LightningRoutine()
    {
        while (_isRaining)
        {
            // Chờ giữa các lần sét đánh
            yield return new WaitForSeconds(Random.Range(lightningInterval * 0.5f, lightningInterval * 1.5f));

            // --- LOGIC MỚI: CHỈ ĐÁNH 1 TIA DUY NHẤT ---

            bool struckPlayer = false;

            // 1. Thử đánh Player trước (Ưu tiên cao nhất)
            if (targetToFollow != null)
            {
                struckPlayer = TryStrikePlayer();
            }

            // 2. Nếu KHÔNG đánh trúng Player -> Mới được phép đánh xuống đất
            if (!struckPlayer)
            {
                SpawnRandomEnvironmentLightning();
            }
        }
    }

    // Hàm này trả về TRUE nếu sét đánh trúng người, FALSE nếu trượt
    bool TryStrikePlayer()
    {
        // A. Nếu đang trong nhà -> Tuyệt đối an toàn -> Return False (để sét đánh ngoài trời)
        if (IsIndoors()) return false;

        // B. Nếu có tỷ lệ bị đánh (do cầm sắt)
        if (currentStrikeChance > 0)
        {
            // Roll xúc xắc
            if (Random.value <= currentStrikeChance)
            {
                Debug.Log($"<color=red>SÉT ĐÁNH TRÚNG PLAYER! (Tỷ lệ: {currentStrikeChance * 100}%)</color>");

                // Đánh ngay đầu Player
                SpawnLightningVFX(targetToFollow.position);

                // TODO: Trừ máu Player tại đây

                return true; // Báo hiệu là "Đã đánh trúng rồi, đừng đánh chỗ khác nữa"
            }
        }

        return false; // Trượt, hoặc không cầm sắt
    }

    void SpawnRandomEnvironmentLightning()
    {
        float randX = targetToFollow.position.x + Random.Range(-randomStrikeRadius, randomStrikeRadius);
        float randZ = targetToFollow.position.z + Random.Range(-randomStrikeRadius, randomStrikeRadius);

        // Đánh xuống đất (Y=0)
        Vector3 strikePos = new Vector3(randX, 0f, randZ);

        SpawnLightningVFX(strikePos);
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
}