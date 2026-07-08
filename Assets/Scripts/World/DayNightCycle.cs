using UnityEngine;

// Day/night cycle for the sailing screen (sail4):
//   - the sun rises and sets (a directional light swinging from one horizon to
//     the other), warming near the horizon and going dark below it;
//   - a moon light takes over at night, opposite the sun;
//   - the sky/ambient shift day <-> dusk <-> night;
//   - visible sun & moon discs sit on the sky, and the moon disc shows the phase
//     for the current in-game date (new -> full -> new over a ~29.53-day month).
// Driven by GameClock (1 in-game day per real minute by default, so a full
// day/night takes ~1 minute; raise GameClock.secondsPerDay to slow it down).
public class DayNightCycle : MonoBehaviour
{
    public Light sun;
    public Light moon;
    public GameClock clock;
    public Camera cam;

    [Header("Sky discs")]
    public float discDistance = 600f;
    public float sunSize = 60f;
    public float moonSize = 46f;

    [Header("Look")]
    public float sunYaw = 150f;               // compass line the sun rises/sets along

    const float Synodic = 29.53f;             // days per lunar (synodic) month

    static readonly Color Night = new Color(0.02f, 0.03f, 0.09f);
    static readonly Color Noon  = new Color(0.45f, 0.62f, 0.92f);
    static readonly Color Dusk  = new Color(0.92f, 0.45f, 0.22f);

    Transform sunDisc, moonDisc, moonShadow;
    Material sunMat, moonMat, shadowMat;

    void Start()
    {
        if (cam == null) cam = Camera.main;
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;

        sunDisc  = MakeDisc("SunDisc",  out sunMat);
        moonDisc = MakeDisc("MoonDisc", out moonMat);
        moonMat.color = new Color(0.92f, 0.92f, 0.85f);
        moonShadow = MakeDisc("MoonShadow", out shadowMat);
        moonShadow.SetParent(moonDisc, false);
        moonShadow.localScale = Vector3.one * 1.06f;
    }

    Transform MakeDisc(string name, out Material mat)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        go.name = name;
        var col = go.GetComponent<Collider>(); if (col != null) Destroy(col);
        go.transform.SetParent(transform, false);
        var shader = Shader.Find("Universal Render Pipeline/Unlit");
        if (shader == null) shader = Shader.Find("Unlit/Color");
        mat = new Material(shader);
        var mr = go.GetComponent<MeshRenderer>();
        mr.sharedMaterial = mat;
        mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        mr.receiveShadows = false;
        return go.transform;
    }

    void LateUpdate()
    {
        if (cam == null) { cam = Camera.main; if (cam == null) return; }

        float f = clock != null ? clock.DayFraction : 0.4f;

        // --- Sun: swing from horizon (sunrise) over the top (noon) to the other
        //     horizon (sunset) and below (night). ---
        float sunAngle = (f - 0.25f) * 360f;                 // .25=sunrise(0) .5=noon(90) .75=sunset(180)
        float sunAlt = Mathf.Sin(sunAngle * Mathf.Deg2Rad);  // -1..1, altitude above horizon
        float day = Mathf.Clamp01((sunAlt + 0.1f) / 0.3f);   // 0 night .. 1 day
        float horizon = Mathf.Clamp01(1f - Mathf.Abs(sunAlt) * 4f); // peaks at sunrise/sunset

        if (sun != null)
        {
            sun.transform.rotation = Quaternion.Euler(sunAngle, sunYaw, 0f);
            sun.intensity = Mathf.Clamp01(sunAlt) * 1.15f;
            sun.color = Color.Lerp(new Color(1f, 0.55f, 0.32f), new Color(1f, 0.96f, 0.86f), day);
            sun.enabled = sunAlt > -0.08f;
            sun.shadows = LightShadows.Soft;
        }

        // --- Moon: opposite the sun, lights the night faintly. ---
        float moonAngle = sunAngle + 180f;
        float moonAlt = Mathf.Sin(moonAngle * Mathf.Deg2Rad);
        if (moon != null)
        {
            moon.transform.rotation = Quaternion.Euler(moonAngle, sunYaw, 0f);
            moon.intensity = Mathf.Clamp01(moonAlt) * 0.22f;
            moon.color = new Color(0.6f, 0.7f, 1f);
            moon.enabled = moonAlt > 0.02f;
        }

        // --- Sky / ambient ---
        Color sky = Color.Lerp(Night, Noon, day);
        sky = Color.Lerp(sky, Dusk, horizon * (1f - day * 0.6f));
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = sky;
        RenderSettings.ambientLight = Color.Lerp(new Color(0.05f, 0.06f, 0.12f),
                                                 new Color(0.55f, 0.58f, 0.62f), day);

        // --- Sky discs (anchored around the camera so they read as far away) ---
        Vector3 camPos = cam.transform.position;

        if (sun != null && sunDisc != null)
        {
            sunDisc.gameObject.SetActive(sunAlt > -0.15f);
            sunDisc.position = camPos - sun.transform.forward * discDistance;
            sunDisc.localScale = Vector3.one * sunSize;
            sunMat.color = Color.Lerp(new Color(1f, 0.5f, 0.28f), new Color(1f, 0.97f, 0.8f), day);
        }

        if (moon != null && moonDisc != null)
        {
            moonDisc.gameObject.SetActive(moonAlt > -0.05f);
            moonDisc.position = camPos - moon.transform.forward * discDistance;
            moonDisc.localScale = Vector3.one * moonSize;
            // Face the camera so the shadow slides "sideways" on screen.
            moonDisc.rotation = Quaternion.LookRotation((moonDisc.position - camPos).normalized, Vector3.up);

            // Phase from the in-game date, offset so day 0 (1480-01-01) is a
            // full moon -> clearly visible from the first night, then waning to
            // new and back: 0 = new .. 0.5 = full .. 1 = new.
            double totalDays = clock != null ? clock.TotalDays : 0.0;
            float phase01 = Mathf.Repeat((float)(totalDays / Synodic) + 0.5f, 1f);
            float illum = (1f - Mathf.Cos(phase01 * 2f * Mathf.PI)) * 0.5f;   // 0 new .. 1 full
            shadowMat.color = sky;                                            // carve with the sky colour
            float side = phase01 < 0.5f ? 1f : -1f;                           // waxing vs waning
            // localPosition in parent radii: 0 = shadow covering (new),
            // ~1.15 = shadow fully clear of the disc (full).
            moonShadow.localPosition = new Vector3(side * illum * 1.15f, 0f, -0.18f);
        }
    }
}
