using UnityEngine;
using System.Collections;

public class Gun : MonoBehaviour
{
    // ==================== Audio Settings ====================
    [Header("Audio Settings")]
    public AudioSource audioSource;
    public AudioClip shootSound;
    public AudioClip reloadSound;
    
    // ... (โค้ด Gun Settings, Ammo Settings, References, VFX & Tracer เดิม) ...
    [Header("Gun Settings")]
    public float range = 100f;
    public float damage = 10f;
    public float fireRate = 5f;
    private float nextFireTime = 0f;
    
    [Header("Interaction Settings")]
    public float interactRange = 5f;

    [Header("Ammo Settings")]
    public int magazineCapacity = 6;
    private int currentAmmo;
    private int reserveAmmo = 0;
    public float reloadTime = 1.5f;
    private bool isReloading = false;

    [Header("References")]
    public Transform muzzle;
    public Camera playerCamera;
    public ParticleSystem muzzleFlash;
    
    [Header("VFX & Tracer")]
    public TrailRenderer bulletTracerPrefab;
    public GameObject impactEffect;

    private int playerLayerMask;

    void Start()
    {
        currentAmmo = magazineCapacity;
        reserveAmmo = 0;
        // LayerMask ที่ไม่รวม Layer "Player" เพื่อไม่ให้ Raycast ชนตัวผู้เล่นเอง
        playerLayerMask = ~LayerMask.GetMask("Player");
        
        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
            }
        }
    }

    void Update()
    {
        if (isReloading)
            return;

        // Input ยิง (Fire1 คือปุ่มซ้ายของเมาส์โดยค่าเริ่มต้น)
        if (Input.GetButton("Fire1") && Time.time >= nextFireTime && currentAmmo > 0)
        {
            nextFireTime = Time.time + 1f / fireRate;
            Shoot();
        }
        
        // Input รีโหลด
        if (Input.GetKeyDown(KeyCode.R) && currentAmmo < magazineCapacity && reserveAmmo > 0)
        {
            StartCoroutine(Reload());
        }
        
        // 🚨 Input สำหรับการโต้ตอบ (กด E)
        if (Input.GetKeyDown(KeyCode.E))
        {
            Interact();
        }
    }

    void Shoot()
    {
        // ... (โค้ด Shoot, Raycast, VFX, Tracer เดิม) ...
        if (currentAmmo <= 0) 
        {
            return; 
        }

        currentAmmo--;
        Debug.Log("Ammo: " + currentAmmo + " / " + reserveAmmo);

        if (audioSource != null && shootSound != null)
        {
            audioSource.PlayOneShot(shootSound);
        }

        if (muzzleFlash != null)
        {
            if (!muzzleFlash.gameObject.activeSelf)
            {
                muzzleFlash.gameObject.SetActive(true);
            }
            
            muzzleFlash.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            muzzleFlash.Play();
        }

        Ray ray = new Ray(playerCamera.transform.position, playerCamera.transform.forward);
        RaycastHit hit;
        
        Vector3 hitPoint;
        Vector3 hitNormal = Vector3.up;
        bool didHit = false;

        if (Physics.Raycast(ray, out hit, range, playerLayerMask))
        {
            hitPoint = hit.point;
            hitNormal = hit.normal;
            didHit = true;
            Debug.Log("Hit: " + hit.collider.name);
            
            Target target = hit.transform.GetComponent<Target>();
            if (target != null)
            {
                target.TakeDamage(damage); 
            }
        }
        else
        {
            hitPoint = ray.GetPoint(range);
        }
        
        if (bulletTracerPrefab != null)
        {
            TrailRenderer tracer = Instantiate(
                bulletTracerPrefab, 
                muzzle.position,
                Quaternion.identity
            );
            StartCoroutine(SpawnTrail(tracer, hitPoint, hitNormal, didHit));
        }
    }

    IEnumerator SpawnTrail(TrailRenderer trail, Vector3 hitPoint, Vector3 hitNormal, bool didHit)
    {
        float trailTime = 0.1f;
        float time = 0;
        Vector3 startPosition = trail.transform.position;
        
        while (time < 1)
        {
            trail.transform.position = Vector3.Lerp(startPosition, hitPoint, time);
            time += Time.deltaTime / trailTime;
            yield return null;
        }
        
        trail.transform.position = hitPoint;
        
        if (didHit && impactEffect != null)
        {
            GameObject impact = Instantiate(impactEffect, hitPoint, Quaternion.LookRotation(hitNormal));
            Destroy(impact, 2f);
        }
        
        Destroy(trail.gameObject, trail.time);
    }

// ในสคริปต์ Gun.cs (ส่วน Interact)

void Interact()
{
    Ray ray = new Ray(playerCamera.transform.position, playerCamera.transform.forward);
    RaycastHit hit;
    
    if (Physics.Raycast(ray, out hit, interactRange, playerLayerMask))
    {
        // 1. ตรวจสอบ Dialogue Trigger (สำหรับ Dialog ปกติ - ยังคงอยู่)
        DialogueTrigger dialogTrigger = hit.collider.GetComponent<DialogueTrigger>();
        if (dialogTrigger != null)
        {
            if (dialogTrigger.TryInteract()) 
            {
                return; 
            }
        }
        
        // 🚨 2. ตรวจสอบ AmmoPickup (เรียก Collect() ตรงๆ)
        AmmoPickup ammoPickup = hit.collider.GetComponent<AmmoPickup>();
        if (ammoPickup != null)
        {
            // เรียก Collect() ตรงๆ โดยไม่ต้องผ่าน TryInteract()
            ammoPickup.Collect(this); 
            return;
        }
        
        // 3. ตรวจสอบ DoorController
        DoorController door = hit.collider.GetComponent<DoorController>();
        if (door != null)
        {
            door.ToggleDoor();
            return;
        }
    }
}

// ... (โค้ดส่วนอื่น ๆ ของ Gun.cs) ...
    IEnumerator Reload()
    {
        isReloading = true;
        Debug.Log("Reloading...");

        if (audioSource != null && reloadSound != null)
        {
            audioSource.PlayOneShot(reloadSound);
        }

        yield return new WaitForSeconds(reloadTime); 

        int ammoNeeded = magazineCapacity - currentAmmo;
        int ammoToUse = Mathf.Min(ammoNeeded, reserveAmmo);
        
        currentAmmo += ammoToUse;
        reserveAmmo -= ammoToUse;
        
        isReloading = false;
        Debug.Log("Reload Complete. Ammo: " + currentAmmo + " / " + reserveAmmo);
    }
    
    public void AddReserveAmmo(int amount)
    {
        reserveAmmo += amount;
        Debug.Log("Picked up ammo. Reserve: " + reserveAmmo);
    }
}