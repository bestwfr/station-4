using UnityEngine;
// ไม่จำเป็นต้องใช้ using UnityEngine.UI; แล้ว ถ้าเราควบคุมแค่ GameObject
// using UnityEngine.UI; 

public class CrosshairManager : MonoBehaviour
{
    [Header("Crosshair UI GameObjects")]
    [Tooltip("ลาก GameObject แม่ของ Crosshair ปกติมาใส่")]
    public GameObject defaultCrosshair; // 🚨 เปลี่ยนจาก Image เป็น GameObject
    [Tooltip("ลาก GameObject แม่ของ Crosshair สำหรับ Interactable มาใส่")]
    public GameObject interactCrosshair; // 🚨 เปลี่ยนจาก Image เป็น GameObject

    [Header("Raycast Settings")]
    [Tooltip("ลาก Main Camera ของผู้เล่นมาใส่")]
    public Camera playerCamera; 
    [Tooltip("ระยะการมองเห็นของ Raycast")]
    public float interactionDistance = 3f; 
    [Tooltip("LayerMask เพื่อกรองวัตถุที่ Raycast จะตรวจจับ")]
    public LayerMask interactableLayer; 

    // Cached references
    private string interactableTag = "Interactable"; 

    void Start()
    {
        // ... (โค้ดตรวจสอบความถูกต้องเดิม) ...
        if (playerCamera == null)
        {
            Debug.LogError("CrosshairManager: Player Camera not assigned! Please assign your main camera.");
            enabled = false; 
            return;
        }

        // ตรวจสอบ GameObject
        if (defaultCrosshair == null || interactCrosshair == null)
        {
            Debug.LogError("CrosshairManager: Crosshair UI GameObjects not assigned! Please assign both default and interact crosshairs.");
            enabled = false; 
            return;
        }
        
        ShowDefaultCrosshair();
    }

    void Update()
    {
        CheckForInteractable();
    }

    void CheckForInteractable()
    {
        Ray ray = playerCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0)); 
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, interactionDistance, interactableLayer))
        {
            if (hit.collider.CompareTag(interactableTag))
            {
                ShowInteractCrosshair(); 
            }
            else
            {
                // ตรวจสอบว่าชนวัตถุอื่น (ที่ไม่ใช่ Interactable)
                ShowDefaultCrosshair(); 
            }
        }
        else
        {
            // Raycast ไม่ชนอะไรเลย
            ShowDefaultCrosshair(); 
        }
    }

    void ShowDefaultCrosshair()
    {
        // 🚨 ใช้ .SetActive() กับ GameObject แม่
        defaultCrosshair.SetActive(true);
        interactCrosshair.SetActive(false);
    }

    void ShowInteractCrosshair()
    {
        // 🚨 ใช้ .SetActive() กับ GameObject แม่
        defaultCrosshair.SetActive(false);
        interactCrosshair.SetActive(true);
    }
}