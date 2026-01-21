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
        if (starterAssetsInputs.shoot && gunSelector.activeGun != null)
        {
            gunSelector.activeGun.Shoot();
        }
    }
}
