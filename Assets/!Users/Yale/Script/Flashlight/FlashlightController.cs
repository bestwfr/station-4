using UnityEngine;

public class FlashlightController : MonoBehaviour
{
    public Light flashlight; 
    private bool isFlashlightOn = false;

    [Header("Normal Mode Settings")]
    public float normalIntensity = 2.0f;
    public float normalSpotAngle = 45.0f;

    [Header("High Beam Mode Settings")]
    public float highBeamIntensity = 8.0f;
    public float highBeamSpotAngle = 15.0f;

    [Header("Enemy Interaction")] // ส่วนใหม่สำหรับเชื่อมต่อกับ AI
    [Tooltip("ระยะสูงสุดที่ไฟฉาย High Beam จะส่งผลกระทบต่อศัตรู")]
    public float highBeamMaxDistance = 15.0f; 
    [Tooltip("Layer ของศัตรูที่ต้องการให้ Raycast ตรวจจับ")]
    public LayerMask enemyLayer; 
    
    [Header("Battery System")]
    public float maxBattery = 100.0f;
    public float currentBattery;
    public float normalDrainRate = 0.5f;
    public float highBeamDrainRate = 2.0f;

    [Header("Audio Settings")]
    public AudioClip soundFlashlightOn;
    public AudioClip soundFlashlightOff;
    public AudioClip soundBatteryDead;
    
    private AudioSource audioSource;

    void Start()
    {
        if (flashlight != null)
        {
            flashlight.enabled = false;
            isFlashlightOn = false;
            flashlight.intensity = normalIntensity;
            flashlight.spotAngle = normalSpotAngle;
        }
        currentBattery = maxBattery;
        
        // (ใช้ GetComponent เพราะสคริปต์นี้อยู่บน Player ที่มี AudioSource)
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            Debug.LogError("FlashlightController: หา AudioSource ไม่เจอ! อย่าลืมแปะไว้ที่ Player นะ");
        }
    }

    void Update()
    {
        if (isFlashlightOn)
        {
            if (currentBattery <= 0)
            {
                currentBattery = 0;
                isFlashlightOn = false;
                flashlight.enabled = false;
                PlaySound(soundFlashlightOff);
                Debug.Log("แบตไฟฉายหมด!! (Battery Dead!)");
                return;
            }
            
            // (เช็กปุ่ม "Fire2" (คลิกขวา) สำหรับ High Beam)
            if (Input.GetButton("Fire2")) 
            {
                // *** NEW: High Beam Cone Logic ***
                flashlight.intensity = highBeamIntensity;
                flashlight.spotAngle = highBeamSpotAngle;
                currentBattery -= highBeamDrainRate * Time.deltaTime;

                // 1. ตรวจจับศัตรูทั้งหมดในระยะสูงสุดด้วย OverlapSphere
                Collider[] hitColliders = Physics.OverlapSphere(flashlight.transform.position, highBeamMaxDistance, enemyLayer);
                float halfAngle = highBeamSpotAngle / 2f;
                Vector3 flashlightOrigin = flashlight.transform.position;
                Vector3 flashlightForward = flashlight.transform.forward;

                foreach (var hitCollider in hitColliders)
                {
                    EnemyAI enemyAI = hitCollider.GetComponent<EnemyAI>();
                    if (enemyAI != null)
                    {
                        Vector3 directionToEnemy = (hitCollider.transform.position - flashlightOrigin).normalized;
                        
                        // 2. เช็กมุม: ศัตรูอยู่ในกรวยแสงหรือไม่
                        if (Vector3.Angle(flashlightForward, directionToEnemy) < halfAngle)
                        {
                            // 3. เช็ก Line of Sight (LOS): ต้องไม่มีสิ่งกีดขวางกั้น
                            RaycastHit hit;
                            // ยิง Raycast ตรงไปยังศัตรูเพื่อดูว่ามีวัตถุอื่นมาบังหรือไม่
                            if (Physics.Raycast(flashlightOrigin, directionToEnemy, out hit, highBeamMaxDistance))
                            {
                                // 4. ถ้าวัตถุแรกที่ Raycast โดนคือ Collider ของศัตรู
                                if (hit.collider == hitCollider)
                                {
                                    // Hit confirmed! 
                                    Debug.Log($"High Beam Hit Enemy ({hitCollider.name})! Initiating Retreat via Cone Check.");
                                    // 5. เรียกฟังก์ชัน OnFlashlightHit บนศัตรู
                                    enemyAI.OnFlashlightHit(hit.point); 
                                }
                            }
                        }
                    }
                }
                // **********************************
            }
            else
            {
                // Normal Mode Drain
                flashlight.intensity = normalIntensity;
                flashlight.spotAngle = normalSpotAngle;
                currentBattery -= normalDrainRate * Time.deltaTime;
            }
        }
    }

    // --- ฟังก์ชันสำหรับ Debug Gizmos ใน Scene View ---
    void OnDrawGizmosSelected()
    {
        if (flashlight == null) return;
        
        Vector3 origin = flashlight.transform.position;
        Vector3 forward = flashlight.transform.forward;
        float maxDistance = highBeamMaxDistance;
        float spotAngle = highBeamSpotAngle;
        float halfAngle = spotAngle / 2f;

        // สีสำหรับ Gizmo
        Gizmos.color = new Color(1f, 1f, 0f, 0.7f); // สีเหลืองโปร่งแสง
        
        // 1. วาดขอบเขตสูงสุดด้วย WireSphere เพื่อแสดงระยะ OverlapSphere
        Gizmos.DrawWireSphere(origin, 0.1f); 

        // 2. วาดขอบเขตกรวยแสง (Horizontal & Vertical)
        
        // Horizontal boundaries (ซ้าย-ขวา)
        // ใช้ Quaternion.AngleAxis หมุนเวกเตอร์ไปตามแกนตั้ง (Vector3.up)
        Vector3 rightBoundary = Quaternion.AngleAxis(halfAngle, Vector3.up) * forward;
        Vector3 leftBoundary = Quaternion.AngleAxis(-halfAngle, Vector3.up) * forward;
        
        // Vertical boundaries (บน-ล่าง)
        // ใช้ Quaternion.AngleAxis หมุนเวกเตอร์ไปตามแกนขวา (flashlight.transform.right)
        Vector3 upBoundary = Quaternion.AngleAxis(-halfAngle, flashlight.transform.right) * forward;
        Vector3 downBoundary = Quaternion.AngleAxis(halfAngle, flashlight.transform.right) * forward;

        // วาดเส้นขอบเขต
        Gizmos.DrawRay(origin, rightBoundary * maxDistance);
        Gizmos.DrawRay(origin, leftBoundary * maxDistance);
        Gizmos.DrawRay(origin, upBoundary * maxDistance);
        Gizmos.DrawRay(origin, downBoundary * maxDistance);

        // 3. วาดจุดกึ่งกลางของกรวย (สีแดง)
        Gizmos.color = Color.red; 
        Gizmos.DrawRay(origin, forward * maxDistance);
        
        // 4. วาดจุดที่ปลายกรวยเพื่อแสดงขีดจำกัด
        Gizmos.color = Color.yellow; 
        Gizmos.DrawWireSphere(origin + forward * maxDistance, 0.5f); 
    }

    // --- ฟังก์ชันสำหรับให้ EquipmentManager เรียก (คืนค่า bool) ---
    public bool ToggleFlashlight()
    {
        if (flashlight == null) return false;

        isFlashlightOn = !isFlashlightOn;

        if (isFlashlightOn) // "กำลังจะเปิด"
        {
            if (currentBattery > 0)
            {
                flashlight.enabled = true;
                flashlight.intensity = normalIntensity;
                flashlight.spotAngle = normalSpotAngle;
                PlaySound(soundFlashlightOn);
                return true; // <-- "ส่งคำตอบกลับ" ว่าเพิ่งเปิด
            }
            else
            {
                isFlashlightOn = false; 
                flashlight.enabled = false;
                PlaySound(soundBatteryDead);
                return false; // <-- "ส่งคำตอบกลับ" ว่าเปิดไม่ติด
            }
        }
        else // "กำลังจะปิด"
        {
            flashlight.enabled = false;
            PlaySound(soundFlashlightOff);
            return false; // <-- "ส่งคำตอบกลับ" ว่าเพิ่งปิด
        }
    }

    // --- ฟังก์ชันสำหรับ "บังคับปิด" (สั่งจากข้างนอก) ---
    public void ForceFlashlightOff()
    {
        if (isFlashlightOn) // <<<--- เช็กก่อนว่ามันเปิดอยู่มั้ย
        {
            isFlashlightOn = false;
            flashlight.enabled = false;
            PlaySound(soundFlashlightOff);
        }
    }

    // --- ฟังก์ชันเล่นเสียง ---
    void PlaySound(AudioClip clip)
    {
        if (audioSource != null && clip != null)
        {
            audioSource.PlayOneShot(clip);
        }
    }

    // --- ฟังก์ชันเติมแบต ---
    public void AddBattery(float amount)
    {
        currentBattery += amount;
        if (currentBattery > maxBattery)
        {
            currentBattery = maxBattery;
        }
        Debug.Log($"เติมแบตไฟฉายแล้ว! ปัจจุบัน: {currentBattery}");
    }
}
