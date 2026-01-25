using UnityEngine;
using System.Collections.Generic;

public class PressurePlate : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private Transform visualModel; // Kéo Visual_Model vào đây
    [SerializeField] private float moveSpeed = 5f;  // Tốc độ lún xuống/nảy lên
    [SerializeField] private float pressDepth = 0.15f; // Độ sâu lún xuống (mét)

    [Header("Debug Info")]
    public bool IsPressed => objectsOnPlate.Count > 0; // Public để Controller đọc

    private List<Collider> objectsOnPlate = new List<Collider>();
    private Vector3 initialPos;
    private Vector3 targetPos;
    public void OnObjectEnter(Collider other)
    {
        // Kiểm tra Tag Player hoặc Vật phẩm
        if (other.CompareTag("Player") || other.CompareTag("QuestItem"))
        {
            if (!objectsOnPlate.Contains(other))
            {
                objectsOnPlate.Add(other);
            }
        }
    }

    public void OnObjectExit(Collider other)
    {
        if (objectsOnPlate.Contains(other))
        {
            objectsOnPlate.Remove(other);
        }
    }
    void Start()
    {
        // Lưu vị trí ban đầu của phần hình ảnh
        if (visualModel != null)
        {
            initialPos = visualModel.localPosition;
        }
        else
        {
            Debug.LogError("Chưa gán Visual Model cho Pressure Plate!");
        }
    }

    void Update()
    {
        HandleMovement();
    }

    // Hàm xử lý di chuyển lên xuống mượt mà
    void HandleMovement()
    {
        if (visualModel == null) return;

        // Nếu đang bị đè -> Đích đến là vị trí lún xuống (trừ trục Y)
        // Nếu không -> Đích đến là vị trí ban đầu
        Vector3 destination = IsPressed ? (initialPos - new Vector3(0, pressDepth, 0)) : initialPos;

        // Dùng MoveTowards để di chuyển tuyến tính, tránh rung lắc
        visualModel.localPosition = Vector3.MoveTowards(visualModel.localPosition, destination, moveSpeed * Time.deltaTime);
    }

    // --- PHẦN XỬ LÝ TRIGGER (Của Sensor) ---
    // Lưu ý: Script này nằm ở cha, nhưng Trigger nằm ở con (Sensor_Trigger).
    // Để cha nhận được sự kiện của con, bạn cần 1 thủ thuật nhỏ hoặc gắn script này trực tiếp vào Sensor.
    // NHƯNG để Pro và gọn, ta sẽ dùng hàm OnTriggerEnter TỪ SENSOR gọi lên CHA.

    // --> Cách đơn giản nhất cho bạn: Hãy gắn script này vào thằng cha (Group).
    // Nhưng thằng Sensor_Trigger cần thêm script phụ trợ nhỏ ở dưới hoặc chỉnh lại Physics settings.

    // CÁCH DỄ NHẤT: Để Script này ở Cha, và đổi logic Trigger một chút:
    // Bạn hãy làm theo hướng dẫn "Bước 3" bên dưới để kết nối logic.
}