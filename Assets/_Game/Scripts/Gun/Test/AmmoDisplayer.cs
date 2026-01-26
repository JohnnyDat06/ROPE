using UnityEngine;
using TMPro;

public class AmmoDisplayer : MonoBehaviour
{
    [SerializeField] PlayerGunSelector gunSelector;
    TextMeshProUGUI ammoText;

    private void Awake()
    {
        ammoText = GetComponent<TextMeshProUGUI>();
    }

    private void Update()
    {
        ammoText.text = ($"{gunSelector.activeGun.ammoConfig.currentClipAmmo} / " + 
            $"{gunSelector.activeGun.ammoConfig.currentAmmo}");
    }
}
