using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

// Batch-mode helper: creates URP pipeline assets and activates them.
// Run with: -executeMethod URPSetup.Configure
public static class URPSetup
{
    public static void Configure()
    {
        try
        {
            Directory.CreateDirectory("Assets/Settings");

            // Renderer data (forward renderer)
            var rendererData = ScriptableObject.CreateInstance<UniversalRendererData>();
            AssetDatabase.CreateAsset(rendererData, "Assets/Settings/URP-Renderer.asset");

            // Pipeline asset bound to that renderer
            var urp = UniversalRenderPipelineAsset.Create(rendererData);
            AssetDatabase.CreateAsset(urp, "Assets/Settings/URP-Pipeline.asset");

            AssetDatabase.SaveAssets();

            GraphicsSettings.defaultRenderPipeline = urp;
            QualitySettings.renderPipeline = urp;

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log("URPSetup: URP pipeline created and activated.");
        }
        catch (System.Exception e)
        {
            Debug.LogError("URPSetup failed: " + e);
            EditorApplication.Exit(1);
        }
    }
}
