using StarterAssets;
using UnityEngine;
using UnityEngine.InputSystem;

[DisallowMultipleComponent]
public class PlayerAction : MonoBehaviour
{
    [SerializeField] PlayerGunSelector gunSelector;
    private StarterAssetsInputs starterAssetsInputs;

    private void Awake()
    {
        starterAssetsInputs = GetComponent<StarterAssetsInputs>();
    }

    private void Update()
    {
        if (gunSelector.activeGun != null)
        {
            gunSelector.activeGun.Tick(starterAssetsInputs.shoot);
        }
    }
}
