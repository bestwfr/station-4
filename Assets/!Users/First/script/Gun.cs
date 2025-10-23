using UnityEngine;
using System.Collections; 

public class Gun : MonoBehaviour
{
    [Header("Gun Settings")]
    public float range = 100f;       // ระยะยิงสูงสุด
    public float damage = 10f;       // ความแรง
    public float fireRate = 1f;      // จำนวนครั้งต่อวินาที
    private float nextFireTime = 0f;
    
    // NEW: ระยะที่สามารถกด E เพื่อ Interact ได้
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
    public GameObject impactEffect;  // เอฟเฟกต์ตอนโดนเป้า (optional)

    private int playerLayerMask;

    void Start()
    {
        currentAmmo = magazineCapacity;
        reserveAmmo = 0;
        
        // คำนวณ Layer Mask เพื่อละเว้น Layer "Player" จาก Raycast
        playerLayerMask = ~LayerMask.GetMask("Player");
    }

    void Update()
    {
        // ถ้ากำลังรีโหลด ให้หยุดการทำงานทั้งหมด
        if (isReloading)
            return; 

        // 1. ตรวจสอบการยิง
        if (Input.GetButton("Fire1") && Time.time >= nextFireTime && currentAmmo > 0)
        {
            nextFireTime = Time.time + 1f / fireRate;
            Shoot();
        }
        
        // 2. ตรวจสอบการรีโหลด (กด R)
        if (Input.GetKeyDown(KeyCode.R) && currentAmmo < magazineCapacity && reserveAmmo > 0)
        {
            StartCoroutine(Reload()); 
        }
        
        // 3. ตรวจสอบการ Interact (กด E) <<< NEW
        if (Input.GetKeyDown(KeyCode.E))
        {
            Interact();
        }
    }

    void Shoot()
    {
        // ลดกระสุนที่เหลือลง 1 นัด
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

        // Raycast สำหรับการยิง
        if (Physics.Raycast(ray, out hit, range, playerLayerMask))
        {
            Target target = hit.transform.GetComponent<Target>();
            
            if (target != null)
            {
                target.TakeDamage(damage); 
            }

            if (impactEffect != null)
            {
                GameObject impact = Instantiate(impactEffect, hit.point, Quaternion.LookRotation(hit.normal));
                Destroy(impact, 2f); 
            }
        }

        Debug.DrawRay(playerCamera.transform.position, playerCamera.transform.forward * range, Color.red, 0.2f);
    }

    // ฟังก์ชันใหม่สำหรับ Interact (เก็บกระสุน) <<< NEW
    void Interact()
    {
        Ray ray = new Ray(playerCamera.transform.position, playerCamera.transform.forward);
        RaycastHit hit;
        
        // ใช้ Raycast ในระยะ InteractRange ที่กำหนดไว้
        if (Physics.Raycast(ray, out hit, interactRange, playerLayerMask))
        {
            // ตรวจสอบว่าวัตถุที่ชนมีสคริปต์ AmmoPickup หรือไม่
            AmmoPickup pickup = hit.collider.GetComponent<AmmoPickup>();
            
            if (pickup != null)
            {
                // ถ้าใช่, เรียกฟังก์ชัน Collect() ในสคริปต์ AmmoPickup และส่งตัวเอง (Gun) ไปให้
                pickup.Collect(this); 
            }
        }
    }


    IEnumerator Reload()
    {
        isReloading = true;
        Debug.Log("Reloading...");

        yield return new WaitForSeconds(reloadTime); 

        // ตรรกะการรีโหลด: ดึงกระสุนจากสำรอง (reserveAmmo)
        int ammoNeeded = magazineCapacity - currentAmmo;
        int ammoToUse = Mathf.Min(ammoNeeded, reserveAmmo); 
        
        currentAmmo += ammoToUse;
        reserveAmmo -= ammoToUse; 
        
        isReloading = false;
        Debug.Log("Reload Complete. Ammo: " + currentAmmo + " / " + reserveAmmo);
    }
    
    // ฟังก์ชันสำหรับเพิ่มกระสุนสำรอง (สำหรับใช้ใน AmmoPickup)
    public void AddReserveAmmo(int amount)
    {
        reserveAmmo += amount;
        Debug.Log("Picked up ammo. Reserve: " + reserveAmmo);
    }
}