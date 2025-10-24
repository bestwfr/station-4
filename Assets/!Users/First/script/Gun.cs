using UnityEngine;
using System.Collections; // <<< ต้องมีสำหรับ Coroutine

public class Gun : MonoBehaviour
{
    [Header("Gun Settings")]
    public float range = 100f;       // ระยะยิงสูงสุด
    public float damage = 10f;       // ความแรง
    public float fireRate = 5f;      // จำนวนครั้งต่อวินาที
    private float nextFireTime = 0f;
    
    [Header("Interaction Settings")]
    public float interactRange = 3f; 

    [Header("Ammo Settings")]
    public int magazineCapacity = 6;  // ความจุกระสุนสูงสุดต่อแม็ก (6 นัด)
    private int currentAmmo;         // กระสุนที่เหลือในแม็ก
    private int reserveAmmo = 0;     // กระสุนสำรองทั้งหมด (เริ่มต้น 0)
    public float reloadTime = 1.5f;  // เวลาที่ใช้ในการรีโหลด
    private bool isReloading = false; // สถานะกำลังรีโหลดหรือไม่

    [Header("References")]
    public Transform muzzle;         // จุดปลายปืน
    public Camera playerCamera;      // กล้องมองจากมุมมองผู้เล่น
    public ParticleSystem muzzleFlash; // เอฟเฟกต์ยิง (optional)
    
    [Header("VFX & Tracer")]
    // NEW: อ้างอิงถึง Tracer Prefab ที่มี Trail Renderer
    public TrailRenderer bulletTracerPrefab; 
    public GameObject impactEffect;  // เอฟเฟกต์ตอนโดนเป้า (optional)

    private int playerLayerMask;

    void Start()
    {
        currentAmmo = magazineCapacity;
        reserveAmmo = 0;
        playerLayerMask = ~LayerMask.GetMask("Player");
    }

    void Update()
    {
        if (isReloading)
            return; 

        if (Input.GetButton("Fire1") && Time.time >= nextFireTime && currentAmmo > 0)
        {
            nextFireTime = Time.time + 1f / fireRate;
            Shoot();
        }
        
        if (Input.GetKeyDown(KeyCode.R) && currentAmmo < magazineCapacity && reserveAmmo > 0)
        {
            StartCoroutine(Reload()); 
        }
        
        if (Input.GetKeyDown(KeyCode.E))
        {
            Interact();
        }
    }

    void Shoot()
    {
        currentAmmo--; 
        Debug.Log("Ammo: " + currentAmmo + " / " + reserveAmmo);

        // Muzzle Flash Reset Fix
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
        
        // ตัวแปรสำหรับ Coroutine
        Vector3 hitPoint;
        Vector3 hitNormal = Vector3.up;
        bool didHit = false;

        // Raycast สำหรับการยิง
        if (Physics.Raycast(ray, out hit, range, playerLayerMask))
        {
            // Raycast ชน
            hitPoint = hit.point;
            hitNormal = hit.normal;
            didHit = true;
            Debug.Log("Hit: " + hit.collider.name);
            
            // ตรวจสอบและสร้างความเสียหาย (ทำทันทีที่ชน)
            Target target = hit.transform.GetComponent<Target>();
            if (target != null)
            {
                target.TakeDamage(damage); 
            }
        }
        else
        {
            // Raycast ไม่ชน: ให้ Tracer วิ่งไปจนสุดระยะ
            hitPoint = ray.GetPoint(range);
        }
        
        // ******************** NEW: สร้างและย้าย Tracer ********************
        if (bulletTracerPrefab != null)
        {
            TrailRenderer tracer = Instantiate(
                bulletTracerPrefab, 
                muzzle.position, // เริ่มต้นที่ปลายกระบอกปืน
                Quaternion.identity
            );
            
            // ส่งข้อมูลการชนไปให้ Coroutine จัดการการเคลื่อนที่และการสร้าง Impact
            StartCoroutine(SpawnTrail(tracer, hitPoint, hitNormal, didHit));
        }
        // *******************************************************************

        Debug.DrawRay(playerCamera.transform.position, playerCamera.transform.forward * range, Color.red, 0.2f);
        
        if (currentAmmo <= 0 && !isReloading)
        {
            // ไม่มีการสั่ง Reload อัตโนมัติ (Manual Reload)
        }
    }

    // NEW: Coroutine สำหรับเคลื่อนที่เส้นกระสุน
    IEnumerator SpawnTrail(TrailRenderer trail, Vector3 hitPoint, Vector3 hitNormal, bool didHit)
    {
        // กำหนดให้ Tracer เคลื่อนที่ไปถึงจุดชนใน 0.1 วินาที (ปรับค่าได้)
        float trailTime = 0.1f; 
        float time = 0;
        Vector3 startPosition = trail.transform.position;
        
        // เคลื่อนที่ด้วย Lerp
        while (time < 1)
        {
            trail.transform.position = Vector3.Lerp(startPosition, hitPoint, time);
            time += Time.deltaTime / trailTime;
            yield return null; 
        }
        
        // 1. ตรวจสอบให้แน่ใจว่า Tracer อยู่ที่จุดชนพอดี
        trail.transform.position = hitPoint;
        
        // 2. สร้าง Impact Effect ที่จุดชน (ถ้ามีการชนจริง)
        if (didHit && impactEffect != null)
        {
            GameObject impact = Instantiate(impactEffect, hitPoint, Quaternion.LookRotation(hitNormal));
            Destroy(impact, 2f);
        }
        
        // 3. ทำลาย Tracer's GameObject หลังจากที่ Trail Renderer หายไปตามเวลาที่ตั้งไว้ (trail.time)
        Destroy(trail.gameObject, trail.time); 
    }

    // ... (ส่วน Interact, Reload, AddReserveAmmo) ...
    // ... (ฟังก์ชันเหล่านี้ใช้โค้ดเดิมที่เคยทำไว้แล้ว) ...

    void Interact()
    {
        Ray ray = new Ray(playerCamera.transform.position, playerCamera.transform.forward);
        RaycastHit hit;
        
        if (Physics.Raycast(ray, out hit, interactRange, playerLayerMask))
        {
            AmmoPickup ammoPickup = hit.collider.GetComponent<AmmoPickup>();
            
            if (ammoPickup != null)
            {
                ammoPickup.Collect(this); 
                return;
            }
            
            DoorController door = hit.collider.GetComponent<DoorController>();

            if (door != null)
            {
                door.ToggleDoor();
                return;
            }
        }
    }

    IEnumerator Reload()
    {
        isReloading = true;
        Debug.Log("Reloading...");

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