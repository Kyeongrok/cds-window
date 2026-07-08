using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Builds the Phase 0 playable scene: sea + a drivable, floating boat + chase cam.
// Menu: Tools > CDS > Build Phase 0 Scene   (or run headless via -executeMethod)
public static class Phase0SceneBuilder
{
    const string ScenePath = "Assets/Scenes/Phase0.unity";

    [MenuItem("Tools/CDS/Build Phase 0 Scene")]
    public static void Build()
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

        // --- Materials (URP Lit) ---
        var waterMat = MakeMaterial("Assets/Settings/WaterMaterial.mat", new Color(0.10f, 0.35f, 0.55f), 0.9f);
        var hullMat = MakeMaterial("Assets/Settings/HullMaterial.mat", new Color(0.45f, 0.30f, 0.18f), 0.2f);

        // --- Sea ---
        var water = new GameObject("Water", typeof(MeshFilter), typeof(MeshRenderer), typeof(WaveField), typeof(WaterPlane));
        water.transform.position = Vector3.zero;
        water.GetComponent<MeshRenderer>().sharedMaterial = waterMat;
        water.GetComponent<WaterPlane>().BuildMesh();

        // --- Boat root ---
        var boat = new GameObject("Boat");
        boat.transform.position = new Vector3(0f, 6f, 0f);
        var rb = boat.AddComponent<Rigidbody>();
        rb.mass = 40f;
        rb.useGravity = false;               // Buoyancy applies gravity per-probe
        rb.linearDamping = 0.2f;
        rb.angularDamping = 1.5f;

        var box = boat.AddComponent<BoxCollider>();
        box.size = new Vector3(2f, 0.8f, 5f);

        // Ship visual (procedural caravel: hull + masts + sails)
        ShipBuilder.AddShipVisual(boat.transform, hullMat);

        // Float probes at the four hull-bottom corners
        var probes = new List<Transform>();
        float px = 0.9f, pz = 2.2f, py = -0.4f;
        foreach (var c in new[]
        {
            new Vector3(-px, py, -pz), new Vector3(px, py, -pz),
            new Vector3(-px, py,  pz), new Vector3(px, py,  pz),
        })
        {
            var p = new GameObject("Probe").transform;
            p.SetParent(boat.transform, false);
            p.localPosition = c;
            probes.Add(p);
        }

        var buoyancy = boat.AddComponent<Buoyancy>();
        buoyancy.floatProbes = probes.ToArray();
        boat.AddComponent<BoatController>();

        // --- Camera ---
        var cam = Object.FindFirstObjectByType<Camera>();
        if (cam != null)
        {
            cam.farClipPlane = 2000f;
            var follow = cam.gameObject.GetComponent<CameraFollow>();
            if (follow == null) follow = cam.gameObject.AddComponent<CameraFollow>();
            follow.target = boat.transform;
            cam.transform.position = new Vector3(0f, 13f, -14f);
        }

        // --- Light angle for nice water specular ---
        var light = Object.FindFirstObjectByType<Light>();
        if (light != null) light.transform.rotation = Quaternion.Euler(45f, 30f, 0f);

        // --- Save ---
        Directory.CreateDirectory("Assets/Scenes");
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene, ScenePath);

        EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
        AssetDatabase.SaveAssets();
        Debug.Log("Phase0SceneBuilder: built " + ScenePath);
    }

    static Material MakeMaterial(string path, Color color, float smoothness)
    {
        var shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null) shader = Shader.Find("Standard");
        var mat = new Material(shader);
        mat.color = color;
        if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", smoothness);
        if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", color);
        AssetDatabase.CreateAsset(mat, path);
        return mat;
    }
}
