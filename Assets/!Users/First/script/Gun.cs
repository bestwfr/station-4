using UnityEngine;
using System.Collections;
using System; 

public class Gun : MonoBehaviour
{
    // ==================== Audio Settings ====================
    [Header("Audio Settings")]
    public AudioSource audioSource;
    public AudioClip shootSound;
    public AudioClip reloadSound;
    public AudioClip cockingSound;

    // ==================== Gun Settings ====================
    [Header("Gun Settings")]
    public float range = 100f;
    public float damage = 10f;
    public float fireRate = 5f;
    private float nextFireTime = 0f;

    // ==================== Interaction ====================
    [Header("Interaction Settings")]
    public float interactRange = 5f;
    public InteractionUI interactionUI;

    // ==================== Ammo Settings ====================
    [Header("Ammo Settings")]
    public int magazineCapacity = 6;
    private int currentAmmo;
    private int reserveAmmo = 0;
    public float reloadTime = 1.5f;
    private bool isReloading = false;

    // ==================== References ====================
    [Header("References")]
    public Transform muzzle;
    public Camera playerCamera;
    public ParticleSystem muzzleFlash; // << ต้องลาก Component Particle System มาใส่ตรงนี้

    // ==================== VFX & Tracer ====================
    [Header("VFX & Tracer")]
    public TrailRenderer bulletTracerPrefab;
    public GameObject impactEffect;

    // ==================== Animation ====================
    [Header("Animation")]
    public GameObject animatedGunObject;
    private Animator gunAnimator;

    private int playerLayerMask;
    private IInteractable currentInteractable; 
    
    // ----------------------------------------------------
    // START & UPDATE
    // ----------------------------------------------------
    
    void Start()
    {
        currentAmmo = magazineCapacity;
        playerLayerMask = ~LayerMask.GetMask("Player"); 

        // Initial Setup (AudioSource, Animator)
        if (audioSource == null) 
            audioSource = GetComponent<AudioSource>() ?? gameObject.AddComponent<AudioSource>();
        
        if (animatedGunObject != null)
        {
            gunAnimator = animatedGunObject.GetComponent<Animator>();
            if (gunAnimator == null)
            {
                Debug.LogError("Gun.cs: Animator component not found on the assigned Animated Gun Object!");
            }
        }
        
        if (interactionUI == null)
        {
            Debug.LogError("InteractionUI reference is missing on the Gun script!");
        }
        
        // 💡 NEW: ปิด GameObject ของ Muzzle Flash ไว้ตั้งแต่เริ่มต้น
        if (muzzleFlash != null)
        {
            muzzleFlash.gameObject.SetActive(false);
        }
    }

    void Update()
    {
        if (isReloading) return;

        // Input ยิง
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

        // Interaction
        DetectInteraction();
        if (Input.GetKeyDown(KeyCode.E) && currentInteractable != null)
        {
            currentInteractable.Interact(this);
            currentInteractable = null;
            if(interactionUI != null) interactionUI.HidePrompt();
        }
    }
    
    // ----------------------------------------------------
    // SHOOT LOGIC
    // ----------------------------------------------------
    
    void Shoot()
    {
        if (currentAmmo <= 0) return; 

        currentAmmo--;
        
        // Animation & Sound (ใช้โค้ดเดิมที่ถูกต้อง)
        if (gunAnimator != null) gunAnimator.SetTrigger("Shoot");
        if (audioSource != null && shootSound != null) audioSource.PlayOneShot(shootSound);

        
        // 🚨 การแก้ไข Muzzle Flash Logic
        if (muzzleFlash != null) 
        {
            // 1. เปิด GameObject (ถ้าปิดอยู่)
            if (!muzzleFlash.gameObject.activeInHierarchy)
            {
                muzzleFlash.gameObject.SetActive(true);
            }
            // 2. หยุดและเคลียร์สถานะก่อนเล่นใหม่ทุกครั้ง
            muzzleFlash.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            // 3. สั่งเล่น
            muzzleFlash.Play();
        }

        // Raycast Setup
        Ray ray = new Ray(playerCamera.transform.position, playerCamera.transform.forward);
        Vector3 hitPoint; 
        Vector3 hitNormal = Vector3.up;
        bool didHit = false;

        // ทำ Raycast จริง
        if (Physics.Raycast(ray, out RaycastHit hit, range, playerLayerMask))
        {
            hitPoint = hit.point;
            hitNormal = hit.normal;
            didHit = true;
            Target target = hit.transform.GetComponent<Target>();
            if (target != null) target.TakeDamage(damage); 
        }
        else
        {
            hitPoint = ray.GetPoint(range);
        }
        
        // สร้าง Tracer
        if (bulletTracerPrefab != null)
        {
            TrailRenderer tracer = Instantiate(bulletTracerPrefab, muzzle.position, Quaternion.identity);
            StartCoroutine(SpawnTrail(tracer, hitPoint, hitNormal, didHit));
        }
        
        StartCoroutine(PlayCockingSoundDelayed(0.15f)); 
    }
    
    // ----------------------------------------------------
    // UTILITY METHODS (Reload & VFX)
    // ----------------------------------------------------
    
    public void AddReserveAmmo(int amount)
    {
        reserveAmmo += amount;
        Debug.Log("Picked up ammo. Reserve: " + reserveAmmo);
    }
    
    IEnumerator Reload()
    {
        if (isReloading || currentAmmo == magazineCapacity || reserveAmmo <= 0)
            yield break;

        isReloading = true;

        // Animation & Sound (ใช้โค้ดเดิมที่ถูกต้อง)
        if (gunAnimator != null) gunAnimator.SetTrigger("Reload");
        if (audioSource != null && reloadSound != null) audioSource.PlayOneShot(reloadSound);
        
        yield return new WaitForSeconds(reloadTime);

        int bulletsToReload = magazineCapacity - currentAmmo;
        int actualReloadAmount = Mathf.Min(bulletsToReload, reserveAmmo);

        currentAmmo += actualReloadAmount;
        reserveAmmo -= actualReloadAmount;

        isReloading = false;
        
        StartCoroutine(PlayCockingSoundDelayed(0.1f)); 
    }

    IEnumerator PlayCockingSoundDelayed(float delayTime)
    {
        yield return new WaitForSeconds(delayTime);
        if (audioSource != null && cockingSound != null)
        {
            audioSource.PlayOneShot(cockingSound);
        }
    }

    IEnumerator SpawnTrail(TrailRenderer trail, Vector3 hitPoint, Vector3 hitNormal, bool didHit)
    {
        float time = 0;
        Vector3 startPosition = trail.transform.position;

        while (time < 1)
        {
            trail.transform.position = Vector3.Lerp(startPosition, hitPoint, time);
            time += Time.deltaTime / trail.time;
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
    
    private void DetectInteraction()
    {
        Ray ray = new Ray(playerCamera.transform.position, playerCamera.transform.forward);
        
        if (Physics.Raycast(ray, out RaycastHit hit, interactRange, playerLayerMask))
        {
            if (hit.collider.TryGetComponent(out IInteractable interactable))
            {
                if (currentInteractable != interactable)
                {
                    currentInteractable = interactable;
                    if(interactionUI != null)
                    {
                        interactionUI.ShowPrompt(currentInteractable.GetInteractionText());
                    }
                }
                return;
            }
        }

        if (currentInteractable != null)
        {
            currentInteractable = null;
            if(interactionUI != null)
            {
                interactionUI.HidePrompt();
            }
        }
    }
}