using UnityEngine;
using UnityEngine.Playables; // สำคัญ: ต้องใช้ namespace นี้สำหรับ PlayableDirector

public class CutsceneTrigger : MonoBehaviour
{
    // [SerializeField] ทำให้เราสามารถลาก Playable Director มาใส่ใน Inspector ได้
    [SerializeField]
    private PlayableDirector cutsceneDirector;

    // ตัวแปรเสริม: เพื่อให้คัทซีนเล่นแค่ครั้งเดียว
    private bool hasPlayed = false;

    // ************************************************************
    // *** ฟังก์ชันนี้จะถูกเรียกเมื่อมีวัตถุ (ที่มี Collider และ Rigidbody) เข้าสู่พื้นที่ Trigger ***
    // ************************************************************
    private void OnTriggerEnter(Collider other)
    {
        // 1. ตรวจสอบว่าคัทซีนเคยเล่นไปแล้วหรือไม่
        if (hasPlayed)
        {
            return; // ออกจากฟังก์ชันทันที ถ้าเคยเล่นแล้ว
        }

        // 2. ตรวจสอบว่าวัตถุที่ชนคือ Player หรือไม่
        //    (สมมติว่า Player GameObject ของคุณมี Tag เป็น "Player")
        if (other.CompareTag("Player"))
        {
            // 3. ตรวจสอบว่ามีการอ้างอิง Playable Director หรือไม่
            if (cutsceneDirector != null)
            {
                // สั่งให้ Timeline (Cutscene) เริ่มเล่น
                cutsceneDirector.Play();

                // ตั้งค่าว่าคัทซีนนี้เล่นไปแล้ว เพื่อป้องกันการเล่นซ้ำ
                hasPlayed = true;

                // -------------------------------------------------------------------
                // **ส่วนเสริม (Optional)**:
                // เพื่อประสบการณ์ที่ดีขึ้น ควรปิดการควบคุมของ Player
                
                // ตัวอย่าง: ถ้า PlayerController ของคุณมีฟังก์ชัน DisableMovement()
                // PlayerController player = other.GetComponent<PlayerController>();
                // if (player != null)
                // {
                //     player.DisableMovement(); 
                // }
                // -------------------------------------------------------------------

                Debug.Log("Player เข้าสู่ Trigger และเริ่ม Cutscene!");
            }
            else
            {
                Debug.LogError("กรุณาลาก Playable Director มาใส่ในช่อง Cutscene Director ใน Inspector");
            }
        }
    }

    // -------------------------------------------------------------------
    // **ส่วนเสริม (Optional)**:
    // คุณอาจต้องการฟังก์ชันที่จะเปิดการควบคุม Player กลับมา เมื่อคัทซีนจบ
    // คุณสามารถใช้ PlayableDirector.stopped Event ในฟังก์ชัน Start() หรือ Awake()
    // -------------------------------------------------------------------

    /*
    private void Awake()
    {
        if (cutsceneDirector != null)
        {
            // ผูกฟังก์ชันที่จะทำงานเมื่อคัทซีนเล่นจบ
            cutsceneDirector.stopped += OnCutsceneStopped;
        }
    }

    private void OnCutsceneStopped(PlayableDirector director)
    {
        // โค้ดสำหรับเปิดการควบคุม Player กลับมา
        Debug.Log("Cutscene จบแล้ว");
        // GameObject.FindGameObjectWithTag("Player").GetComponent<PlayerController>().EnableMovement();
    }
    */
}