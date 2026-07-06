using UnityEngine;

// Sail-based sailing (sc1 spec):
//   Space  = raise / furl the sail
//   sail up   -> a tailwind pushes the boat forward automatically
//   sail down -> no push; the boat coasts to a stop (water drag)
//   A / D  = steer (turn) at any time
[RequireComponent(typeof(Rigidbody))]
public class BoatController : MonoBehaviour
{
    [Tooltip("Forward push from the tailwind while the sail is up.")]
    public float windForce = 700f;
    [Tooltip("Steering torque around the vertical axis (A/D).")]
    public float steerTorque = 220f;
    [Tooltip("Only sail while the hull centre is within this height of the water.")]
    public float driveDepth = 2f;
    public KeyCode sailKey = KeyCode.Space;

    public bool SailUp { get; private set; }

    Rigidbody rb;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    void Update()
    {
        if (Input.GetKeyDown(sailKey)) SailUp = !SailUp;
    }

    void FixedUpdate()
    {
        var field = WaveField.Instance;
        bool inWater = field == null || transform.position.y <= field.GetHeight(transform.position) + driveDepth;
        if (!inWater) return;

        float steer = Input.GetAxis("Horizontal"); // A / D
        rb.AddTorque(Vector3.up * steer * steerTorque, ForceMode.Force);

        if (SailUp)
            rb.AddForce(transform.forward * windForce, ForceMode.Force);
    }
}
