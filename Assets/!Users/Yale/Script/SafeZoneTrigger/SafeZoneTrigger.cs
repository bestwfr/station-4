using UnityEngine;

public class SafeZoneTrigger : MonoBehaviour
{
    // [1] ลาก "ผี" มาใส่ช่องนี้
    [SerializeField] private GameObject monsterToDeactivate;

    private bool hasTriggered = false; // กันทำงานซ้ำ

    void OnTriggerEnter(Collider other)
    {
        // เช็กว่าใช่ "Player" รึเปล่า... และยังไม่เคยทำงาน
        if (other.CompareTag("Player") && !hasTriggered)
        {
            hasTriggered = true; // ทำงานแล้ว!

            // เช็กว่าลากผีมาใส่รึยัง
            if (monsterToDeactivate != null)
            {
                // สั่ง "ปิด" (Deactivate) ผีซะ!
                monsterToDeactivate.SetActive(false);
                Debug.Log("Player entered safe zone. Monster is deactivated.");
            }

            // (Optional) ทำลายตัวเองทิ้ง... จะได้ไม่รันอีก
            Destroy(gameObject); 
        }
    }
}