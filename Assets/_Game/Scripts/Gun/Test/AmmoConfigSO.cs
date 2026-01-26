using UnityEngine;

[CreateAssetMenu(fileName = "AmmoutConfig", menuName = "Guns/AmmoConfig", order = 3)]
public class AmmoConfigSO : ScriptableObject
{
    [Header("Cấu hình mặc định")]
    public int maxAmmo = 120;
    public int clipSize = 30;

    [Header("Trạng thái hiện tại")]
    public int currentAmmo;
    public int currentClipAmmo;

    // Hàm này tự động chạy khi ScriptableObject được load (hoặc khi bắt đầu game trong Editor)
    // Giúp reset lại số đạn về mặc định, tránh việc bị lưu số 0 vĩnh viễn.
    private void OnEnable()
    {
        currentAmmo = maxAmmo;
        currentClipAmmo = clipSize;
    }

    public void Reload()
    {
        // 1. Tính số đạn cần thiết để làm đầy băng đạn
        int bulletsNeeded = clipSize - currentClipAmmo;

        // 2. Tính số đạn thực tế có thể nạp (lấy số nhỏ hơn giữa: đạn cần và đạn đang có)
        int bulletsToReload = Mathf.Min(bulletsNeeded, currentAmmo);

        // 3. Thực hiện nạp đạn
        if (bulletsToReload > 0)
        {
            currentClipAmmo += bulletsToReload; // Cộng vào băng đạn
            currentAmmo -= bulletsToReload;     // Trừ đi ở kho dự trữ
        }
    }

    public bool CanReload()
    {
        // Chỉ nạp được khi băng đạn chưa đầy VÀ còn đạn trong kho
        return currentClipAmmo < clipSize && currentAmmo > 0;
    }
}