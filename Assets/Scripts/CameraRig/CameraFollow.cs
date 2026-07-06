using UnityEngine;

// Orbit chase camera (조작-해상 ct1-1): the camera follows the boat's position,
// and holding the RIGHT mouse button + dragging orbits the view (yaw/pitch).
// It does not auto-swing behind the boat, so the player controls the framing.
public class CameraFollow : MonoBehaviour
{
    public Transform target;
    public float distance = 22f;
    public float yaw = 0f;
    public float pitch = 22f;
    public float sensitivity = 4f;
    public float minPitch = 6f;
    public float maxPitch = 80f;
    public float lookHeight = 2f;

    void Start()
    {
        if (target != null) yaw = target.eulerAngles.y;
    }

    void LateUpdate()
    {
        if (target == null) return;

        if (Input.GetMouseButton(1)) // right button held -> orbit
        {
            yaw += Input.GetAxis("Mouse X") * sensitivity;
            pitch -= Input.GetAxis("Mouse Y") * sensitivity;
            pitch = Mathf.Clamp(pitch, minPitch, maxPitch);
        }

        Vector3 offset = Quaternion.Euler(pitch, yaw, 0f) * (Vector3.back * distance);
        transform.position = target.position + offset;
        transform.LookAt(target.position + Vector3.up * lookHeight);
    }
}
