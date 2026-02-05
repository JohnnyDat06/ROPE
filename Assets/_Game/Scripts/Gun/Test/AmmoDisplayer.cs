using UnityEngine;
using TMPro;

public class AmmoDisplayer : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Kéo object Player (chứa script ActiveWeapon) vào đây")]
    [SerializeField] private ActiveWeapon activeWeapon;

    private TextMeshProUGUI ammoText;

    private void Awake()
    {
        ammoText = GetComponent<TextMeshProUGUI>();
        if (activeWeapon == null)
        {
            activeWeapon = FindObjectOfType<ActiveWeapon>();
        }
    }

    private void Update()
    {
        if (activeWeapon != null && activeWeapon.CurrentWeapon != null && activeWeapon.CurrentWeapon.ammoConfig != null)
        {
            var config = activeWeapon.CurrentWeapon.ammoConfig;
            ammoText.text = $"{config.currentClipAmmo} / {config.currentAmmo}";
        }
        else
        {
            ammoText.text = "";
        }
    }
}