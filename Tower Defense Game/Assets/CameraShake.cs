using UnityEngine;

public class CameraShake : MonoBehaviour
{
    private Vector3 originalPos;
    private float duration;
    private float magnitude;
    private bool active;

    public static void Shake(float duration, float magnitude)
    {
        Camera cam = Camera.main;
        if (cam == null) return;
        CameraShake cs = cam.GetComponent<CameraShake>() ?? cam.gameObject.AddComponent<CameraShake>();
        cs.Trigger(duration, magnitude);
    }

    void Trigger(float d, float m)
    {
        if (!active) originalPos = transform.localPosition;
        // Take the stronger / longer of overlapping shakes
        duration  = Mathf.Max(duration,  d);
        magnitude = Mathf.Max(magnitude, m);
        active = true;
    }

    void LateUpdate()
    {
        if (!active) return;

        if (duration > 0f)
        {
            // Use unscaled time so shake is unaffected by hit-stop
            transform.localPosition = originalPos + (Vector3)(Random.insideUnitCircle * magnitude);
            duration -= Time.unscaledDeltaTime;
        }
        else
        {
            transform.localPosition = originalPos;
            duration = 0f;
            magnitude = 0f;
            active = false;
        }
    }
}
