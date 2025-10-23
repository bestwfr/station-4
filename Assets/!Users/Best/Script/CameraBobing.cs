using UnityEngine;

public class CameraBobbing : MonoBehaviour
{
    public bool enableBobbing = true;
    public float frequency = 1.8f;
    public float amplitude = 0.05f;
    public float smooth = 8f;
    public Vector3 externalVelocity;

    private Vector3 startPos;
    private float timer;

    void Start()
    {
        startPos = transform.localPosition;
    }

    void LateUpdate()
    {
        if (!enableBobbing)
        {
            transform.localPosition = Vector3.Lerp(transform.localPosition, startPos, Time.deltaTime * smooth);
            return;
        }

        float speed = new Vector3(externalVelocity.x, 0, externalVelocity.z).magnitude;

        if (speed < 0.1f)
        {
            transform.localPosition = Vector3.Lerp(transform.localPosition, startPos, Time.deltaTime * smooth);
            return;
        }

        timer += Time.deltaTime * frequency * Mathf.Clamp01(speed / 10f);
        float offsetY = Mathf.Sin(timer * Mathf.PI * 2f) * amplitude;

        Vector3 targetPos = startPos + new Vector3(0, offsetY, 0);
        transform.localPosition = Vector3.Lerp(transform.localPosition, targetPos, Time.deltaTime * smooth);
    }
}