using UnityEngine;

// 해역 지도 (화면2-1): press M to toggle a top-down regional sea map centered
// on the boat, drawn straight from the world map texture, with cities and the
// boat's position + heading. Press M or Esc to close.
public class MapScreen : MonoBehaviour
{
    public Transform boat;
    public Texture2D mapTexture;
    public KeyCode toggleKey = KeyCode.M;

    [Tooltip("Half-width of the map window in degrees of longitude / latitude.")]
    public float halfLonSpan = 30f;
    public float halfLatSpan = 18f;

    bool open;
    City[] cities;

    Font krFont;
    GUIStyle titleStyle, cityStyle, boatStyle;
    Texture2D dot;

    void Start()
    {
        cities = CityLoader.LoadFromResources();
    }

    void Update()
    {
        if (Input.GetKeyDown(toggleKey)) open = !open;
        if (open && Input.GetKeyDown(KeyCode.Escape)) open = false;
    }

    void Ensure()
    {
        if (dot == null)
        {
            dot = new Texture2D(1, 1);
            dot.SetPixel(0, 0, Color.white);
            dot.Apply();
        }
        if (krFont == null) krFont = Font.CreateDynamicFontFromOSFont(new[] { "Malgun Gothic", "맑은 고딕", "Gulim", "Arial" }, 16);
        if (titleStyle == null)
        {
            titleStyle = new GUIStyle { font = krFont, fontSize = 22, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
            titleStyle.normal.textColor = Color.white;
            cityStyle = new GUIStyle { font = krFont, fontSize = 12, alignment = TextAnchor.MiddleLeft };
            cityStyle.normal.textColor = Color.white;
            boatStyle = new GUIStyle { font = krFont, fontSize = 13, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
            boatStyle.normal.textColor = new Color(0.4f, 1f, 1f);
        }
    }

    void Fill(Rect r, Color c)
    {
        var old = GUI.color; GUI.color = c;
        GUI.DrawTexture(r, dot);
        GUI.color = old;
    }

    void OnGUI()
    {
        if (!open || boat == null) return;
        Ensure();

        Fill(new Rect(0, 0, Screen.width, Screen.height), new Color(0f, 0f, 0.02f, 0.9f));

        var ll = GeoProjection.WorldToLatLon(boat.position);
        float latC = ll.x, lonC = ll.y;
        float lonMin = lonC - halfLonSpan, lonMax = lonC + halfLonSpan;
        float latMin = latC - halfLatSpan, latMax = latC + halfLatSpan;

        // Centered map rect, keeping the degree-window aspect.
        float margin = 60f;
        float availW = Screen.width - 2f * margin;
        float availH = Screen.height - 2f * margin - 50f;
        float aspect = (2f * halfLonSpan) / (2f * halfLatSpan);
        float rw = availW, rh = rw / aspect;
        if (rh > availH) { rh = availH; rw = rh * aspect; }
        Rect map = new Rect((Screen.width - rw) / 2f, (Screen.height - rh) / 2f + 20f, rw, rh);

        if (mapTexture != null)
        {
            float u0 = (lonMin + 180f) / 360f, u1 = (lonMax + 180f) / 360f;
            float v0 = (latMin + 90f) / 180f, v1 = (latMax + 90f) / 180f;
            GUI.DrawTextureWithTexCoords(map, mapTexture, new Rect(u0, v0, u1 - u0, v1 - v0));
        }
        // border
        Fill(new Rect(map.x, map.y, map.width, 2f), Color.white);
        Fill(new Rect(map.x, map.yMax - 2f, map.width, 2f), Color.white);
        Fill(new Rect(map.x, map.y, 2f, map.height), Color.white);
        Fill(new Rect(map.xMax - 2f, map.y, 2f, map.height), Color.white);

        // Cities inside the window.
        if (cities != null)
        {
            foreach (var c in cities)
            {
                if (c.longitude < lonMin || c.longitude > lonMax || c.latitude < latMin || c.latitude > latMax) continue;
                float sx = map.x + (c.longitude - lonMin) / (lonMax - lonMin) * map.width;
                float sy = map.y + (latMax - c.latitude) / (latMax - latMin) * map.height;
                Fill(new Rect(sx - 3f, sy - 3f, 6f, 6f), new Color(1f, 0.85f, 0.2f));
                GUI.Label(new Rect(sx + 5f, sy - 9f, 120f, 18f), c.name, cityStyle);
            }
        }

        // Boat marker + heading.
        float bx = map.x + (lonC - lonMin) / (lonMax - lonMin) * map.width;
        float by = map.y + (latMax - latC) / (latMax - latMin) * map.height;
        var oldM = GUI.matrix;
        GUIUtility.RotateAroundPivot(boat.eulerAngles.y, new Vector2(bx, by));
        GUI.Label(new Rect(bx - 12f, by - 14f, 24f, 24f), "▲", boatStyle);
        GUI.matrix = oldM;
        Fill(new Rect(bx - 2f, by - 2f, 4f, 4f), new Color(0.4f, 1f, 1f));

        GUI.Label(new Rect(0f, 18f, Screen.width, 30f), "해역 지도    (M 닫기)", titleStyle);
    }
}
