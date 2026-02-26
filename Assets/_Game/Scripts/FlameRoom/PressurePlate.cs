using UnityEngine;
using System.Collections.Generic;

public class PressurePlate : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private Transform visualModel;
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float pressDepth = 0.15f;

    [Header("Debug Info")]
    // Kiểm tra xem có vật nào hợp lệ đang đè lên không
    public bool IsPressed => objectsOnPlate.Count > 0;

    private List<Collider> objectsOnPlate = new List<Collider>();
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
        // 1. Dọn dẹp danh sách (QUAN TRỌNG)
        // Nếu món đồ bị người chơi nhặt -> Nó bị Disable hoặc Destroy -> Cần xóa khỏi list để bàn đạp nảy lên
        ValidateObjectsOnPlate();

        // 2. Xử lý chuyển động
        HandleMovement();
    }

    // --- LOGIC NHẬN DIỆN MỚI ---
    public void OnObjectEnter(Collider other)
    {
        // A. Kiểm tra Player
        if (other.CompareTag("Player"))
        {
            AddObj(other);
            return;
        }

        // B. Kiểm tra Item từ hệ thống Inventory (ĐỒNG BỘ VỚI INTERACTABLE)
        // Thay vì check Tag, ta check xem nó có script ItemController không
        if (other.TryGetComponent(out ItemController item))
        {
            AddObj(other);
        }
    }

    public void OnObjectExit(Collider other)
    {
        RemoveObj(other);
    }

    // Các hàm phụ trợ để quản lý List an toàn hơn
    void AddObj(Collider col)
    {
        if (!objectsOnPlate.Contains(col))
        {
            objectsOnPlate.Add(col);
        }
    }

    void RemoveObj(Collider col)
    {
        if (objectsOnPlate.Contains(col))
        {
            objectsOnPlate.Remove(col);
        }
    }

    void ValidateObjectsOnPlate()
    {
        // Duyệt ngược để xóa các phần tử null hoặc không còn active (đã bị nhặt)
        for (int i = objectsOnPlate.Count - 1; i >= 0; i--)
        {
            Collider col = objectsOnPlate[i];

            // Nếu vật thể bị hủy (null) hoặc bị tắt (SetActive false - do chui vào túi đồ)
            if (col == null || !col.gameObject.activeInHierarchy || !col.enabled)
            {
                objectsOnPlate.RemoveAt(i);
            }
        }
    }

    void HandleMovement()
    {
        if (visualModel == null) return;
        Vector3 destination = IsPressed ? (initialPos - new Vector3(0, pressDepth, 0)) : initialPos;
        visualModel.localPosition = Vector3.MoveTowards(visualModel.localPosition, destination, moveSpeed * Time.deltaTime);
    }
}