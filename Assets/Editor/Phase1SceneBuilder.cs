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
        boat.transform.position = GeoProjection.LatLonToWorld(38f, -11f) + Vector3.up * 6f; // Atlantic, just west of Lisbon
        var rb = boat.AddComponent<Rigidbody>();
        rb.mass = 40f;
        rb.useGravity = false;
        rb.linearDamping = 0.2f;
        rb.angularDamping = 1.5f;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic; // don't tunnel through coasts
        rb.centerOfMass = new Vector3(0f, -0.6f, 0f);                          // keel: resists flipping
        boat.AddComponent<BoxCollider>().size = new Vector3(2f, 0.8f, 5f);

        AddShipVisual(boat.transform, hullMat);

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
        bc.windForce = 700f;     // tailwind push when the sail is up
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

        var compass = world.AddComponent<Compass>();
        compass.boat = boat.transform;

        var mapScreen = world.AddComponent<MapScreen>();
        mapScreen.boat = boat.transform;
        mapScreen.mapTexture = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Textures/worldmap.jpg");

        world.AddComponent<GameClock>(); // 1480-01-01, 1 day per real minute

        // --- Land (real geography from the Blue Marble map) ---
        LandBuilder.Build();

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
            follow.distance = 24f;
            follow.pitch = 24f;
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

    // Simple cube boat visual. (A proper ship model is parked until the glTF
    // importer is stable in this project.)
    static void AddShipVisual(Transform boat, Material hullMat)
    {
        MakeChildCube(boat, "Hull", Vector3.zero, new Vector3(2f, 0.8f, 5f), hullMat);
        MakeChildCube(boat, "Bow", new Vector3(0f, 0.5f, 2.4f), new Vector3(0.4f, 0.4f, 0.6f), hullMat);
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
