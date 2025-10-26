using UnityEngine;
using System.Collections;
using FirstGearGames.SmoothCameraShaker;

// กำหนดให้ต้องมี Collider Component ใน GameObject นี้
[RequireComponent(typeof(Collider))] 
public class HintTrigger : MonoBehaviour
{
    [Tooltip("ใส่ข้อความที่จะแสดงเมื่อผู้เล่นเดินเข้า Trigger")]
    [TextArea(3, 5)] // ทำให้ช่องกรอกข้อความใน Inspector ใหญ่ขึ้น
    public string hintMessage = "find a Generator Room to Turn on the electric door";

    [Tooltip("ลาก GameObject ของ InteractionHandler มาใส่")]
    public InteractionHandler interactionHandler;

    void Start()
    {
        // ตรวจสอบว่า GameObject มี Collider และตั้งค่าเป็น Is Trigger หรือยัง
        Collider col = GetComponent<Collider>();
        if (col == null)
        {
            Debug.LogError("HintTrigger: Missing Collider component!");
            return;
        }

        if (!col.isTrigger)
        {
            Debug.LogWarning("HintTrigger: Collider is not set to 'Is Trigger'. Setting it now.");
            col.isTrigger = true;
        }

        // ตรวจสอบ InteractionHandler
        if (interactionHandler == null)
        {
            // ลองหาจากซีน
            interactionHandler = FindObjectOfType<InteractionHandler>();
            if (interactionHandler == null)
            {
                Debug.LogError("HintTrigger: InteractionHandler is not assigned or found in the scene! Cannot display hint.");
            }
        }
    }

    // ฟังก์ชันที่ถูกเรียกเมื่อ Collider อื่นชน/ซ้อนทับ
    private void OnTriggerEnter(Collider other)
    {
        // ตรวจสอบว่าเป็นผู้เล่นหรือไม่ (โดยสมมติว่าผู้เล่นมี tag "Player")
        if (other.gameObject.CompareTag("Player") && interactionHandler != null)
        {
            // เรียก ShowPrompt ด้วย 2 Arguments:
            // 1. ข้อความ
            // 2. false (เพื่อไม่ให้มีคำว่า "Press E to" นำหน้า)
            interactionHandler.interactionUI?.ShowPrompt(hintMessage, false); 
        }
    }

    // ฟังก์ชันที่ถูกเรียกเมื่อ Collider อื่นออกจากขอบเขต Trigger
    private void OnTriggerExit(Collider other)
    {
        // ตรวจสอบว่าเป็นผู้เล่นหรือไม่
        if (other.gameObject.CompareTag("Player") && interactionHandler != null)
        {
            // ซ่อนข้อความ
            interactionHandler.interactionUI?.HidePrompt();
        }
    }
}