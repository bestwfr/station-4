using UnityEngine;
using UnityEngine.AI; // <--- [1] ต้องเพิ่มบรรทัดนี้!

public class MonsterSpawnTrigger : MonoBehaviour
{
    // [2] เปลี่ยนจาก GameObject... เป็น NavMeshAgent
    // (ลาก "Enemy" มาใส่ช่องนี้ใน Inspector)
    [SerializeField] private NavMeshAgent monsterAgent; 

    [SerializeField] private float spawnDistance = 15f; 

    private bool hasTriggered = false;

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player") && !hasTriggered)
        {
            hasTriggered = true; 

            // --- คำนวณจุดเกิด (เหมือนเดิม) ---
            Vector3 spawnDirection = -other.transform.forward; 
            Vector3 spawnPosition = other.transform.position + (spawnDirection * spawnDistance);
            
            // (กันผีเกิดใต้ดิน/ลอยฟ้า) - ห้ามลืม!
            // พยายามหา "พื้น" (NavMesh) ที่ใกล้ที่สุดตรงจุดสปอน
            if (NavMesh.SamplePosition(spawnPosition, out NavMeshHit hit, 5f, NavMesh.AllAreas))
            {
                spawnPosition = hit.position;
            }
            // (ถ้าหาไม่เจอ... ก็ใช้จุดเดิม... แต่มันอาจจะลอย)

            // --- สั่งการผี (เวอร์ชันใหม่) ---
            if (monsterAgent != null)
            {
                Debug.Log("ได้เวลาสนุกแล้วสิ! (Warped)");

                // [3] นี่คือคำสั่ง "วาร์ป" ที่ถูกต้องสำหรับ AI
                monsterAgent.Warp(spawnPosition); 
                
                // (ถ้า Enemy ถูกปิด (Set Active False) ไว้... ให้เปิดมัน)
                monsterAgent.gameObject.SetActive(true); 

                // (ถ้า AI มันมี Script ที่ต้องสั่ง "เริ่มล่า" ก็เรียกตรงนี้)
                // monsterAgent.GetComponent<YourAIScript>().StartHunting(other.transform); 
            }

            // ทำลายกับดักนี้ทิ้ง
            Destroy(gameObject); 
        }
    }
}