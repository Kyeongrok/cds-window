using System.Collections.Generic;
using UnityEngine;

// Loads the 226 cities, drops a marker for each at its mapped world position,
// and reports which city the boat is currently near ("입항").
public class WorldMap : MonoBehaviour
{
    [Header("Refs")]
    public Transform boat;
    public Material markerMaterial;

    [Header("Markers")]
    public float markerHeight = 7f;
    public float markerRadius = 2.5f;

    [Header("Docking")]
    public float arrivalRadius = 18f;
    [Tooltip("Only draw name labels for cities within this range of the boat.")]
    public float labelRange = 450f;

    struct Entry { public City city; public Vector3 pos; }
    readonly List<Entry> entries = new List<Entry>();

    Entry? nearest;
    float nearestDist;
    int dockedId = -1;

    GUIStyle labelStyle, hudStyle;
    Font krFont;

    void Start()
    {
        var cities = CityLoader.LoadFromResources();
        if (cities.Length == 0) return;

        var root = new GameObject("Cities").transform;
        root.SetParent(transform, false);

        foreach (var c in cities)
        {
            Vector3 pos = MapToWorld(c);
            entries.Add(new Entry { city = c, pos = pos });

            var m = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            m.name = c.name;
            var col = m.GetComponent<Collider>();
            if (col != null) Destroy(col); // waypoint marker, not an obstacle
            m.transform.SetParent(root, false);
            m.transform.position = pos + Vector3.up * (markerHeight * 0.5f);
            m.transform.localScale = new Vector3(markerRadius, markerHeight * 0.5f, markerRadius);
            if (markerMaterial != null) m.GetComponent<MeshRenderer>().sharedMaterial = markerMaterial;
        }

        Debug.Log("WorldMap: placed " + entries.Count + " cities (lat/lon projection).");
    }

    // City position from real latitude/longitude (shared with the land mesh).
    Vector3 MapToWorld(City c) => GeoProjection.LatLonToWorld(c.latitude, c.longitude);

    void Update()
    {
        if (boat == null || entries.Count == 0) return;

        nearest = null;
        nearestDist = float.MaxValue;
        Vector3 b = boat.position;
        foreach (var e in entries)
        {
            float d = Vector2.Distance(new Vector2(b.x, b.z), new Vector2(e.pos.x, e.pos.z));
            if (d < nearestDist) { nearestDist = d; nearest = e; }
        }

        if (nearest.HasValue && nearestDist <= arrivalRadius)
        {
            if (dockedId != nearest.Value.city.id)
            {
                dockedId = nearest.Value.city.id;
                Debug.Log("입항: " + nearest.Value.city.name + " (" + nearest.Value.city.culturalSphere + ")");
            }
        }
        else dockedId = -1;
    }

    void EnsureStyles()
    {
        if (krFont == null)
            krFont = Font.CreateDynamicFontFromOSFont(new[] { "Malgun Gothic", "맑은 고딕", "Gulim", "Arial" }, 16);
        if (labelStyle == null)
        {
            labelStyle = new GUIStyle { font = krFont, fontSize = 14, alignment = TextAnchor.MiddleCenter };
            labelStyle.normal.textColor = Color.white;
        }
        if (hudStyle == null)
        {
            hudStyle = new GUIStyle { font = krFont, fontSize = 20 };
            hudStyle.normal.textColor = Color.white;
        }
    }

    void OnGUI()
    {
        if (entries.Count == 0) return;
        EnsureStyles();

        var cam = Camera.main;
        if (cam != null)
        {
            Vector3 b = boat != null ? boat.position : Vector3.zero;
            foreach (var e in entries)
            {
                if (Vector2.Distance(new Vector2(b.x, b.z), new Vector2(e.pos.x, e.pos.z)) > labelRange) continue;
                Vector3 sp = cam.WorldToScreenPoint(e.pos + Vector3.up * (markerHeight + 3f));
                if (sp.z <= 0f) continue;
                GUI.Label(new Rect(sp.x - 60f, Screen.height - sp.y - 10f, 120f, 20f), e.city.name, labelStyle);
            }
        }

        // HUD
        GUI.Label(new Rect(16f, 12f, 600f, 30f),
            nearest.HasValue ? string.Format("가까운 도시: {0}   거리 {1:0} m", nearest.Value.city.name, nearestDist)
                             : "도시 없음", hudStyle);

        if (nearest.HasValue && nearestDist <= arrivalRadius)
        {
            var box = new GUIStyle(hudStyle); box.normal.textColor = new Color(1f, 0.9f, 0.3f);
            GUI.Label(new Rect(16f, 44f, 600f, 30f), "⚓ 입항: " + nearest.Value.city.name, box);
        }
    }
}
