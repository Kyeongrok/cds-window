using System;
using UnityEngine;

// In-game calendar shown at the top of the screen.
// Starts 1480-01-01 and advances one day per real-time minute (configurable).
public class GameClock : MonoBehaviour
{
    public int startYear = 1480;
    public int startMonth = 1;
    public int startDay = 1;
    [Tooltip("Real seconds per in-game day.")]
    public float secondsPerDay = 60f;

    public DateTime Now => start.AddDays(Mathf.FloorToInt(elapsed / Mathf.Max(0.01f, secondsPerDay)));

    // Continuous days since the start date (fractional) and the fraction through
    // the current day (0 = midnight, 0.5 = noon). Used by the day/night cycle.
    public double TotalDays => elapsed / Mathf.Max(0.01f, secondsPerDay);
    public float DayFraction => Mathf.Repeat((float)TotalDays, 1f);

    DateTime start;
    float elapsed;

    Font krFont;
    GUIStyle style;
    Texture2D bg;

    void Awake()
    {
        start = new DateTime(startYear, startMonth, startDay);
    }

    void Update()
    {
        elapsed += Time.deltaTime;
    }

    void Ensure()
    {
        if (bg == null)
        {
            bg = new Texture2D(1, 1);
            bg.SetPixel(0, 0, new Color(0f, 0f, 0.05f, 0.55f));
            bg.Apply();
        }
        if (krFont == null) krFont = Font.CreateDynamicFontFromOSFont(new[] { "Malgun Gothic", "맑은 고딕", "Gulim", "Arial" }, 22);
        if (style == null)
        {
            style = new GUIStyle { font = krFont, fontSize = 22, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
            style.normal.textColor = Color.white;
        }
    }

    void OnGUI()
    {
        Ensure();
        var d = Now;
        string text = string.Format("{0}년 {1}월 {2}일", d.Year, d.Month, d.Day);
        var r = new Rect(Screen.width / 2f - 110f, 8f, 220f, 34f);
        GUI.DrawTexture(r, bg);
        GUI.Label(r, text, style);
    }
}
