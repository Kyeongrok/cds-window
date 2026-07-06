using UnityEngine;

// Analytic wave height field (sum of sine waves).
// Both the visible water mesh (WaterPlane) and the boat (Buoyancy) sample this
// so the boat rides exactly the waves you see. Swap this out for Crest later:
// just make Buoyancy read Crest's sampled height instead of WaveField.GetHeight.
[ExecuteAlways]
public class WaveField : MonoBehaviour
{
    public static WaveField Instance { get; private set; }

    [System.Serializable]
    public struct Wave
    {
        public Vector2 direction; // XZ direction (auto-normalized)
        public float amplitude;   // metres
        public float wavelength;  // metres between crests
        public float speed;       // crest travel speed (m/s)
    }

    [Tooltip("Superposed sine waves. A few with different directions/lengths reads as a natural sea.")]
    public Wave[] waves;

    void OnEnable()
    {
        Instance = this;
        if (waves == null || waves.Length == 0)
        {
            waves = new[]
            {
                new Wave { direction = new Vector2(1f, 0.2f),  amplitude = 0.5f, wavelength = 18f, speed = 4f },
                new Wave { direction = new Vector2(0.3f, 1f),  amplitude = 0.3f, wavelength = 11f, speed = 3f },
                new Wave { direction = new Vector2(-0.6f, 0.4f), amplitude = 0.15f, wavelength = 6f, speed = 2f },
            };
        }
    }

    // Water surface height (world Y) at a world position.
    public float GetHeight(Vector3 worldPos)
    {
        float t = Application.isPlaying ? Time.time : 0f;
        float h = 0f;
        if (waves != null)
        {
            foreach (var w in waves)
            {
                if (w.wavelength <= 0.001f) continue;
                Vector2 dir = w.direction.sqrMagnitude > 0.0001f ? w.direction.normalized : Vector2.right;
                float k = 2f * Mathf.PI / w.wavelength;
                float phase = (dir.x * worldPos.x + dir.y * worldPos.z) * k + t * w.speed * k;
                h += w.amplitude * Mathf.Sin(phase);
            }
        }
        return transform.position.y + h;
    }
}
