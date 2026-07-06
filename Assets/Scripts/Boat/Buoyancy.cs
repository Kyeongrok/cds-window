using UnityEngine;

// Probe-based buoyancy. Put a few empty child transforms near the hull bottom
// (corners work well); each pushes up when it dips below the wave surface.
// Gravity is applied here per-probe (so turn OFF Rigidbody.useGravity), which
// also produces natural pitch/roll as different corners submerge.
[RequireComponent(typeof(Rigidbody))]
public class Buoyancy : MonoBehaviour
{
    public Transform[] floatProbes;

    [Tooltip("Depth (m) over which submersion ramps from 0 to full push.")]
    public float depthBeforeSubmerged = 1f;
    [Tooltip("Upward push strength multiplier when fully submerged.")]
    public float displacementAmount = 3f;
    public float waterDrag = 1f;
    public float waterAngularDrag = 0.5f;

    Rigidbody rb;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.useGravity = false; // we apply gravity per-probe below
    }

    void FixedUpdate()
    {
        if (floatProbes == null || floatProbes.Length == 0) return;
        var field = WaveField.Instance;

        foreach (var probe in floatProbes)
        {
            if (probe == null) continue;

            // Gravity, split across probes.
            rb.AddForceAtPosition(Physics.gravity / floatProbes.Length, probe.position, ForceMode.Acceleration);

            float waterHeight = field != null ? field.GetHeight(probe.position) : 0f;
            if (probe.position.y >= waterHeight) continue;

            float submersion = Mathf.Clamp01((waterHeight - probe.position.y) / depthBeforeSubmerged);
            float displacement = submersion * displacementAmount;

            // Buoyant lift.
            rb.AddForceAtPosition(new Vector3(0f, Mathf.Abs(Physics.gravity.y) * displacement, 0f),
                                  probe.position, ForceMode.Acceleration);

            // Water resistance while submerged.
            rb.AddForce(displacement * -rb.linearVelocity * waterDrag * Time.fixedDeltaTime, ForceMode.VelocityChange);
            rb.AddTorque(displacement * -rb.angularVelocity * waterAngularDrag * Time.fixedDeltaTime, ForceMode.VelocityChange);
        }
    }
}
