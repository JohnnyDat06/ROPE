using NUnit.Framework;
using System;
using System.Collections.Generic;
using UnityEngine;

public class RaycastWeapon : MonoBehaviour
{
    public class Bullet
    {
        public float time;
        public Vector3 inititalPosition;
        public Vector3 inititalVelocity;
        public TrailRenderer tracer;
    }

    public bool isFiring = false;
    public int fireRate = 25;
    public float bulletSpeed = 100f;
    public float bulletDrop = 0f;

    [SerializeField] private ParticleSystem[] muzzleFlash;
    [SerializeField] private ParticleSystem hitEffect;
    [SerializeField] private Transform raycastOrigin;
    [SerializeField] public Transform raycastDestination;

    [SerializeField] private TrailRenderer tracerEffect;
    [SerializeField] private LayerMask bulletLayerMask;

    [SerializeField] public AnimationClip weaponAnimation;

    Ray ray;
    RaycastHit hitInfo;
    float accumulatedTime;
    float maxLifeTime = 3f;
    List<Bullet> bullets = new List<Bullet>();

    Vector3 GetPosition(Bullet bullet)
    {
        // p + vt + 0.5gt^2
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
        bullet.tracer.AddPosition(position);
        return bullet;
    }

    public void StartFiring()
    {
        isFiring = true;
        accumulatedTime = 0f;
        FireBullet();
    }

    public void UpdateFiring(float deltaTime)
    {
        accumulatedTime += deltaTime;
        float fireInterval = 1f / fireRate;
        while (accumulatedTime >= 0f)
        {
            FireBullet();
            accumulatedTime -= fireInterval;
        }
    }

    public void UpdateBullets(float deltaTime)
    {
        SimulateBullets(deltaTime);
        DestroyBullets();
    }

    private void DestroyBullets()
    {
        bullets.RemoveAll(bullet => {
            if (bullet.time >= maxLifeTime)
            {
                if (bullet.tracer != null) Destroy(bullet.tracer.gameObject);
                return true;
            }
            return false;
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
            hitEffect.transform.position = hitInfo.point;
            hitEffect.transform.forward = hitInfo.normal;
            hitEffect.Emit(1);

            bullet.tracer.transform.position = hitInfo.point;
            bullet.time = maxLifeTime;
        }
        else
            bullet.tracer.transform.position = end;
    }

    private void FireBullet()
    {
        foreach (var particleSystem in muzzleFlash)
            particleSystem.Emit(1);

        Vector3 velocity = (raycastDestination.position - raycastOrigin.position).normalized * bulletSpeed;
        var bullet = CreateBullet(raycastOrigin.position, velocity);
        bullets.Add(bullet);

    }

    public void StopFiring()
    {
        isFiring = false;
    }
}
