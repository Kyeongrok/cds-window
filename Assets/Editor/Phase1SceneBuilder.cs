using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;

// Phase 1 scene: the Phase 0 boat/sea + all 226 cities placed from cities.json,
// with the sea following the boat so the wide map is sailable.
// Menu: Tools > CDS > Build Phase 1 Scene
public static class Phase1SceneBuilder
{
    const string ScenePath = "Assets/Scenes/Phase1.unity";

    [MenuItem("Tools/CDS/Build Phase 1 Scene")]
    public static void Build()
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

        var waterMat = MakeMaterial("Assets/Settings/WaterMaterial.mat", new Color(0.10f, 0.35f, 0.55f), 0.9f);
        var hullMat = MakeMaterial("Assets/Settings/HullMaterial.mat", new Color(0.45f, 0.30f, 0.18f), 0.2f);
        var cityMat = MakeMaterial("Assets/Settings/CityMaterial.mat", new Color(0.95f, 0.55f, 0.15f), 0.3f);

        // --- Boat (start at map origin = Lisbon) ---
        var boat = new GameObject("Boat");
        boat.transform.position = new Vector3(0f, 6f, 0f);
        var rb = boat.AddComponent<Rigidbody>();
        rb.mass = 40f;
        rb.useGravity = false;
        rb.linearDamping = 0.2f;
        rb.angularDamping = 1.5f;
        boat.AddComponent<BoxCollider>().size = new Vector3(2f, 0.8f, 5f);

        var hull = MakeChildCube(boat.transform, "Hull", Vector3.zero, new Vector3(2f, 0.8f, 5f), hullMat);
        MakeChildCube(boat.transform, "Bow", new Vector3(0f, 0.5f, 2.4f), new Vector3(0.4f, 0.4f, 0.6f), hullMat);

        var probes = new List<Transform>();
        float px = 0.9f, pz = 2.2f, py = -0.4f;
        foreach (var c in new[]
        {
            new Vector3(-px, py, -pz), new Vector3(px, py, -pz),
            new Vector3(-px, py, pz),  new Vector3(px, py, pz),
        })
        {
            var p = new GameObject("Probe").transform;
            p.SetParent(boat.transform, false);
            p.localPosition = c;
            probes.Add(p);
        }

        var buoyancy = boat.AddComponent<Buoyancy>();
        buoyancy.floatProbes = probes.ToArray();
        var bc = boat.AddComponent<BoatController>();
        bc.thrust = 600f;        // faster for map travel
        bc.steerTorque = 220f;

        // --- Sea (follows the boat) ---
        var water = new GameObject("Water", typeof(MeshFilter), typeof(MeshRenderer), typeof(WaveField), typeof(WaterPlane));
        water.GetComponent<MeshRenderer>().sharedMaterial = waterMat;
        var wp = water.GetComponent<WaterPlane>();
        wp.size = 400f;
        wp.resolution = 80;
        wp.follow = boat.transform;
        wp.BuildMesh();

        // --- World map / cities ---
        var world = new GameObject("World");
        var map = world.AddComponent<WorldMap>();
        map.boat = boat.transform;
        map.markerMaterial = cityMat;

        // --- Camera ---
        var cam = Object.FindFirstObjectByType<Camera>();
        if (cam != null)
        {
            cam.farClipPlane = 3000f;
            if (cam.GetComponent<UniversalAdditionalCameraData>() == null)
                cam.gameObject.AddComponent<UniversalAdditionalCameraData>();
            var follow = cam.gameObject.GetComponent<CameraFollow>();
            if (follow == null) follow = cam.gameObject.AddComponent<CameraFollow>();
            follow.target = boat.transform;
            follow.offset = new Vector3(0f, 9f, -18f);
            cam.transform.position = new Vector3(0f, 15f, -18f);
        }

        var light = Object.FindFirstObjectByType<Light>();
        if (light != null) light.transform.rotation = Quaternion.Euler(45f, 30f, 0f);

        Directory.CreateDirectory("Assets/Scenes");
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene, ScenePath);

        EditorBuildSettings.scenes = new[]
        {
            new EditorBuildSettingsScene(ScenePath, true),
            new EditorBuildSettingsScene("Assets/Scenes/Phase0.unity", true),
        };
        AssetDatabase.SaveAssets();
        Debug.Log("Phase1SceneBuilder: built " + ScenePath);
    }

    static GameObject MakeChildCube(Transform parent, string name, Vector3 localPos, Vector3 scale, Material mat)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = name;
        Object.DestroyImmediate(go.GetComponent<BoxCollider>());
        go.transform.SetParent(parent, false);
        go.transform.localPosition = localPos;
        go.transform.localScale = scale;
        go.GetComponent<MeshRenderer>().sharedMaterial = mat;
        return go;
    }

    static Material MakeMaterial(string path, Color color, float smoothness)
    {
        var shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null) shader = Shader.Find("Standard");
        var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (mat == null)
        {
            mat = new Material(shader);
            AssetDatabase.CreateAsset(mat, path);
        }
        mat.shader = shader;
        mat.color = color;
        if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", smoothness);
        if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", color);
        return mat;
    }
}
