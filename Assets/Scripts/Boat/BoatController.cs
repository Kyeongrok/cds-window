using UnityEngine;

// Sail-based sailing (sc1) + sea controls (조작-해상 ct1-1):
//   Space          = raise / furl the sail (tailwind auto-drives when up)
//   A / D          = steer manually
//   Double-click   = turn the bow toward the clicked point on the water
[RequireComponent(typeof(Rigidbody))]
public class BoatController : MonoBehaviour
{
    [Tooltip("Forward push from the tailwind while the sail is up.")]
    public float windForce = 700f;
    [Tooltip("Steering torque around the vertical axis.")]
    public float steerTorque = 220f;
    [Tooltip("Only sail while the hull centre is within this height of the water.")]
    public float driveDepth = 2f;
    public KeyCode sailKey = KeyCode.Space;
    public float doubleClickTime = 0.3f;

    public bool SailUp { get; private set; }

    Rigidbody rb;
    float lastClickTime = -1f;
    bool hasDesiredHeading;
    float desiredHeading;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    void Update()
    {
        if (Input.GetKeyDown(sailKey)) SailUp = !SailUp;

        if (Input.GetMouseButtonDown(0))
        {
            if (Time.time - lastClickTime <= doubleClickTime) SteerToMouse();
            lastClickTime = Time.time;
        }

        // Manual steering cancels the auto-heading.
        if (Mathf.Abs(Input.GetAxis("Horizontal")) > 0.01f) hasDesiredHeading = false;
    }

    void SteerToMouse()
    {
        var cam = Camera.main;
        if (cam == null) return;
        Ray ray = cam.ScreenPointToRay(Input.mousePosition);
        if (Mathf.Abs(ray.direction.y) < 1e-4f) return;

        float t = -ray.origin.y / ray.direction.y; // intersect the water plane (y = 0)
        if (t <= 0f) return;

        Vector3 hit = ray.origin + ray.direction * t;
        Vector3 dir = hit - transform.position;
        dir.y = 0f;
        if (dir.sqrMagnitude < 0.01f) return;

        desiredHeading = Mathf.Atan2(dir.x, dir.z) * Mathf.Rad2Deg;
        hasDesiredHeading = true;
    }

    void FixedUpdate()
    {
        var field = WaveField.Instance;
        bool inWater = field == null || transform.position.y <= field.GetHeight(transform.position) + driveDepth;
        if (!inWater) return;

        float steer = Input.GetAxis("Horizontal"); // A / D

        // Auto-turn toward the double-clicked heading when not steering manually.
        if (hasDesiredHeading && Mathf.Abs(steer) < 0.01f)
        {
            float diff = Mathf.DeltaAngle(transform.eulerAngles.y, desiredHeading);
            steer = Mathf.Clamp(diff / 30f, -1f, 1f);
            if (Mathf.Abs(diff) < 1.5f) hasDesiredHeading = false;
        }

        rb.AddTorque(Vector3.up * steer * steerTorque, ForceMode.Force);

        if (SailUp)
            rb.AddForce(transform.forward * windForce, ForceMode.Force);
    }
}
