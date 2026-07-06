using UnityEngine;

// Bottom-right compass dial (sc1). The dial rotates with the boat's heading so
// N/E/S/W always point to true world directions; a fixed marker at the top is
// the bow. Also shows the sail state (돛 펼침 / 접힘).
public class Compass : MonoBehaviour
{
    public Transform boat;
    public float size = 130f;
    public float margin = 20f;

    BoatController controller;
    Texture2D dialTex;
    Font krFont;
    GUIStyle card, cardN, marker, headingStyle, sailUpStyle, sailDownStyle;

    void Start()
    {
        if (boat != null) controller = boat.GetComponent<BoatController>();
    }

    void Ensure()
    {
        if (dialTex == null) dialTex = MakeDisc(96, new Color(0.05f, 0.08f, 0.12f, 0.65f));
        if (krFont == null) krFont = Font.CreateDynamicFontFromOSFont(new[] { "Malgun Gothic", "맑은 고딕", "Gulim", "Arial" }, 16);
        if (card == null)
        {
            card = new GUIStyle { fontSize = 15, alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold };
            card.normal.textColor = Color.white;
            cardN = new GUIStyle(card); cardN.normal.textColor = new Color(1f, 0.4f, 0.4f);
            marker = new GUIStyle { fontSize = 18, alignment = TextAnchor.MiddleCenter };
            marker.normal.textColor = new Color(1f, 0.9f, 0.3f);
            headingStyle = new GUIStyle { font = krFont, fontSize = 16, alignment = TextAnchor.MiddleCenter };
            headingStyle.normal.textColor = Color.white;
            sailUpStyle = new GUIStyle { font = krFont, fontSize = 16, alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold };
            sailUpStyle.normal.textColor = new Color(0.5f, 1f, 0.6f);
            sailDownStyle = new GUIStyle(sailUpStyle); sailDownStyle.normal.textColor = new Color(0.8f, 0.8f, 0.8f);
        }
    }

    void OnGUI()
    {
        Ensure();

        Rect r = new Rect(Screen.width - size - margin, Screen.height - size - margin, size, size);
        Vector2 c = r.center;
        float heading = boat != null ? boat.eulerAngles.y : 0f;

        GUI.DrawTexture(r, dialTex);

        // Rotating dial: cardinal letters at the ring.
        Matrix4x4 old = GUI.matrix;
        GUIUtility.RotateAroundPivot(-heading, c);
        float ring = size * 0.5f - 16f;
        Label("N", c + new Vector2(0f, -ring), cardN);
        Label("E", c + new Vector2(ring, 0f), card);
        Label("S", c + new Vector2(0f, ring), card);
        Label("W", c + new Vector2(-ring, 0f), card);
        GUI.matrix = old;

        // Fixed bow marker at the top.
        Label("▲", new Vector2(c.x, r.y + 10f), marker);

        // Heading readout in the centre.
        Label(string.Format("{0:000}° {1}", heading, Cardinal(heading)), c, headingStyle);

        // Sail state above the dial.
        bool up = controller != null && controller.SailUp;
        Label(up ? "돛 ▲ 펼침" : "돛 ▽ 접힘  (Space)", new Vector2(c.x, r.y - 12f), up ? sailUpStyle : sailDownStyle);
    }

    void Label(string text, Vector2 center, GUIStyle style)
    {
        GUI.Label(new Rect(center.x - 60f, center.y - 12f, 120f, 24f), text, style);
    }

    static string Cardinal(float deg)
    {
        string[] names = { "N", "NE", "E", "SE", "S", "SW", "W", "NW" };
        int i = Mathf.RoundToInt(deg / 45f) & 7;
        return names[i];
    }

    static Texture2D MakeDisc(int px, Color fill)
    {
        var t = new Texture2D(px, px, TextureFormat.RGBA32, false);
        float rad = px * 0.5f;
        Vector2 ctr = new Vector2(rad, rad);
        var edge = new Color(fill.r + 0.2f, fill.g + 0.25f, fill.b + 0.3f, 0.9f);
        for (int y = 0; y < px; y++)
        {
            for (int x = 0; x < px; x++)
            {
                float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), ctr);
                if (d > rad) t.SetPixel(x, y, Color.clear);
                else if (d > rad - 3f) t.SetPixel(x, y, edge);
                else t.SetPixel(x, y, fill);
            }
        }
        t.Apply();
        t.wrapMode = TextureWrapMode.Clamp;
        return t;
    }
}
