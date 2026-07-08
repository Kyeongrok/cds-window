using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

// Builds a procedural age-of-sail caravel/carrack visual for the Boat root:
//   - a tapered wooden hull (generated mesh) with a low waist and raised
//     fore- and aft-castle sheer line, like a 15th-c. sailing ship,
//   - three masts (fore / main / mizzen) + a bowsprit,
//   - square courses & a topsail on curved "canvas" sails, a lateen mizzen,
//   - yards and small pennants.
// No external asset or glTF importer is needed: every generated mesh is saved
// under Assets/Models as a .asset (same pattern as LandBuilder) so it survives
// scene reload; masts/yards reuse Unity's built-in cylinder mesh.
//
// Call ShipBuilder.AddShipVisual(boatTransform, hullMat) from a scene builder.
public static class ShipBuilder
{
    const string HullMeshPath   = "Assets/Models/ShipHull.asset";
    const string SquareSailPath = "Assets/Models/ShipSailSquare.asset";
    const string LateenSailPath = "Assets/Models/ShipSailLateen.asset";

    // Menu entry so the meshes can be (re)generated on their own if wanted.
    [MenuItem("Tools/CDS/Rebuild Ship Meshes")]
    public static void RebuildMeshes()
    {
        BuildHullMesh();
        BuildSquareSailMesh();
        BuildLateenSailMesh();
        AssetDatabase.SaveAssets();
        Debug.Log("ShipBuilder: rebuilt ship meshes under Assets/Models.");
    }

    // Attaches the whole ship model under `boat` as a "ShipModel" child.
    // hullMat is the shared wooden hull material supplied by the scene builder.
    public static GameObject AddShipVisual(Transform boat, Material hullMat)
    {
        // Render the hull double-sided too: it is a closed shell, so back-faces
        // are hidden anyway, but this guarantees it stays visible even if a few
        // generated normals point the wrong way.
        if (hullMat.HasProperty("_Cull")) hullMat.SetFloat("_Cull", 0f);

        var mastMat = MakeMat("Assets/Settings/MastMaterial.mat", new Color(0.26f, 0.17f, 0.09f), 0.15f, false);
        var sailMat = MakeMat("Assets/Settings/SailMaterial.mat", new Color(0.90f, 0.86f, 0.74f), 0.05f, true);
        var flagMat = MakeMat("Assets/Settings/FlagMaterial.mat", new Color(0.72f, 0.13f, 0.13f), 0.1f, true);

        var hullMesh   = BuildHullMesh();
        var squareSail = BuildSquareSailMesh();
        var lateenSail = BuildLateenSailMesh();

        var root = new GameObject("ShipModel");
        root.transform.SetParent(boat, false);

        // --- Hull ---
        var hull = new GameObject("Hull", typeof(MeshFilter), typeof(MeshRenderer));
        hull.transform.SetParent(root.transform, false);
        hull.GetComponent<MeshFilter>().sharedMesh = hullMesh;
        hull.GetComponent<MeshRenderer>().sharedMaterial = hullMat;

        // --- Masts (base sits on deck; +z is forward) ---
        var mainBase   = new Vector3(0f, 0.40f, 0.10f);
        var foreBase   = new Vector3(0f, 0.62f, 1.80f);
        var mizzenBase = new Vector3(0f, 0.68f, -1.50f);

        Spar(root.transform, "MainMast",   mainBase,   Vector3.up,                       0.15f, 4.9f, mastMat);
        Spar(root.transform, "ForeMast",   foreBase,   Vector3.up,                       0.13f, 3.5f, mastMat);
        Spar(root.transform, "MizzenMast", mizzenBase, new Vector3(0f, 0.985f, -0.174f), 0.11f, 3.2f, mastMat); // raked aft

        // Bowsprit off the stem, angled up and forward
        Spar(root.transform, "Bowsprit", new Vector3(0f, 0.85f, 2.85f), new Vector3(0f, 0.34f, 0.94f), 0.10f, 2.2f, mastMat);

        // --- Yards (horizontal spars) ---
        Bar(root.transform, "MainYard",    new Vector3(0f, 2.50f, 0.10f), Vector3.right, 0.08f, 2.8f, mastMat);
        Bar(root.transform, "MainTopYard", new Vector3(0f, 4.10f, 0.10f), Vector3.right, 0.06f, 1.9f, mastMat);
        Bar(root.transform, "ForeYard",    new Vector3(0f, 2.00f, 1.80f), Vector3.right, 0.07f, 2.1f, mastMat);

        // --- Square sails (hang just below their yard, bellied toward the bow) ---
        AddSail(root.transform, "MainCourse",  squareSail, new Vector3(0f, 1.72f, 0.16f), new Vector3(2.55f, 1.55f, 0.34f), Quaternion.identity, sailMat);
        AddSail(root.transform, "MainTopsail", squareSail, new Vector3(0f, 3.42f, 0.15f), new Vector3(1.75f, 1.10f, 0.22f), Quaternion.identity, sailMat);
        AddSail(root.transform, "ForeCourse",  squareSail, new Vector3(0f, 1.30f, 1.85f), new Vector3(1.95f, 1.35f, 0.30f), Quaternion.identity, sailMat);

        // --- Lateen mizzen (triangular fore-and-aft sail, already in ship-local coords) ---
        AddSail(root.transform, "MizzenLateen", lateenSail, Vector3.zero, Vector3.one, Quaternion.identity, sailMat);
        // its yard, raked along the sail's leading edge
        Bar(root.transform, "MizzenYard", new Vector3(0f, 2.25f, -1.90f), new Vector3(0f, 0.80f, -0.60f), 0.06f, 3.4f, mastMat);

        // --- Pennants at the mastheads ---
        AddPennant(root.transform, "MainPennant", new Vector3(0f, 5.20f, 0.10f), flagMat);
        AddPennant(root.transform, "ForePennant", new Vector3(0f, 4.02f, 1.80f), flagMat);

        return root;
    }

    // -------------------------------------------------------------------------
    // Hull mesh: cross-sections lofted stern -> bow. Control stations define the
    // beam, sheer (deck-edge height) and keel depth; the sheer dips amidships and
    // rises at both ends to read as fore- and aft-castles.
    // -------------------------------------------------------------------------
    static Mesh BuildHullMesh()
    {
        // control stations: z, half-beam, sheer(top y), keel(bottom y)
        float[] cz    = { -3.0f, -2.4f, -1.7f, -0.9f, 0.0f, 0.9f, 1.7f, 2.4f, 2.9f, 3.3f };
        float[] cbeam = {  0.62f, 0.82f, 0.98f, 1.05f, 1.05f, 1.00f, 0.85f, 0.60f, 0.34f, 0.08f };
        float[] csheer= {  1.15f, 0.95f, 0.62f, 0.50f, 0.45f, 0.50f, 0.60f, 0.72f, 0.85f, 0.95f };
        float[] ckeel = { -0.35f,-0.50f,-0.60f,-0.65f,-0.65f,-0.63f,-0.58f,-0.45f,-0.28f,-0.05f };

        const int N = 30;  // lofted stations
        const int M = 11;  // points per cross-section (port top -> keel -> stbd top)

        var verts = new List<Vector3>();
        var ringStart = new int[N];

        for (int i = 0; i < N; i++)
        {
            float z = Mathf.Lerp(cz[0], cz[cz.Length - 1], i / (float)(N - 1));
            float beam  = Sample(cz, cbeam,  z);
            float sheer = Sample(cz, csheer, z);
            float keel  = Sample(cz, ckeel,  z);

            ringStart[i] = verts.Count;
            for (int k = 0; k < M; k++)
            {
                float phi = Mathf.PI * k / (M - 1);            // 0..pi
                float x = -beam * Mathf.Cos(phi);              // -beam -> +beam
                // Clamp sin >= 0: at phi==pi, Mathf.Sin returns a tiny NEGATIVE
                // float, and Pow(negative, 1.4) is NaN -> corrupts the mesh.
                float s = Mathf.Max(0f, Mathf.Sin(phi));
                float y = sheer - (sheer - keel) * Mathf.Pow(s, 1.4f);
                verts.Add(new Vector3(x, y, z));
            }
        }

        var tris = new List<int>();

        // Hull shell between adjacent stations.
        for (int i = 0; i < N - 1; i++)
            for (int k = 0; k < M - 1; k++)
            {
                int a = ringStart[i] + k,     b = ringStart[i] + k + 1;
                int c = ringStart[i + 1] + k, d = ringStart[i + 1] + k + 1;
                tris.Add(a); tris.Add(c); tris.Add(d);
                tris.Add(a); tris.Add(d); tris.Add(b);
            }

        // Deck: flat span across the two top edges (k=0 port, k=M-1 stbd), flush
        // with the gunwale so the hull is decked over. Follows the sheer, so it
        // rises into the castles at bow and stern.
        for (int i = 0; i < N - 1; i++)
        {
            int p0 = ringStart[i],     s0 = ringStart[i] + M - 1;
            int p1 = ringStart[i + 1], s1 = ringStart[i + 1] + M - 1;
            tris.Add(p0); tris.Add(p1); tris.Add(s1);
            tris.Add(p0); tris.Add(s1); tris.Add(s0);
        }

        // End caps (stern & bow): fan the ring to its centroid so there are no holes.
        AddEndCap(verts, tris, ringStart[0], M);
        AddEndCap(verts, tris, ringStart[N - 1], M);

        var vArr = verts.ToArray();
        FixWinding(vArr, tris); // make every face point radially outward

        var mesh = LoadOrNew(HullMeshPath, "ShipHull");
        mesh.Clear();
        mesh.SetVertices(new List<Vector3>(vArr));
        mesh.SetTriangles(tris, 0);
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        SaveMesh(mesh, HullMeshPath);
        return mesh;
    }

    static void AddEndCap(List<Vector3> verts, List<int> tris, int start, int M)
    {
        Vector3 c = Vector3.zero;
        for (int k = 0; k < M; k++) c += verts[start + k];
        c /= M;
        int center = verts.Count;
        verts.Add(c);
        for (int k = 0; k < M - 1; k++)
        {
            tris.Add(center); tris.Add(start + k); tris.Add(start + k + 1);
        }
        // close the top edge (stbd-top back to port-top) across the deck line
        tris.Add(center); tris.Add(start + M - 1); tris.Add(start);
    }

    // Flip any triangle whose normal points inward so every face (and thus
    // RecalculateNormals) points outward. "Outward" = away from the hull's
    // overall centre, which is robust for a convex-ish shell including the
    // bow/stern caps (a fixed axis reference is not).
    static void FixWinding(Vector3[] v, List<int> tris)
    {
        Vector3 center = Vector3.zero;
        for (int i = 0; i < v.Length; i++) center += v[i];
        center /= v.Length;

        for (int t = 0; t < tris.Count; t += 3)
        {
            Vector3 a = v[tris[t]], b = v[tris[t + 1]], c = v[tris[t + 2]];
            Vector3 n = Vector3.Cross(b - a, c - a);
            Vector3 outward = (a + b + c) / 3f - center;
            if (Vector3.Dot(n, outward) < 0f)
            {
                (tris[t + 1], tris[t + 2]) = (tris[t + 2], tris[t + 1]);
            }
        }
    }

    // -------------------------------------------------------------------------
    // Sails
    // -------------------------------------------------------------------------
    // Unit curved square sail in the XY plane (1x1), bellied +z. Instances scale
    // it: X=width, Y=height, Z=belly depth.
    static Mesh BuildSquareSailMesh()
    {
        const int nx = 9, ny = 6;
        var verts = new Vector3[nx * ny];
        var uv = new Vector2[nx * ny];
        for (int j = 0; j < ny; j++)
            for (int i = 0; i < nx; i++)
            {
                float u = i / (float)(nx - 1), w = j / (float)(ny - 1);
                float z = Mathf.Sin(Mathf.PI * u) * (0.4f + 0.6f * Mathf.Sin(Mathf.PI * w));
                verts[j * nx + i] = new Vector3(u - 0.5f, w - 0.5f, z);
                uv[j * nx + i] = new Vector2(u, w);
            }

        var tris = new List<int>();
        for (int j = 0; j < ny - 1; j++)
            for (int i = 0; i < nx - 1; i++)
            {
                int a = j * nx + i, b = a + 1, c = a + nx, d = c + 1;
                tris.Add(a); tris.Add(c); tris.Add(d);
                tris.Add(a); tris.Add(d); tris.Add(b);
            }

        var mesh = LoadOrNew(SquareSailPath, "ShipSailSquare");
        mesh.Clear();
        mesh.vertices = verts;
        mesh.uv = uv;
        mesh.SetTriangles(tris, 0);
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        SaveMesh(mesh, SquareSailPath);
        return mesh;
    }

    // Triangular lateen mizzen, built directly in ship-local coordinates (so the
    // instance uses an identity transform). Lies in the x=0 plane, bellied ±x.
    static Mesh BuildLateenSailMesh()
    {
        Vector3 A = new Vector3(0f, 0.85f, -1.35f); // tack (low, forward, near mast foot)
        Vector3 B = new Vector3(0f, 3.55f, -0.95f); // peak (high on the yard)
        Vector3 C = new Vector3(0f, 1.45f, -3.35f); // clew (aft, low)
        const int div = 6;
        const float amp = 0.28f;

        var verts = new List<Vector3>();
        var index = new Dictionary<(int, int), int>();
        for (int a = 0; a <= div; a++)
            for (int b = 0; b <= div - a; b++)
            {
                int cc = div - a - b;
                float wa = a / (float)div, wb = b / (float)div, wc = cc / (float)div;
                Vector3 p = wa * A + wb * B + wc * C;
                p.x = amp * (wa * wb * wc) * 27f; // belly out, peaks at the centre
                index[(a, b)] = verts.Count;
                verts.Add(p);
            }

        var tris = new List<int>();
        for (int a = 0; a < div; a++)
            for (int b = 0; b < div - a; b++)
            {
                int i0 = index[(a, b)], i1 = index[(a + 1, b)], i2 = index[(a, b + 1)];
                tris.Add(i0); tris.Add(i1); tris.Add(i2);
                if (b < div - a - 1)
                {
                    int i3 = index[(a + 1, b + 1)];
                    tris.Add(i1); tris.Add(i3); tris.Add(i2);
                }
            }

        var mesh = LoadOrNew(LateenSailPath, "ShipSailLateen");
        mesh.Clear();
        mesh.SetVertices(verts);
        mesh.SetTriangles(tris, 0);
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        SaveMesh(mesh, LateenSailPath);
        return mesh;
    }

    static void AddSail(Transform parent, string name, Mesh mesh, Vector3 pos, Vector3 scale, Quaternion rot, Material mat)
    {
        var go = new GameObject(name, typeof(MeshFilter), typeof(MeshRenderer));
        go.transform.SetParent(parent, false);
        go.transform.localPosition = pos;
        go.transform.localRotation = rot;
        go.transform.localScale = scale;
        go.GetComponent<MeshFilter>().sharedMesh = mesh;
        go.GetComponent<MeshRenderer>().sharedMaterial = mat;
    }

    static void AddPennant(Transform parent, string name, Vector3 pos, Material mat)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Quad);
        go.name = name;
        Object.DestroyImmediate(go.GetComponent<Collider>());
        go.transform.SetParent(parent, false);
        go.transform.localPosition = pos + new Vector3(0f, 0.25f, 0.3f);
        go.transform.localRotation = Quaternion.Euler(0f, 90f, 0f);
        go.transform.localScale = new Vector3(0.6f, 0.22f, 1f);
        go.GetComponent<MeshRenderer>().sharedMaterial = mat;
    }

    // -------------------------------------------------------------------------
    // Spars (cylinders). `dir` is the axis; length is measured along it from base.
    // -------------------------------------------------------------------------
    static GameObject Spar(Transform parent, string name, Vector3 basePos, Vector3 dir, float dia, float length, Material mat)
    {
        Vector3 center = basePos + dir.normalized * (length * 0.5f);
        return Bar(parent, name, center, dir, dia, length, mat);
    }

    // Cylinder centred at `center`, axis `dir`, given diameter and length.
    static GameObject Bar(Transform parent, string name, Vector3 center, Vector3 dir, float dia, float length, Material mat)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        go.name = name;
        Object.DestroyImmediate(go.GetComponent<Collider>());
        go.transform.SetParent(parent, false);
        go.transform.localPosition = center;
        go.transform.localRotation = Quaternion.FromToRotation(Vector3.up, dir.normalized);
        go.transform.localScale = new Vector3(dia, length * 0.5f, dia); // built-in cylinder is 2 units tall
        go.GetComponent<MeshRenderer>().sharedMaterial = mat;
        return go;
    }

    // -------------------------------------------------------------------------
    // Helpers
    // -------------------------------------------------------------------------
    static float Sample(float[] xs, float[] ys, float x)
    {
        if (x <= xs[0]) return ys[0];
        if (x >= xs[xs.Length - 1]) return ys[ys.Length - 1];
        for (int i = 0; i < xs.Length - 1; i++)
            if (x >= xs[i] && x <= xs[i + 1])
            {
                float t = (x - xs[i]) / (xs[i + 1] - xs[i]);
                return Mathf.Lerp(ys[i], ys[i + 1], t);
            }
        return ys[ys.Length - 1];
    }

    static Mesh LoadOrNew(string path, string name)
    {
        var existing = AssetDatabase.LoadAssetAtPath<Mesh>(path);
        return existing != null ? existing : new Mesh { name = name };
    }

    static void SaveMesh(Mesh mesh, string path)
    {
        Directory.CreateDirectory("Assets/Models");
        if (AssetDatabase.LoadAssetAtPath<Mesh>(path) == null)
            AssetDatabase.CreateAsset(mesh, path);
        else
            EditorUtility.SetDirty(mesh);
    }

    static Material MakeMat(string path, Color color, float smoothness, bool doubleSided)
    {
        var shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null) shader = Shader.Find("Standard");
        var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (mat == null) { mat = new Material(shader); AssetDatabase.CreateAsset(mat, path); }
        mat.shader = shader;
        mat.color = color;
        if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", color);
        if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", smoothness);
        if (doubleSided && mat.HasProperty("_Cull")) mat.SetFloat("_Cull", 0f); // render both faces (sails/flags)
        return mat;
    }
}
