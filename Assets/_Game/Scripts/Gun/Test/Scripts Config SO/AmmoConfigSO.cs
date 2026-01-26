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

    private void OnEnable()
    {
        currentAmmo = maxAmmo;
        currentClipAmmo = clipSize;
    }

    public void Reload()
    {
        int bulletsNeeded = clipSize - currentClipAmmo;

        int bulletsToReload = Mathf.Min(bulletsNeeded, currentAmmo);

        if (bulletsToReload > 0)
        {
            currentClipAmmo += bulletsToReload;
            currentAmmo -= bulletsToReload;
        }
    }

    public bool CanReload()
    {
        return currentClipAmmo < clipSize && currentAmmo > 0;
    }
}