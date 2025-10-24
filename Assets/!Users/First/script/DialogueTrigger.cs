using UnityEngine;
using System.Collections;
using TMPro; // 🚨 Need this for TextMeshProUGUI

public class DialogueTrigger : MonoBehaviour
{
    // 🚨 NEW: ลาก Text Component 'New_text' มาใส่ตรงนี้ใน Inspector
    [Header("Text Source")]
    public TextMeshProUGUI externalTextSource; 
    
    [Header("Settings")]
    public string playerTag = "Player";
    public bool oneTimeUse = false; 
    
    private DialogueManager manager;
    private bool playerInZone = false;
    private bool used = false;

    void Start()
    {
        manager = FindAnyObjectByType<DialogueManager>();
        
        if (manager == null)
        {
            Debug.LogError("DialogueManager not found! Cannot manage dialogue.");
            enabled = false;
        }
    }
// ในสคริปต์ DialogueTrigger.cs
// ในสคริปต์ DialogueTrigger.cs ในฟังก์ชัน OnTriggerEnter

private void OnTriggerEnter(Collider other)
{
    // ตรวจสอบ: ชน Player, ยังไม่เคยใช้, Dialog ต้องไม่ Active อยู่
    if (other.CompareTag(playerTag) && !used && !manager.IsDialogueActive && externalTextSource != null)
    {
        // 🚨 NEW: การตรวจสอบความพร้อมของ GameObject 
        // ถ้า Collider ที่ชนเป็นส่วนหนึ่งของ Player (other) ไม่ active, ให้ข้าม
        if (!other.gameObject.activeInHierarchy) 
        {
             return;
        }

        playerInZone = true;
        
        // 🚨 NEW: ใช้ Coroutine หน่วงเวลาการเปิด Dialog
        StartCoroutine(DelayedOpen(externalTextSource.text));
    }
}
// ...
// Coroutine ที่หน่วงเวลาการเปิด Dialog
IEnumerator DelayedOpen(string sentence)
{
    // รอก่อน 1 เฟรม
    yield return null; 
    
    // ตรวจสอบสถานะอีกครั้งก่อนเปิด Dialog
    // และตรวจสอบว่าผู้เล่นยังอยู่ในโซน (ไม่ได้ชนแล้วเด้งออกไปทันที)
    if (playerInZone && !manager.IsDialogueActive)
    {
        manager.StartDialogue(sentence);
        StartCoroutine(WaitForDialogueEndAndFinalize());
    }
}

// ... (โค้ดส่วนอื่น ๆ ของ DialogueTrigger.cs) ...
    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag(playerTag))
        {
            playerInZone = false;
            
            // ถ้าผู้เล่นเดินออกขณะที่ Dialog เปิดอยู่ ให้ปิด Dialog ทันที
            if (manager.IsDialogueActive)
            {
                manager.EndDialogue();
                StopAllCoroutines(); // หยุด Coroutine รอการปิดด้วย
            }
        }
    }
    
    // ฟังก์ชันนี้ถูกเรียกโดย Gun.cs เมื่อผู้เล่นกด E (ใช้สำหรับ Skip/Close Dialog)
    public bool TryInteract()
    {
        // ถ้า Dialog กำลังทำงานอยู่ (ผู้เล่นกด E)
        if (manager.IsDialogueActive)
        {
            // สั่ง Skip/Close
            manager.StartDialogue(externalTextSource.text); 
            return true;
        }
        
        return false;
    }
    
    // Coroutine รอให้ DialogueManager ปิด Dialog
    IEnumerator WaitForDialogueEndAndFinalize()
    {
        // รอจนกว่า IsDialogueActive จะกลายเป็น false (หลัง EndDialogue ถูกเรียกจากการกด E)
        yield return new WaitUntil(() => manager.IsDialogueActive == false);

        // 🚨 Logic ที่เกิดขึ้นเมื่อ Dialog ปิดลง
        if (oneTimeUse)
        {
            GetComponent<Collider>().enabled = false;
            used = true;
            Debug.Log("Dialogue Trigger Finalized: Cannot be reused.");
        }
    }
}