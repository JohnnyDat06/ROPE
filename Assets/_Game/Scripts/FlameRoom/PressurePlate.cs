using UnityEngine;

public class PressurePlate : MonoBehaviour
{
    [Header("Cài đặt di chuyển đĩa")]
    [SerializeField] private Transform visualModel;
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float pressDepth = 0.15f;

    [Header("Cài đặt Radar Quét Đồ Vật")]
    [Tooltip("Kích thước vùng không gian hộp quét phía trên đĩa")]
    [SerializeField] private Vector3 detectionBoxSize = new Vector3(1f, 0.5f, 1f);
    [Tooltip("Điểm đặt tâm của vùng quét (kéo lên cao một chút so với mặt đĩa)")]
    [SerializeField] private Vector3 detectionOffset = new Vector3(0, 0.25f, 0);

    [Header("Trạng thái (Debug)")]
    public bool IsPressed = false;

    private Vector3 initialPos;

    void Start()
    {
        if (visualModel != null)
        {
            initialPos = visualModel.localPosition;
        }
    }

    void Update()
    {
        // Liên tục quét xem có gì đè lên không mỗi khung hình
        CheckPlateWithRadar();

        // Cập nhật hoạt ảnh lún xuống/nảy lên
        HandleMovement();
    }

    void CheckPlateWithRadar()
    {
        IsPressed = false;
        Vector3 center = transform.position + detectionOffset;

        // Quét tạo ra một cái hộp ảo, lấy toàn bộ Collider lọt vào trong hộp
        Collider[] hits = Physics.OverlapBox(center, detectionBoxSize / 2f, transform.rotation);

        foreach (var hit in hits)
        {
            // Bỏ qua chính bản thân cái đĩa hoặc các vùng trigger vô hình
            if (hit.gameObject == gameObject || hit.isTrigger) continue;

            // Nếu vật chạm vào là Player HOẶC là món đồ có chứa ItemController
            if (hit.CompareTag("Player") || hit.GetComponent<ItemController>() != null)
            {
                IsPressed = true;
                break; // Có 1 vật đè lên là đủ kích hoạt, thoát vòng lặp
            }
        }
    }

    void HandleMovement()
    {
        if (visualModel == null) return;
        Vector3 destination = IsPressed ? (initialPos - new Vector3(0, pressDepth, 0)) : initialPos;
        visualModel.localPosition = Vector3.MoveTowards(visualModel.localPosition, destination, moveSpeed * Time.deltaTime);
    }

    // Hiển thị hộp quét màu xanh lá trong màn hình Editor để bạn dễ căn chỉnh
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0, 1, 0, 0.4f);
        Gizmos.matrix = Matrix4x4.TRS(transform.position + detectionOffset, transform.rotation, Vector3.one);
        Gizmos.DrawWireCube(Vector3.zero, detectionBoxSize);
    }
}