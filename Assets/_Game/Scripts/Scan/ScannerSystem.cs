using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class ScannerSystem : MonoBehaviour
{
    [Header("--- Scan Settings ---")]
    public float maxScanRadius = 30f;
    public float scanDuration = 1.5f;
    public LayerMask itemLayer;

    [Tooltip("Chọn Layer của mặt đất/địa hình để sóng bám vào")]
    public LayerMask groundLayer;

    [Tooltip("Thời gian hồi chiêu (Cooldown) giữa các lần quét")]
    public float scanCooldown = 10f;
    private float currentCooldown = 0f;

    [Header("--- Visual Effects ---")]
    [Tooltip("Kéo Prefab ScanSphere (Hình Cầu) vào đây")]
    public GameObject scanRingPrefab;

    private bool isScanning = false;

    void Update()
    {
        // 1. Trừ lùi thời gian hồi chiêu
        if (currentCooldown > 0)
        {
            currentCooldown -= Time.deltaTime;
        }

        // 2. Bấm phím V để quét (chỉ khi hết cooldown và không đang quét)
        if (Input.GetKeyDown(KeyCode.V) && !isScanning && currentCooldown <= 0)
        {
            currentCooldown = scanCooldown; // Reset lại bộ đếm 10 giây
            StartCoroutine(ScanWaveRoutine());
        }
    }

    IEnumerator ScanWaveRoutine()
    {
        isScanning = true;
        float timer = 0f;
        HashSet<ItemController> scannedItems = new HashSet<ItemController>();

        // ---------------------------------------------------------
        // BƯỚC 1: TẠO RA VÒNG SÓNG ÂM VÀ GHIM XUỐNG MẶT ĐẤT
        // ---------------------------------------------------------
        GameObject waveObj = null;
        if (scanRingPrefab != null)
        {
            Vector3 spawnPos = transform.position;

            // Bắn tia xuống dưới chân nhân vật để tìm mặt đất (quét sâu tối đa 20m)
            if (Physics.Raycast(transform.position, Vector3.down, out RaycastHit hit, 20f, groundLayer))
            {
                // Nếu thấy đất -> Dời tâm sóng xuống đúng điểm chạm đất (nhích lên 0.05f để không chìm)
                spawnPos = hit.point + Vector3.up * 0.05f;
            }

            waveObj = Instantiate(scanRingPrefab, spawnPos, Quaternion.identity);

            // Vì là khối cầu (Sphere) nên khởi tạo Scale = 0 đều cho cả 3 trục
            waveObj.transform.localScale = Vector3.zero;
        }

        // ---------------------------------------------------------
        // BƯỚC 2: QUÁ TRÌNH SÓNG QUÉT LAN RỘNG DẦN
        // ---------------------------------------------------------
        while (timer < scanDuration)
        {
            timer += Time.deltaTime;

            // Tính bán kính quét hiện tại
            float currentRadius = Mathf.Lerp(0, maxScanRadius, timer / scanDuration);

            if (waveObj != null)
            {
                // Phóng to đều 3 trục (X, Y, Z) để khối cầu nở ra
                waveObj.transform.localScale = new Vector3(currentRadius * 2f, currentRadius * 2f, currentRadius * 2f);

                // Cập nhật giá trị làm mờ (_Fade) truyền vào Shader Graph
                Renderer waveRenderer = waveObj.GetComponent<Renderer>();
                if (waveRenderer != null)
                {
                    float currentFade = Mathf.Lerp(1f, 0f, timer / scanDuration);
                    waveRenderer.material.SetFloat("_Fade", currentFade);
                }
            }

            // Tìm vật phẩm nằm trong phạm vi khối cầu hiện tại
            Collider[] hits = Physics.OverlapSphere(transform.position, currentRadius, itemLayer);
            foreach (Collider hit in hits)
            {
                ItemController item = hit.GetComponentInParent<ItemController>();
                if (item != null && !scannedItems.Contains(item))
                {
                    scannedItems.Add(item);
                    item.TriggerHighlight(); // Phát sáng bằng Quick Outline
                }
            }

            yield return null;
        }

        // ---------------------------------------------------------
        // BƯỚC 3: KẾT THÚC QUÉT -> DỌN DẸP XÓA KHỐI CẦU
        // ---------------------------------------------------------
        if (waveObj != null)
        {
            Destroy(waveObj);
        }

        isScanning = false;
    }
}