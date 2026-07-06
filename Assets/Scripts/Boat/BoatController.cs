using UnityEngine;

// WASD / arrow-key sailing. W-S drive thrust along the hull's forward axis,
// A-D steer (yaw torque). Force is only applied while the hull sits near the
// water so you can't fly the boat through the air.
[RequireComponent(typeof(Rigidbody))]
public class BoatController : MonoBehaviour
{
    [Tooltip("Forward/back thrust force.")]
    public float thrust = 250f;
    [Tooltip("Steering torque around the vertical axis.")]
    public float steerTorque = 120f;
    [Tooltip("Only drive when the hull centre is within this height of the water.")]
    public float driveDepth = 2f;

    Rigidbody rb;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    void FixedUpdate()
    {
        float forward = Input.GetAxis("Vertical");    // W / S
        float steer = Input.GetAxis("Horizontal");    // A / D

        var field = WaveField.Instance;
        bool inWater = true;
        if (field != null)
            inWater = transform.position.y <= field.GetHeight(transform.position) + driveDepth;
        if (!inWater) return;

        rb.AddForce(transform.forward * forward * thrust, ForceMode.Force);
        rb.AddTorque(Vector3.up * steer * steerTorque, ForceMode.Force);
    }
}
