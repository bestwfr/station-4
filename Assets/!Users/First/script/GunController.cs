using UnityEngine;

public class GunController : MonoBehaviour
{
    [Header("References")]
    public Transform muzzle;               // จุดที่กระสุนออก
    public GameObject projectilePrefab;    // กระสุน prefab

    [Header("Settings")]
    public float shootForce = 20f;         // แรงยิง
    public AudioClip shootSound;           // เสียงตอนยิง

    private AudioSource audioSource;

    void Start()
    {
        // เพิ่ม AudioSource ถ้ายังไม่มี
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();
    }

    void Update()
    {
        if (Input.GetMouseButtonDown(0)) // คลิกซ้าย
        {
            Shoot();
        }
    }

    void Shoot()
    {
        if (projectilePrefab == null || muzzle == null) return;

        // สร้างกระสุนที่ muzzle
        GameObject bullet = Instantiate(projectilePrefab, muzzle.position, muzzle.rotation);

        // ถ้ามี Rigidbody ให้เพิ่มแรงออกไปข้างหน้า
        Rigidbody rb = bullet.GetComponent<Rigidbody>();
        if (rb != null)
            rb.AddForce(muzzle.forward * shootForce, ForceMode.VelocityChange);

        // เล่นเสียงยิง
        if (shootSound != null)
            audioSource.PlayOneShot(shootSound);
    }
}
