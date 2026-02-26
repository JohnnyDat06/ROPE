using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using TMPro;

public class SellingZone : MonoBehaviour
{
    [Header("--- Game Balance Settings ---")]
    [Tooltip("Tỷ lệ phần trăm tổng giá trị đồ trong map cần để thắng (0.75 = 75%)")]
    [Range(0.1f, 1.0f)]
    public float winPercentage = 0.75f;

    [Header("--- Current Quota (Auto Calculated) ---")]
    public int quotaMoney = 0;

    [Header("--- Selling Settings ---")]
    public float sellDelay = 7.0f;
    public float floatHeight = 1.0f;
    public float rocketSpeed = 50.0f;

    [Header("--- Reward (KeyCard) Settings ---")]
    public GameObject keyCardPrefab;
    public Transform rewardSpawnPoint;
    [Tooltip("Độ rơi chậm lúc đang bay")]
    public float rewardFallingDrag = 4.0f;

    [Tooltip("Chỉ chọn Layer của sàn nhà (Ground/Default). Đừng chọn Trigger!")]
    public LayerMask groundLayer;

    // ==========================================
    // --- MỚI THÊM: AUDIO SETTINGS ---
    // ==========================================
    [Header("--- Audio Settings ---")]
    public AudioSource audioSource;
    public AudioClip rewardDropSound; // File âm thanh 12-13s
    [Range(0f, 1f)] public float airborneVolume = 1.0f; // Âm lượng lúc đang rơi
    [Range(0f, 1f)] public float landedVolume = 0.3f;   // Âm lượng lúc chạm đất (nhỏ lại)
    public float fadeDuration = 1.5f; // Mất bao lâu để âm thanh từ to chuyển sang nhỏ (giúp nghe tự nhiên hơn)
    // ==========================================

    [Header("--- References ---")]
    public Transform rugCenter;
    public TMP_Text infoText;

    // Internal Variables
    private List<ItemController> itemsOnRug = new List<ItemController>();
    private Coroutine sellProcess;
    private bool isSelling = false;
    private bool isLaunching = false;

    private bool hasSpawnedKeyCard = false;
    private bool isQuotaRevealed = false;

    private void Start()
    {
        CalculateDynamicQuota();
        itemsOnRug.Clear();
        UpdateUI(0);
    }

    void CalculateDynamicQuota()
    {
        ItemController[] allItemsInMap = FindObjectsByType<ItemController>(FindObjectsSortMode.None);
        int totalMapValue = 0;
        foreach (var item in allItemsInMap) if (item.scrapValue > 0) totalMapValue += item.scrapValue;
        quotaMoney = Mathf.RoundToInt(totalMapValue * winPercentage);
        if (quotaMoney <= 0) quotaMoney = 100;
        Debug.Log($"<color=yellow>[GAME BALANCE] Tổng Map: {totalMapValue}$. Quota ({winPercentage * 100}%): {quotaMoney}$</color>");
    }

    private void Update()
    {
        if (!isLaunching && itemsOnRug.Count > 0) CheckAndHandleState();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (isLaunching) return;
        ItemController item = other.GetComponentInParent<ItemController>();
        if (item != null && item.transform.parent == null && !itemsOnRug.Contains(item))
        {
            itemsOnRug.Add(item);
            CheckAndHandleState();
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (isLaunching) return;
        ItemController item = other.GetComponentInParent<ItemController>();
        if (item != null && itemsOnRug.Contains(item))
        {
            itemsOnRug.Remove(item);
            RestoreItemPhysics(item);
            CheckAndHandleState();
        }
    }

    void RestoreItemPhysics(ItemController item)
    {
        if (item == null) return;
        Rigidbody rb = item.GetComponent<Rigidbody>();
        if (rb)
        {
            rb.useGravity = true;
            rb.linearVelocity = Vector3.zero;
            rb.linearDamping = 0;
            rb.angularDamping = 0.05f;
        }
    }

    void CheckAndHandleState()
    {
        if (isLaunching) return;

        int total = CalculateTotalValue();

        if (total > 0) isQuotaRevealed = true;

        UpdateUI(total);

        if (total >= quotaMoney)
        {
            if (!isSelling) StartSellingProcess();
        }
        else
        {
            if (isSelling) CancelSellingProcess();
        }
    }

    int CalculateTotalValue()
    {
        int total = 0;
        for (int i = itemsOnRug.Count - 1; i >= 0; i--)
        {
            ItemController item = itemsOnRug[i];
            if (item == null || item.transform.parent != null)
            {
                itemsOnRug.RemoveAt(i);
                continue;
            }
            total += item.scrapValue;
        }
        return total;
    }

    void StartSellingProcess()
    {
        isSelling = true;
        if (sellProcess != null) StopCoroutine(sellProcess);
        sellProcess = StartCoroutine(SellingRoutine());
    }

    void CancelSellingProcess()
    {
        isSelling = false;
        if (sellProcess != null) StopCoroutine(sellProcess);
        foreach (var item in itemsOnRug) RestoreItemPhysics(item);
        UpdateUI(CalculateTotalValue());
    }

    IEnumerator SellingRoutine()
    {
        // GIAI ĐOẠN 1: ĐẾM NGƯỢC
        float timer = 0f;
        while (timer < sellDelay)
        {
            timer += Time.deltaTime;
            if (infoText) infoText.text = $"SELLING IN: {(sellDelay - timer):F1}s";
            if (CalculateTotalValue() < quotaMoney) { CancelSellingProcess(); yield break; }

            foreach (var item in itemsOnRug)
            {
                if (item == null) continue;
                Rigidbody rb = item.GetComponent<Rigidbody>();
                if (rb) { rb.useGravity = false; rb.linearVelocity = Vector3.zero; }
                Vector3 targetPos = rugCenter.position + Vector3.up * floatHeight;
                targetPos.y += Mathf.Sin(Time.time * 3f) * 0.1f;
                item.transform.position = Vector3.Lerp(item.transform.position, targetPos, Time.deltaTime * 2f);
                item.transform.Rotate(Vector3.up * 45 * Time.deltaTime);
            }
            yield return null;
        }

        // GIAI ĐOẠN 2: PHÓNG
        isLaunching = true;
        if (infoText) infoText.text = "LAUNCHING...";
        foreach (var item in itemsOnRug) { if (item != null) { Collider col = item.GetComponent<Collider>(); if (col) col.enabled = false; } }

        float launchTime = 0f;
        while (launchTime < 2.0f)
        {
            launchTime += Time.deltaTime;
            foreach (var item in itemsOnRug) if (item != null) item.transform.Translate(Vector3.up * rocketSpeed * Time.deltaTime, Space.World);
            yield return null;
        }

        // GIAI ĐOẠN 3: DỌN DẸP
        int finalMoney = 0;
        foreach (var item in itemsOnRug) { if (item != null) { finalMoney += item.scrapValue; Destroy(item.gameObject); } }
        itemsOnRug.Clear();
        if (infoText) infoText.text = $"SOLD: {finalMoney}$";

        // >>> GIAI ĐOẠN 4: SPAWN KEY CARD <<<
        if (keyCardPrefab != null && !hasSpawnedKeyCard)
        {
            hasSpawnedKeyCard = true;

            Vector3 spawnPos = (rewardSpawnPoint != null) ? rewardSpawnPoint.position : (rugCenter.position + Vector3.up * 3.5f);
            if (rewardSpawnPoint != null && spawnPos.y < rugCenter.position.y + 1f) spawnPos.y += 3.5f;

            GameObject card = Instantiate(keyCardPrefab, spawnPos, Quaternion.Euler(-90, 0, 0));
            Rigidbody cardRb = card.GetComponent<Rigidbody>();

            // --- MỚI THÊM: PHÁT ÂM THANH KHI BẮT ĐẦU RƠI ---
            if (audioSource != null && rewardDropSound != null)
            {
                audioSource.clip = rewardDropSound;
                audioSource.volume = airborneVolume; // Đặt âm lượng to
                audioSource.Play();
            }
            // -----------------------------------------------

            if (cardRb)
            {
                cardRb.useGravity = true;
                cardRb.linearDamping = rewardFallingDrag;
                cardRb.angularVelocity = new Vector3(0, 3.0f, 0);
                StartCoroutine(HandleCardLanding(card, cardRb));
            }
        }

        yield return new WaitForSeconds(3.0f);
        isSelling = false; isLaunching = false; UpdateUI(0);
    }

    IEnumerator HandleCardLanding(GameObject card, Rigidbody rb)
    {
        yield return new WaitForSeconds(0.5f);

        bool isGrounded = false;
        float timeout = 8.0f;

        while (!isGrounded && card != null && timeout > 0)
        {
            timeout -= Time.deltaTime;
            if (Physics.Raycast(card.transform.position, Vector3.down, 0.5f, groundLayer))
            {
                isGrounded = true;
            }
            yield return null;
        }

        if (card == null) yield break;

        // KHI CHẠM ĐẤT

        // --- MỚI THÊM: GIẢM ÂM LƯỢNG MƯỢT MÀ XUỐNG KHI CHẠM ĐẤT ---
        StartCoroutine(FadeVolume(landedVolume, fadeDuration));
        // ---------------------------------------------------------

        rb.angularVelocity = Vector3.zero;
        rb.linearVelocity = Vector3.zero;
        Quaternion startRot = card.transform.rotation;
        Quaternion targetRot = Quaternion.Euler(0, 0, 0);

        float t = 0;
        while (t < 1.0f && card != null)
        {
            t += Time.deltaTime * 5.0f;
            card.transform.rotation = Quaternion.Lerp(startRot, targetRot, t);
            rb.linearVelocity = Vector3.zero;
            yield return null;
        }

        if (card != null && rb != null)
        {
            card.transform.rotation = targetRot;
            rb.isKinematic = false;
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.linearDamping = 1f;
        }
    }

    // ==========================================
    // --- MỚI THÊM: HÀM FADE ÂM LƯỢNG ---
    // ==========================================
    private IEnumerator FadeVolume(float targetVolume, float duration)
    {
        if (audioSource == null) yield break;

        float startVolume = audioSource.volume;
        float time = 0;

        while (time < duration)
        {
            time += Time.deltaTime;
            // Chuyển đổi mượt mà từ âm lượng hiện tại về âm lượng chạm đất
            audioSource.volume = Mathf.Lerp(startVolume, targetVolume, time / duration);
            yield return null;
        }

        audioSource.volume = targetVolume;
    }
    // ==========================================

    void UpdateUI(int current)
    {
        if (infoText == null || isLaunching) return;
        if (isSelling) return;

        string color = current >= quotaMoney ? "green" : "red";
        string displayQuota = (isQuotaRevealed) ? quotaMoney.ToString() : "???";

        infoText.text = $"TOTAL: <color={color}>{current}</color> / {displayQuota}$";
    }
}