using System.IO;
using UnityEditor;
using UnityEngine;

// Builds a static land mesh from an equirectangular world map (Blue Marble).
// Land vertices rise above the water; sea vertices dip below. The map texture
// is UV-mapped on so continents show real coastlines. A few narrow straits are
// forced to sea so historic routes (Gibraltar -> Mediterranean) stay passable.
public static class LandBuilder
{
    const string ImagePath = "Assets/Textures/worldmap.jpg";
    const string MeshPath = "Assets/Models/LandMesh.asset";
    const string MatPath = "Assets/Settings/LandMaterial.mat";

    // Covered region (degrees) and grid resolution.
    const float LonMin = -110f, LonMax = 140f, LatMin = -40f, LatMax = 65f;
    const float Step = 0.5f;
    const float LandHeight = 3f, SeaDepth = 2f; // land pokes ~half out of the water

    // Straits forced to sea: {lonMin, lonMax, latMin, latMax}
    static readonly float[][] Straits =
    {
        new[] { -6.3f, -5.0f, 35.5f, 36.3f }, // Gibraltar
        new[] { 25.8f, 29.8f, 40.0f, 41.4f }, // Dardanelles + Bosphorus
        new[] { 42.4f, 44.3f, 11.6f, 13.3f }, // Bab-el-Mandeb
        new[] { 55.6f, 57.3f, 25.6f, 27.1f }, // Hormuz
        new[] { 99.3f, 103.6f, 0.4f, 4.6f },  // Malacca
    };

    public static GameObject Build()
    {
        // Full-res readable copy for accurate land/sea classification.
        var tex = new Texture2D(2, 2, TextureFormat.RGB24, false);
        tex.LoadImage(File.ReadAllBytes(ImagePath));

        int cols = Mathf.RoundToInt((LonMax - LonMin) / Step) + 1;
        int rows = Mathf.RoundToInt((LatMax - LatMin) / Step) + 1;

        var verts = new Vector3[cols * rows];
        var uvs = new Vector2[cols * rows];
        var tris = new int[(cols - 1) * (rows - 1) * 6];

        for (int r = 0; r < rows; r++)
        {
            for (int c = 0; c < cols; c++)
            {
                float lon = LonMin + c * Step;
                float lat = LatMin + r * Step;
                int i = r * cols + c;
                var p = GeoProjection.LatLonToWorld(lat, lon);
                p.y = IsSea(tex, lat, lon) ? -SeaDepth : LandHeight;
                verts[i] = p;
                uvs[i] = GeoProjection.UV(lat, lon);
            }
        }

        int t = 0;
        for (int r = 0; r < rows - 1; r++)
        {
            for (int c = 0; c < cols - 1; c++)
            {
                int i = r * cols + c;
                tris[t++] = i; tris[t++] = i + cols; tris[t++] = i + cols + 1;
                tris[t++] = i; tris[t++] = i + cols + 1; tris[t++] = i + 1;
            }
        }

        var mesh = new Mesh { name = "LandMesh" };
        mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
        mesh.vertices = verts;
        mesh.uv = uvs;
        mesh.triangles = tris;
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        Object.DestroyImmediate(tex);

        Directory.CreateDirectory("Assets/Models");
        var existing = AssetDatabase.LoadAssetAtPath<Mesh>(MeshPath);
        if (existing != null) { EditorUtility.CopySerialized(mesh, existing); mesh = existing; }
        else AssetDatabase.CreateAsset(mesh, MeshPath);

        var go = new GameObject("Land", typeof(MeshFilter), typeof(MeshRenderer));
        go.GetComponent<MeshFilter>().sharedMesh = mesh;
        go.GetComponent<MeshRenderer>().sharedMaterial = MakeLandMaterial();

        // Static collision so the boat can't sail across land.
        var mc = go.AddComponent<MeshCollider>();
        mc.sharedMesh = mesh;
        return go;
    }

    static Material MakeLandMaterial()
    {
        var shader = Shader.Find("Universal Render Pipeline/Lit");
        var mat = AssetDatabase.LoadAssetAtPath<Material>(MatPath);
        if (mat == null) { mat = new Material(shader); AssetDatabase.CreateAsset(mat, MatPath); }
        mat.shader = shader;
        var mapTex = AssetDatabase.LoadAssetAtPath<Texture2D>(ImagePath);
        if (mat.HasProperty("_BaseMap")) mat.SetTexture("_BaseMap", mapTex);
        mat.mainTexture = mapTex;
        if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", 0.1f);
        return mat;
    }

    static bool IsSea(Texture2D tex, float lat, float lon)
    {
        foreach (var s in Straits)
            if (lon >= s[0] && lon <= s[1] && lat >= s[2] && lat <= s[3]) return true;

        var uv = GeoProjection.UV(lat, lon);
        int x = Mathf.Clamp(Mathf.RoundToInt(uv.x * (tex.width - 1)), 0, tex.width - 1);
        int y = Mathf.Clamp(Mathf.RoundToInt(uv.y * (tex.height - 1)), 0, tex.height - 1);
        var c = tex.GetPixel(x, y);
        // Blue Marble ocean is dark navy: blue clearly dominant + low brightness.
        return (c.b - c.r) > 0.05f && (c.r + c.g + c.b) / 3f < 0.33f;
    }
}
