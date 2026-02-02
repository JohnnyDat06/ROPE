using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using TMPro;

public class SellingZone : MonoBehaviour
{
    [Header("Settings")]
    public int quotaMoney = 150;
    public float sellDelay = 7.0f;
    public float floatHeight = 1.0f;
    public float rocketSpeed = 50.0f;

    [Header("References")]
    public Transform rugCenter;
    public TMP_Text infoText;

    private List<ItemController> itemsOnRug = new List<ItemController>();
    private Coroutine sellProcess;
    private bool isSelling = false;
    private bool isLaunching = false;

    private void Start()
    {
        itemsOnRug.Clear();
        UpdateUI(0); // Lúc đầu là 0 -> Sẽ hiện "TOTAL: 0 / ???"
    }

    private void Update()
    {
        // Kiểm tra liên tục để cập nhật giá tiền nếu người chơi nhặt bớt đồ ra
        if (!isLaunching && itemsOnRug.Count > 0)
        {
            CheckAndHandleState();
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (isLaunching) return;

        if (other.TryGetComponent(out ItemController item))
        {
            // Chỉ nhận đồ chưa có ai cầm (Parent == null)
            if (item.transform.parent == null && !itemsOnRug.Contains(item))
            {
                itemsOnRug.Add(item);
                CheckAndHandleState();
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (isLaunching) return;

        if (other.TryGetComponent(out ItemController item))
        {
            if (itemsOnRug.Contains(item))
            {
                itemsOnRug.Remove(item);
                RestoreItemPhysics(item);
                CheckAndHandleState();
            }
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

            // Nếu item bị mất hoặc bị Player cầm lên -> Xóa khỏi list tính tiền
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

        Debug.Log("<color=red>HỦY BÁN! (Do rút bớt đồ)</color>");
        foreach (var item in itemsOnRug) RestoreItemPhysics(item);
        UpdateUI(CalculateTotalValue());
    }

    IEnumerator SellingRoutine()
    {
        float timer = 0f;
        while (timer < sellDelay)
        {
            timer += Time.deltaTime;
            if (infoText) infoText.text = $"SELLING IN: {(sellDelay - timer):F1}s";

            if (CalculateTotalValue() < quotaMoney)
            {
                CancelSellingProcess();
                yield break;
            }

            // Hiệu ứng bay
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

        isLaunching = true;
        if (infoText) infoText.text = "LAUNCHING...";

        foreach (var item in itemsOnRug)
        {
            if (item != null)
            {
                Collider col = item.GetComponent<Collider>();
                if (col) col.enabled = false;
            }
        }

        float launchTime = 0f;
        while (launchTime < 2.0f)
        {
            launchTime += Time.deltaTime;
            foreach (var item in itemsOnRug)
            {
                if (item != null) item.transform.Translate(Vector3.up * rocketSpeed * Time.deltaTime, Space.World);
            }
            yield return null;
        }

        int finalMoney = 0;
        foreach (var item in itemsOnRug)
        {
            if (item != null)
            {
                finalMoney += item.scrapValue;
                Destroy(item.gameObject);
            }
        }

        itemsOnRug.Clear();
        isSelling = false;
        isLaunching = false;

        if (infoText) infoText.text = $"SOLD: {finalMoney}$";
    }

    // --- ĐÂY LÀ HÀM SỬA ĐỔI ĐỂ ĐÁP ỨNG YÊU CẦU CỦA BẠN ---
    void UpdateUI(int current)
    {
        if (infoText == null || isLaunching) return;
        if (isSelling) return;

        string color = current >= quotaMoney ? "green" : "red";

        // LOGIC MỚI: 
        // - Nếu tiền hiện tại (current) > 0: Hiện Quota thật (quotaMoney).
        // - Nếu tiền hiện tại == 0: Hiện "???".
        string displayQuota = (current > 0) ? quotaMoney.ToString() : "???";

        infoText.text = $"TOTAL: <color={color}>{current}</color> / {displayQuota}$";
    }
}