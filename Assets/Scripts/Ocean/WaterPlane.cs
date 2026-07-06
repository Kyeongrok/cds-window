using UnityEngine;

// Procedurally builds a grid mesh and displaces its vertices with WaveField
// every frame so you can see the sea the boat is floating on.
[ExecuteAlways]
[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class WaterPlane : MonoBehaviour
{
    [Tooltip("Total width/length of the water patch, in metres.")]
    public float size = 320f;
    [Tooltip("Vertices per side. Higher = smoother waves but more CPU.")]
    public int resolution = 80;
    [Tooltip("If set, the water patch recenters on this transform each frame (infinite-ocean illusion).")]
    public Transform follow;

    Mesh mesh;
    Vector3[] baseVerts;
    Vector3[] verts;

    void OnEnable()
    {
        BuildMesh();
    }

    // Public so the editor scene-builder can generate the mesh at author time.
    public void BuildMesh()
    {
        resolution = Mathf.Max(2, resolution);
        mesh = new Mesh { name = "WaterMesh" };
        mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;

        int n = resolution;
        baseVerts = new Vector3[n * n];
        var uvs = new Vector2[n * n];
        var tris = new int[(n - 1) * (n - 1) * 6];

        float step = size / (n - 1);
        float half = size * 0.5f;
        for (int z = 0; z < n; z++)
        {
            for (int x = 0; x < n; x++)
            {
                int i = z * n + x;
                baseVerts[i] = new Vector3(x * step - half, 0f, z * step - half);
                uvs[i] = new Vector2((float)x / (n - 1), (float)z / (n - 1));
            }
        }

        int t = 0;
        for (int z = 0; z < n - 1; z++)
        {
            for (int x = 0; x < n - 1; x++)
            {
                int i = z * n + x;
                tris[t++] = i;
                tris[t++] = i + n;
                tris[t++] = i + n + 1;
                tris[t++] = i;
                tris[t++] = i + n + 1;
                tris[t++] = i + 1;
            }
        }

        verts = (Vector3[])baseVerts.Clone();
        mesh.vertices = verts;
        mesh.uv = uvs;
        mesh.triangles = tris;
        mesh.RecalculateNormals();
        GetComponent<MeshFilter>().sharedMesh = mesh;
    }

    void Update()
    {
        if (mesh == null || baseVerts == null) BuildMesh();

        // Recenter under the followed target, snapped to the grid so waves don't crawl.
        if (follow != null)
        {
            float step = size / (Mathf.Max(2, resolution) - 1);
            Vector3 p = follow.position;
            transform.position = new Vector3(Mathf.Round(p.x / step) * step, 0f, Mathf.Round(p.z / step) * step);
        }

        var field = WaveField.Instance;
        if (field == null) return;

        Vector3 origin = transform.position;
        for (int i = 0; i < baseVerts.Length; i++)
        {
            Vector3 world = origin + baseVerts[i];
            float h = field.GetHeight(world);
            verts[i] = new Vector3(baseVerts[i].x, h - origin.y, baseVerts[i].z);
        }
        mesh.vertices = verts;
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
    }
}
