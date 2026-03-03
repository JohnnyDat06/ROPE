using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class ScannerSystem : MonoBehaviour
{
    [Header("--- Scan Settings ---")]
    public float maxScanRadius = 30f;
    public float scanDuration = 1.5f;

    [Tooltip("Layer chứa các vật phẩm có thể nhặt/tương tác")]
    public LayerMask itemLayer;

    [Tooltip("Layer chứa điểm yếu của Boss")]
    public LayerMask bossWeakPointLayer;

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
        if (currentCooldown > 0)
        {
            currentCooldown -= Time.deltaTime;
        }

        if (Input.GetKeyDown(KeyCode.V) && !isScanning && currentCooldown <= 0)
        {
            currentCooldown = scanCooldown;
            StartCoroutine(ScanWaveRoutine());
        }
    }

    IEnumerator ScanWaveRoutine()
    {
        isScanning = true;
        float timer = 0f;

        // Sử dụng 2 HashSet riêng biệt để quản lý Item và Điểm yếu
        HashSet<ItemController> scannedItems = new HashSet<ItemController>();
        HashSet<BossWeakPoint> scannedWeakPoints = new HashSet<BossWeakPoint>();

        GameObject waveObj = null;
        if (scanRingPrefab != null)
        {
            Vector3 spawnPos = transform.position;

            if (Physics.Raycast(transform.position, Vector3.down, out RaycastHit hit, 20f, groundLayer))
            {
                spawnPos = hit.point + Vector3.up * 0.05f;
            }

            waveObj = Instantiate(scanRingPrefab, spawnPos, Quaternion.identity);
            waveObj.transform.localScale = Vector3.zero;
        }

        // Gộp cả 2 LayerMask lại để quét trong 1 lần OverlapSphere duy nhất (Tối ưu hiệu suất)
        LayerMask combinedScanLayer = itemLayer | bossWeakPointLayer;

        while (timer < scanDuration)
        {
            timer += Time.deltaTime;
            float currentRadius = Mathf.Lerp(0, maxScanRadius, timer / scanDuration);

            if (waveObj != null)
            {
                waveObj.transform.localScale = new Vector3(currentRadius * 2f, currentRadius * 2f, currentRadius * 2f);

                Renderer waveRenderer = waveObj.GetComponent<Renderer>();
                if (waveRenderer != null)
                {
                    float currentFade = Mathf.Lerp(1f, 0f, timer / scanDuration);
                    waveRenderer.material.SetFloat("_Fade", currentFade);
                }
            }

            // Quét tất cả các object thuộc itemLayer HOẶC bossWeakPointLayer
            Collider[] hits = Physics.OverlapSphere(transform.position, currentRadius, combinedScanLayer);
            foreach (Collider hit in hits)
            {
                // 1. Nếu chạm vào Item
                if ((itemLayer.value & (1 << hit.gameObject.layer)) > 0)
                {
                    ItemController item = hit.GetComponentInParent<ItemController>();
                    if (item != null && !scannedItems.Contains(item))
                    {
                        scannedItems.Add(item);
                        item.TriggerHighlight();
                    }
                }
                // 2. Nếu chạm vào Điểm yếu Boss
                else if ((bossWeakPointLayer.value & (1 << hit.gameObject.layer)) > 0)
                {
                    BossWeakPoint weakPoint = hit.GetComponentInParent<BossWeakPoint>();
                    if (weakPoint != null && !scannedWeakPoints.Contains(weakPoint))
                    {
                        scannedWeakPoints.Add(weakPoint);
                        weakPoint.TriggerHighlight(); // Bật outline cho điểm yếu
                    }
                }
            }

            yield return null;
        }

        if (waveObj != null)
        {
            Destroy(waveObj);
        }

        isScanning = false;
    }
}