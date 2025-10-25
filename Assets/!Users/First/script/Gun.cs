using System;
using UnityEngine;
using System.Collections;
using FirstGearGames.SmoothCameraShaker;
using KinematicCharacterController;

public class Gun : MonoBehaviour
{
    public MainCharacterController playerController;
    
    // ==================== Audio Settings ====================
    [Header("Audio Settings")]
    public AudioSource audioSource;
    public AudioClip shootSound;
    public AudioClip cockingSound; // เสียงชักกระสุนหลังยิง
    
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
    private int reserveAmmo;
    private bool isReloading = false;
    private bool canInsertBullet = false;
    private float reloadGraceTime = 0.5f; // time window to press R again
    private Coroutine reloadCoroutine;
    
    [Header("Reload Animations")]
    public string startReloadTrigger = "StartReload";
    public string insertBulletTrigger = "InsertBullet";
    public string endReloadTrigger = "EndReload";
    public float insertDelay = 0.7f;

    [Header("References")]
    public Transform muzzle;
    public Camera playerCamera;
    public ParticleSystem muzzleFlash;
    
    [Header("VFX & Tracer")]
    public TrailRenderer bulletTracerPrefab;
    public GameObject impactEffect;

    // 🚨 NEW: อ้างอิง GameObject ที่มี Animator (คือ Gunmo/Gunmodel)
    [Header("Animation")]
    public GameObject animatedGunObject; 
    private Animator gunAnimator; // ตัวแปรส่วนตัวสำหรับเก็บ Animator Component
    
    public Animator leftHandAnimator;
    
    [Header("Bullet Ejection")]
    public Transform ejectPoint;        // where the shell or bullet exits
    public GameObject bulletEjectPrefab; // prefab with a Rigidbody (shell or bullet model)
    public float ejectForce = 1.5f;      // how hard it shoots out
    public float ejectTorque = 5f;  
    
    public ShakeData shakeData;
    public ShakeData reloadShakeData;
    public GameObject light;

    public event Action OnShootImpact;
    
    private int playerLayerMask;

    void Start()
    {
        currentAmmo = magazineCapacity;
        reserveAmmo = 1000;
        playerLayerMask = ~LayerMask.GetMask("Player");
        
        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
            }
        }
        
        // 🚨 NEW: ดึง Animator Component จาก GameObject ที่ลากมาใส่ (Gunmo)
        if (animatedGunObject != null)
        {
            gunAnimator = animatedGunObject.GetComponent<Animator>();
            if (gunAnimator == null)
            {
                Debug.LogError("Gun.cs: Animator component not found on the assigned Animated Gun Object!");
            }
        }
    }

    void Update()
    {
        if (isReloading)
            return;

        if (playerController.IsSprinting)
        {
            gunAnimator.SetBool("IsRunning", true);
            leftHandAnimator.SetBool("IsHandDown", true);
            return;
        }
        gunAnimator.SetBool("IsRunning", false);
        leftHandAnimator.SetBool("IsHandDown", false);

        // Input ยิง 
        if (Input.GetButton("Fire1") && Time.time >= nextFireTime && currentAmmo > 0)
        {
            nextFireTime = Time.time + 1f / fireRate;
            Shoot();
        }
        
        if (Input.GetKeyDown(KeyCode.R) && currentAmmo < magazineCapacity && reserveAmmo > 0)
        {
            if (!isReloading)
            {
                reloadCoroutine = StartCoroutine(ReloadRoutine());
            }
            else if (canInsertBullet)
            {
                // Player pressed R again → insert one bullet immediately
                StartCoroutine(InsertSingleBullet());
            }
        }
        
        // Input สำหรับการโต้ตอบ (กด E)
        if (Input.GetKeyDown(KeyCode.E))
        {
            Interact();
        }
    }

    void Shoot()
    {
        if (currentAmmo <= 0) 
        {
            return; 
        }

        currentAmmo--;
        
        // 🚨 NEW: เรียก Animator Trigger เพื่อเริ่ม Animation Recoil
        if (gunAnimator != null)
        {
            gunAnimator.SetTrigger("Shoot"); // "Shoot" ต้องเป็นชื่อ Trigger ที่คุณตั้งใน Animator
        }
        
        EjectBullet();

        CameraShakerHandler.Shake(shakeData);
        
        OnShootImpact?.Invoke();
        
        // 1. เล่นเสียงยิงหลัก
        if (audioSource != null && shootSound != null)
        {
            audioSource.PlayOneShot(shootSound);
        }

        // Muzzle Flash
        if (muzzleFlash != null)
        {
            muzzleFlash.Stop();
            muzzleFlash.Play();
            light.SetActive(true);
        }
        
        StartCoroutine(MuzzleLightOff(0.2f)); 

        // Raycast and Tracer (เดิม)
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

    IEnumerator MuzzleLightOff(float delayTime)
    {
        yield return new WaitForSeconds(delayTime);
        
        light.SetActive(false);
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
            float impactOffset = 0.02f; // how far off the surface to spawn
            Vector3 spawnPos = hitPoint + hitNormal * impactOffset;

            GameObject impact = Instantiate(impactEffect, spawnPos, Quaternion.LookRotation(hitNormal));

            Destroy(impact, 2f);
        }
        
        Destroy(trail.gameObject, trail.time);
    }

    void Interact()
    {
        Ray ray = new Ray(playerCamera.transform.position, playerCamera.transform.forward);
        RaycastHit hit;
        
        if (Physics.Raycast(ray, out hit, interactRange, playerLayerMask))
        {
            // 1. ตรวจสอบ Dialogue Trigger (สำหรับ Dialog ปกติ)
            DialogueTrigger dialogTrigger = hit.collider.GetComponent<DialogueTrigger>();
            if (dialogTrigger != null)
            {
                if (dialogTrigger.TryInteract()) 
                {
                    return; 
                }
            }
            
            // 2. ตรวจสอบ AmmoPickup (สำหรับเก็บกระสุน)
            AmmoPickup ammoPickup = hit.collider.GetComponent<AmmoPickup>();
            if (ammoPickup != null)
            {
                // ammoPickup.Collect(this); 
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

    IEnumerator ReloadRoutine()
    {
        isReloading = true;
        
        leftHandAnimator.SetBool("IsHandDown", true);

        // Allow inserting bullets immediately
        canInsertBullet = true;

        // Start Reload Animation
        if (gunAnimator != null)
            gunAnimator.SetTrigger(startReloadTrigger);

        // Wait small delay before first automatic insert (optional)
        yield return new WaitForSeconds(0.1f);

        // First bullet insert
        if (Input.GetKey(KeyCode.R))
            yield return InsertSingleBullet();

        while (currentAmmo < magazineCapacity && reserveAmmo > 0)
        {
            // Wait for player to press R again
            float timer = 0f;
            bool pressed = false;
            while (timer < reloadGraceTime && !pressed)
            {
                if (Input.GetKeyDown(KeyCode.R) && canInsertBullet)
                {
                    pressed = true;
                    yield return InsertSingleBullet();
                }

                timer += Time.deltaTime;
                yield return null;
            }

            // Exit if no R pressed in grace time
            if (!pressed)
                break;
        }

        // End Reload Animation
        if (gunAnimator != null)
            gunAnimator.SetTrigger(endReloadTrigger);
        
        leftHandAnimator.SetBool("IsHandDown", false);

        isReloading = false;
        canInsertBullet = false;
    }

    IEnumerator InsertSingleBullet()
    {
        canInsertBullet = false; // temporarily block further inserts

        if (gunAnimator != null)
        {
            gunAnimator.ResetTrigger(insertBulletTrigger);
            gunAnimator.SetTrigger(insertBulletTrigger);
        }
        
        audioSource.PlayOneShot(cockingSound);
        
        yield return new WaitForSeconds(.1f);
        
        CameraShakerHandler.Shake(reloadShakeData);

        yield return new WaitForSeconds(insertDelay);
        
        currentAmmo++;
        reserveAmmo--;
        Debug.Log($"Inserted 1 bullet. Ammo: {currentAmmo}/{reserveAmmo}");

        // Allow next insert immediately after delay
        canInsertBullet = true;
    }
    
    void EjectBullet()
    {
        if (ejectPoint == null || bulletEjectPrefab == null)
            return;

        GameObject ejected = Instantiate(bulletEjectPrefab, ejectPoint.position, ejectPoint.rotation);

        Rigidbody rb = ejected.GetComponent<Rigidbody>();
        if (rb != null)
        {
            Vector3 ejectDir = (-ejectPoint.right + ejectPoint.up * 0.5f).normalized;
            rb.AddForce(ejectDir * ejectForce, ForceMode.Impulse);

            // Add random spin
            rb.AddTorque(UnityEngine.Random.insideUnitSphere * ejectTorque, ForceMode.Impulse);
        }

        // Destroy after 5s to clean up
        Destroy(ejected, 20f);
    }
    
    public void AddReserveAmmo(int amount)
    {
        reserveAmmo += amount;
        Debug.Log("Picked up ammo. Reserve: " + reserveAmmo);
    }
}