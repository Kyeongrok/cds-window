using UnityEngine;

// Simple chase camera. Follows the target using yaw only, so the view doesn't
// seasick-roll with the boat's pitch and roll. (Swap for Cinemachine later.)
public class CameraFollow : MonoBehaviour
{
    public Transform target;
    public Vector3 offset = new Vector3(0f, 7f, -14f);
    public float positionSmooth = 3f;
    public float lookHeight = 2f;

    void LateUpdate()
    {
        if (target == null) return;

        Quaternion yaw = Quaternion.Euler(0f, target.eulerAngles.y, 0f);
        Vector3 desired = target.position + yaw * offset;
        transform.position = Vector3.Lerp(transform.position, desired, positionSmooth * Time.deltaTime);
        transform.LookAt(target.position + Vector3.up * lookHeight);
    }
}
