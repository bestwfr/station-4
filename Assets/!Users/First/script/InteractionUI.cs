using UnityEngine;
using TMPro; // อย่าลืม using นี้!
using System.Collections; // สำหรับ Coroutine

public class InteractionUI : MonoBehaviour
{
    // ลาก TextMeshProUGUI Component มาใส่ใน Inspector
    public TextMeshProUGUI promptText; 
    
    [Header("Typing Effect Settings")]
    [Tooltip("ความเร็วในการพิมพ์ (วินาทีต่อตัวอักษร)")]
    public float typingSpeed = 0.05f; 

    private Coroutine typingCoroutine; // สำหรับเก็บ Coroutine เพื่อหยุดการทำงานเก่า

    void Start()
    {
        if (promptText == null)
        {
            Debug.LogError("InteractionUI: Prompt TextMeshProUGUI is not assigned!");
        }
        else
        {
            // ซ่อนตั้งแต่เริ่มต้น
            promptText.gameObject.SetActive(false);
        }
    }

    // 💡 แก้ไข: เพิ่ม bool includePrefix = true เพื่อรองรับการแสดงข้อความแบบไม่มี "Press E to"
    // เมธอดที่ GunInteractionHandler และ HintTrigger เรียกเพื่อแสดงข้อความ
    public void ShowPrompt(string actionText, bool includePrefix = true)
    {
        if (promptText == null) return;
        
        // 🚨 NEW: ถ้า includePrefix เป็น false จะใช้ actionText ตรงๆ
        string fullText = includePrefix ? ("Press E to " + actionText) : actionText;
        
        // หยุด Coroutine เก่าก่อนเริ่มใหม่ (ถ้ามี)
        if (typingCoroutine != null)
        {
            StopCoroutine(typingCoroutine);
        }

        promptText.gameObject.SetActive(true);
        // เริ่ม Coroutine สำหรับพิมพ์ทีละตัว
        typingCoroutine = StartCoroutine(TypeSentence(fullText));
    }

    public void HidePrompt()
    {
        if (promptText == null) return;
        
        // ถ้ากำลังพิมพ์อยู่ ให้หยุด
        if (typingCoroutine != null)
        {
            StopCoroutine(typingCoroutine);
        }

        promptText.gameObject.SetActive(false);
    }
    
    // ----------------------------------------------------
    // Coroutine สำหรับ Typing Effect
    // ----------------------------------------------------
    IEnumerator TypeSentence(string sentence)
    {
        // เริ่มต้นด้วยการเคลียร์ข้อความทั้งหมด
        promptText.text = "";
        
        // วนลูปผ่านทุกตัวอักษรในประโยค
        foreach (char letter in sentence.ToCharArray())
        {
            promptText.text += letter; // เพิ่มตัวอักษรทีละตัว
            
            // รอตามเวลาที่กำหนด (Typing Speed)
            yield return new WaitForSeconds(typingSpeed);
        }
        
        // เมื่อพิมพ์เสร็จแล้ว ล้าง Reference ของ Coroutine
        typingCoroutine = null;
    }
}