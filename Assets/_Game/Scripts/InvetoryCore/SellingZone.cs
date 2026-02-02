using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using TMPro;

public class SellingZone : MonoBehaviour
{
    [Header("Settings")]
    public int quotaMoney = 150;
    public float sellDelay = 7.0f;   // Thời gian chờ (7s)
    public float floatHeight = 1.0f;
    public float rocketSpeed = 50.0f;

    [Header("References")]
    public Transform rugCenter;
    public TextMeshPro infoText;

    // Danh sách các món đồ đang nằm trên thảm
    private List<ItemController> itemsOnRug = new List<ItemController>();

    private Coroutine sellProcess;
    private bool isSelling = false;  // Đang đếm ngược
    private bool isLaunching = false; // Đang bay lên trời (Giai đoạn không thể đảo ngược)

    private void OnTriggerEnter(Collider other)
    {
        // Nếu đang trong giai đoạn phóng tàu vũ trụ thì không nhận thêm đồ nữa
        if (isLaunching) return;

        if (other.TryGetComponent(out ItemController item))
        {
            if (!itemsOnRug.Contains(item))
            {
                itemsOnRug.Add(item);
                CheckAndHandleState();
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        // QUAN TRỌNG: Nếu đang bay lên trời thì KHÔNG ĐƯỢC hủy bán
        // (Vì khi bay lên nó sẽ rời khỏi Collider, ta phải phớt lờ sự kiện này)
        if (isLaunching) return;

        if (other.TryGetComponent(out ItemController item))
        {
            if (itemsOnRug.Contains(item))
            {
                itemsOnRug.Remove(item);

                // Trả lại vật lý cho món đồ vừa bị lôi ra
                RestoreItemPhysics(item);

                CheckAndHandleState();
            }
        }
    }

    // Khôi phục vật lý để đồ rơi xuống đất bình thường
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
        // Nếu đang phóng thì không tính toán lại nữa
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

        // Cho tất cả rơi xuống
        foreach (var item in itemsOnRug)
        {
            RestoreItemPhysics(item);
        }

        // Cập nhật lại UI ngay lập tức
        UpdateUI(CalculateTotalValue());
    }

    IEnumerator SellingRoutine()
    {
        float timer = 0f;

        // ====================================================
        // GIAI ĐOẠN 1: ĐẾM NGƯỢC & BAY LƠ LỬNG (CÓ THỂ HỦY)
        // ====================================================
        while (timer < sellDelay)
        {
            timer += Time.deltaTime;
            if (infoText) infoText.text = $"SELLING IN: {(sellDelay - timer):F1}s";

            // Dùng vòng lặp ngược để an toàn nếu list bị thay đổi (do người chơi nhặt đồ)
            for (int i = itemsOnRug.Count - 1; i >= 0; i--)
            {
                ItemController item = itemsOnRug[i];

                // Kiểm tra null hoặc item đã bị Inventory lấy mất (parent không còn null)
                if (item == null || item.transform.parent != null)
                {
                    // Nếu item đã bị nhặt, xóa khỏi list bán hàng ngay tại đây
                    itemsOnRug.RemoveAt(i);
                    continue;
                }

                Rigidbody rb = item.GetComponent<Rigidbody>();
                if (rb)
                {
                    rb.useGravity = false;
                    rb.linearVelocity = Vector3.zero;
                }

                // Bay lơ lửng
                Vector3 targetPos = rugCenter.position + Vector3.up * floatHeight;
                targetPos.y += Mathf.Sin(Time.time * 3f) * 0.1f;
                item.transform.position = Vector3.Lerp(item.transform.position, targetPos, Time.deltaTime * 2f);
                item.transform.Rotate(Vector3.up * 45 * Time.deltaTime);
            }

            // Kiểm tra lại Quota liên tục (Đề phòng nhặt đồ ra mà logic chưa kịp cập nhật)
            if (CalculateTotalValue() < quotaMoney)
            {
                CancelSellingProcess();
                yield break; // Thoát Coroutine ngay
            }

            yield return null;
        }

        // ====================================================
        // GIAI ĐOẠN 2: CHỐT ĐƠN (KHÔNG THỂ HỦY TỪ ĐÂY)
        // ====================================================
        isLaunching = true; // BẬT CỜ KHÓA: Từ giờ OnTriggerExit sẽ bị vô hiệu hóa

        if (infoText) infoText.text = "LAUNCHING...";

        // Tắt Collider của tất cả item để không ai nhặt được nữa
        foreach (var item in itemsOnRug)
        {
            if (item != null)
            {
                Collider col = item.GetComponent<Collider>();
                if (col) col.enabled = false;
            }
        }

        // Bay lên trời
        float launchTime = 0f;
        while (launchTime < 2.0f)
        {
            launchTime += Time.deltaTime;
            foreach (var item in itemsOnRug)
            {
                if (item != null)
                {
                    item.transform.Translate(Vector3.up * rocketSpeed * Time.deltaTime, Space.World);
                }
            }
            yield return null;
        }

        // ====================================================
        // GIAI ĐOẠN 3: DỌN DẸP
        // ====================================================
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
        isLaunching = false; // Reset cờ khóa để bán đợt sau

        if (infoText) infoText.text = $"SOLD: {finalMoney}$";
        Debug.Log($"<color=green>BÁN XONG! +{finalMoney}$</color>");

        // TODO: GameManager.Instance.AddMoney(finalMoney);
    }

    int CalculateTotalValue()
    {
        int total = 0;
        foreach (var item in itemsOnRug)
        {
            if (item != null) total += item.scrapValue;
        }
        return total;
    }

    void UpdateUI(int current)
    {
        if (infoText == null || isLaunching) return;

        if (isSelling) return; // Đang đếm giây thì không hiện tiền đè lên

        string color = current >= quotaMoney ? "green" : "red";
        infoText.text = $"TOTAL: <color={color}>{current}</color> / {quotaMoney}$";
    }
}