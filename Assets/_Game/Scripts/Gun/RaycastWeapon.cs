using System.Collections.Generic;
using UnityEngine;
using LlamAcademy.ImpactSystem;

[RequireComponent(typeof(AudioSource))]
public class RaycastWeapon : MonoBehaviour
{
    public enum HolsterLocation { Back, Hip }

    public class Bullet
    {
        public float time;
        public Vector3 inititalPosition;
        public Vector3 inititalVelocity;
        public TrailRenderer tracer;
    }

    [Header("State")]
    public bool isFiring = false;
    public bool isReloading = false; // Trạng thái đang nạp đạn

    [Header("Weapon Stats")]
    public int fireRate = 25;
    public float bulletSpeed = 100f;
    public float bulletDrop = 0f;

    [Header("Effects & Configs")]
    [SerializeField] private ParticleSystem[] muzzleFlash;
    [SerializeField] private Transform raycastOrigin;
    [SerializeField] public Transform raycastDestination;
    [SerializeField] private TrailRenderer tracerEffect;
    [SerializeField] private LayerMask bulletLayerMask;

    [Header("Configurations")]
    [SerializeField] public AmmoConfigSO ammoConfig;      // 1. Config Đạn (Kéo SO vào đây)
    [SerializeField] private AudioConfigSO audioConfig;
    [SerializeField] private DamgeConfigSO damageConfig;
    [SerializeField] private ImpactType impactType;

    [SerializeField] public string weaponName;
    [SerializeField] public AnimationClip weaponAnimation;

    public HolsterLocation holsterLocation;
    public WeaponRecoil recoil;

    Ray ray;
    RaycastHit hitInfo;
    float accumulatedTime;
    float maxLifeTime = 3f;
    List<Bullet> bullets = new List<Bullet>();
    private AudioSource audioSource;

    private void Awake()
    {
        recoil = GetComponent<WeaponRecoil>();
        audioSource = GetComponent<AudioSource>();
    }

    public void StartFiring()
    {
        if (isReloading || (ammoConfig != null && ammoConfig.currentClipAmmo <= 0))
        {
            isFiring = false;
            if (ammoConfig != null && ammoConfig.currentClipAmmo <= 0)
            {
                if (audioConfig) audioConfig.PlayOutAmmoClip(audioSource);
            }
            return;
        }

        isFiring = true;
        accumulatedTime = 0f;
        //FireBullet();
    }

    public void UpdateFiring(float deltaTime)
    {
        if (ammoConfig != null && ammoConfig.currentClipAmmo <= 0)
        {
            StopFiring();
            return;
        }

        accumulatedTime += deltaTime;
        float fireInterval = 1f / fireRate;
        while (accumulatedTime >= 0f)
        {
            FireBullet();
            accumulatedTime -= fireInterval;
        }
    }

    private void FireBullet()
    {
        if (ammoConfig == null) return;
        if (ammoConfig.currentClipAmmo <= 0) return;

        ammoConfig.currentClipAmmo--;

        foreach (var particleSystem in muzzleFlash) particleSystem.Emit(1);

        if (audioConfig != null && audioSource != null)
        {
            bool isLastBullet = ammoConfig.currentClipAmmo == 0;
            audioConfig.PlayShootingClip(audioSource, isLastBullet);
        }

        if (recoil != null) recoil.GenerateRecoil(weaponName);

        Vector3 velocity = (raycastDestination.position - raycastOrigin.position).normalized * bulletSpeed;
        var bullet = CreateBullet(raycastOrigin.position, velocity);
        bullets.Add(bullet);
    }

    public void StopFiring()
    {
        isFiring = false;
    }

    public bool CanReload()
    {
        return ammoConfig != null && ammoConfig.CanReload() && !isReloading;
    }


    public void StartReload()
    {
        isReloading = true;
        isFiring = false;
        if (audioConfig) audioConfig.PlayReloadClip(audioSource);
    }

    public void RefillAmmo()
    {
        if (ammoConfig != null)
        {
            ammoConfig.Reload();
        }
        isReloading = false;
    }


    Vector3 GetPosition(Bullet bullet)
    { 
        Vector3 gravity = Vector3.down * bulletDrop;
        return ((bullet.inititalPosition) + (bullet.inititalVelocity * bullet.time) + (0.5f * gravity * bullet.time * bullet.time)); 
    }
    Bullet CreateBullet(Vector3 position, Vector3 velocity)
    { 
        Bullet bullet = new Bullet();
        bullet.inititalPosition = position;
        bullet.inititalVelocity = velocity;
        bullet.time = 0f;
        bullet.tracer = Instantiate(tracerEffect, position, Quaternion.identity);
        bullet.tracer.AddPosition(position); return bullet; 
    }
    public void UpdateBullets(float deltaTime)
    { 
        SimulateBullets(deltaTime);
        DestroyBullets(); 
    }
    private void DestroyBullets()
    { 
        bullets.RemoveAll(bullet => 
        { if (bullet.time >= maxLifeTime) 
            { 
                if (bullet.tracer != null) Destroy(bullet.tracer.gameObject);
                return true; 
            } return false; 
        }); 
    }
    private void SimulateBullets(float deltaTime)
    { 
        bullets.ForEach(bullet => 
        { 
            Vector3 p0 = GetPosition(bullet); 
            bullet.time += deltaTime;
            Vector3 p1 = GetPosition(bullet);
            RaycastSegment(p0, p1, bullet); 
        }); 
    }
    private void RaycastSegment(Vector3 start, Vector3 end, Bullet bullet)
    {
        if (bullet.tracer == null) return;
        Vector3 direction = end - start;
        float distance = direction.magnitude;
        ray.origin = start;
        ray.direction = end - start;
        if (Physics.Raycast(ray, out hitInfo, distance, bulletLayerMask))
        {
            if (SurfaceManager.Instance != null)
            {
                SurfaceManager.Instance.HandleImpact(hitInfo.transform.gameObject, hitInfo.point, hitInfo.normal, impactType, 0);
            }

            if (damageConfig != null && hitInfo.collider.TryGetComponent<IDamageable>(out IDamageable damageable))
                damageable.TakeDamage(damageConfig.GetDamage(hitInfo.distance));

            bullet.tracer.transform.position = hitInfo.point; bullet.time = maxLifeTime;
        }
        else bullet.tracer.transform.position = end;
    }
}