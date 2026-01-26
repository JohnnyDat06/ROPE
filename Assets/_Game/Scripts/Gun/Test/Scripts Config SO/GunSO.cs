using LlamAcademy.ImpactSystem;
using System.Collections;
using UnityEngine;
using UnityEngine.Pool;
using Random = UnityEngine.Random;

[CreateAssetMenu(fileName = "GunSO", menuName = "Guns/GunSO", order = 1)]
public class GunSO : ScriptableObject
{
    public ImpactType impactType;
    public GunType type;
    public new string name;
    public GameObject modelPrefab;
    public Vector3 spawnPoint;
    public Vector3 spawnRotation;

    public AmmoConfigSO ammoConfig;
    public DamgeConfigSO damageConfig;
    public ShootConfigSO shootConfig;
    public TrailConfigSO trailConfig;
    public AudioConfigSO audioConfig;

    private MonoBehaviour activeMonobihaviour;
    private GameObject model;
    private AudioSource shootingAudioSource;
    private float lastShootTime;

    private float initialClickTime;
    private float stopShootingTime;
    private bool lastFrameWantedToShoot;

    private ParticleSystem shootSystem;
    private ObjectPool<TrailRenderer> trailPool;

    public void Spawn(Transform parent, MonoBehaviour activeMonobihaviour)
    {
        this.activeMonobihaviour = activeMonobihaviour;
        lastShootTime = 0;
        trailPool = new ObjectPool<TrailRenderer>(CreateTrail);

        model = Instantiate(modelPrefab);
        model.transform.SetParent(parent, false);
        model.transform.localPosition = spawnPoint;
        model.transform.localRotation = Quaternion.Euler(spawnRotation);

        shootSystem = model.GetComponentInChildren<ParticleSystem>();
        shootingAudioSource = model.AddComponent<AudioSource>();
    }

    public void TryToShoot()
    {
        //if (ammoConfig.currentClipAmmo <= 0) return;
        if (Time.time - lastShootTime - shootConfig.fireRate > Time.deltaTime)
        {
            float lastDuration = Mathf.Clamp(0, (stopShootingTime - initialClickTime), shootConfig.maxSpreadTime);
            float lerpTime = (shootConfig.recoilRecoverySpeed) - (Time.time - stopShootingTime) / shootConfig.recoilRecoverySpeed;

            initialClickTime = Time.time - Mathf.Lerp(0, lastDuration, Mathf.Clamp01(lerpTime));
        }

        if (Time.time > shootConfig.fireRate + lastShootTime)
        {
            lastShootTime = Time.time;

            if (ammoConfig.currentClipAmmo == 0)
            {
                audioConfig.PlayOutAmmoClip(shootingAudioSource);
                return;
            }

            shootSystem.Play(); // play muzzle, smoke,... particle effect
            audioConfig.PlayShootingClip(shootingAudioSource, ammoConfig.currentClipAmmo == 1);

            Vector3 spreadAmount = shootConfig.GetSpread(Time.time - initialClickTime);
            model.transform.forward += model.transform.TransformDirection(spreadAmount);

            Vector3 shootDirection = model.transform.forward;

            // minus one ammo
            ammoConfig.currentClipAmmo--;

            if (Physics.Raycast(shootSystem.transform.position, shootDirection,
                out RaycastHit hit, float.MaxValue, shootConfig.hitMask))
            {
                activeMonobihaviour.StartCoroutine(PlayTrail(shootSystem.transform.position, hit.point, hit));
            }
            else
            {
                activeMonobihaviour.StartCoroutine(PlayTrail(shootSystem.transform.position,
                    shootSystem.transform.position + (shootDirection * trailConfig.missDistance),
                    new RaycastHit()));
            }
        }
    }

    public void StartReloading() => audioConfig.PlayReloadClip(shootingAudioSource);
    public void EndReload() => ammoConfig.Reload();
    public bool CanReload() => ammoConfig.CanReload();

    public void Tick(bool wantsToShoot)
    {
        model.transform.localRotation = Quaternion.Lerp(
        model.transform.localRotation,
        Quaternion.Euler(spawnRotation),
        Time.deltaTime * shootConfig.recoilRecoverySpeed
    );

        if (wantsToShoot)
        {
            lastFrameWantedToShoot = true;
            TryToShoot();
        }
        else if (!wantsToShoot && lastFrameWantedToShoot)
        {
            stopShootingTime = Time.time;
            lastFrameWantedToShoot = false;
        }
    }

    private IEnumerator PlayTrail(Vector3 startPoint, Vector3 endPoint, RaycastHit hit)
    {
        TrailRenderer instance = trailPool.Get();
        instance.gameObject.SetActive(true);
        instance.transform.position = startPoint;
        yield return null; // void position carry-over from last if reused

        instance.emitting = true;

        float distance = Vector3.Distance(startPoint, endPoint);
        float remainingDistance = distance;
        while (remainingDistance > 0f)
        {
            instance.transform.position = Vector3.Lerp(startPoint, endPoint,
                Mathf.Clamp01(1 - (remainingDistance / distance)));
            remainingDistance -= trailConfig.simualtionSpeed * Time.deltaTime;

            yield return null;
        }

        instance.transform.position = endPoint;

        if (hit.collider != null)
        {
            SurfaceManager.Instance.HandleImpact(hit.transform.gameObject, endPoint,
                hit.normal, impactType, 0);

            // Link to IDamageable and apply damage
            if (hit.collider.TryGetComponent<IDamageable>(out IDamageable damageable))
            {
                damageable.TakeDamage(damageConfig.GetDamage());
            }
        }

        yield return new WaitForSeconds(trailConfig.duration);
        yield return null; // wait a frame to avoid trail disappearing instantly when reused
        instance.emitting = false;
        instance.gameObject.SetActive(false);
        trailPool.Release(instance);
    }

    private TrailRenderer CreateTrail()
    {
        GameObject instance = new GameObject("Bullet Trail");
        TrailRenderer trail = instance.AddComponent<TrailRenderer>();
        trail.colorGradient = trailConfig.color;
        trail.material = trailConfig.material;
        trail.widthCurve = trailConfig.widthCurve;
        trail.time = trailConfig.duration;
        trail.minVertexDistance = trailConfig.minVertexDistance;

        trail.emitting = false;
        trail.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

        return trail;
    }
}
