using UnityEngine;
using System.Collections;
using System.IO;
using System.Text;
using System.Linq;
using Meta.XR.BuildingBlocks;

/// <summary>
/// Official Meta Room Mesh Extractor
/// 
/// This script works with Meta's official RoomMeshController and RoomMeshEvent
/// to extract the actual triangle mesh data from Quest's spatial understanding.
/// 
/// Based on the official Meta XR Building Blocks implementation.
/// </summary>
public class MetaRoomMeshExtractor : MonoBehaviour
{
    [Header("Export Settings")]
    [Tooltip("Filename for exported mesh")]
    public string exportFileName = "meta_room_mesh";
    
    [Tooltip("Auto-extract when room mesh loads")]
    public bool autoExtractOnLoad = true;
    
    [Tooltip("Enable detailed logging")]
    public bool enableLogging = true;

    [Header("Required Components")]
    [Tooltip("RoomMeshController component (will be found automatically)")]
    public RoomMeshController roomMeshController;
    
    [Tooltip("RoomMeshEvent component (will be found automatically)")]
    public RoomMeshEvent roomMeshEvent;

    [Header("Networking (Optional)")]
    [Tooltip("FileSender component for network transmission (optional)")]
    public FileSender fileSender;

    private MeshFilter extractedMeshFilter;
    private bool hasExtracted = false;

    void Start()
    {
        StartCoroutine(InitializeExtractor());
    }

    private IEnumerator InitializeExtractor()
    {
        Log("?? Meta Room Mesh Extractor initializing...");

        // Find required components
        if (roomMeshController == null)
        {
            roomMeshController = FindAnyObjectByType<RoomMeshController>();
        }

        if (roomMeshEvent == null)
        {
            roomMeshEvent = FindAnyObjectByType<RoomMeshEvent>();
        }

        // Check if components exist
        if (roomMeshController == null)
        {
            LogError("? RoomMeshController not found in scene!");
            LogError("?? Add the Room Mesh Building Block prefab to your scene");
            yield break;
        }

        if (roomMeshEvent == null)
        {
            LogError("? RoomMeshEvent not found in scene!");
            LogError("?? Make sure the Room Mesh Building Block is properly configured");
            yield break;
        }

        Log("? Found RoomMeshController and RoomMeshEvent");

        // Subscribe to the room mesh loaded event
        if (autoExtractOnLoad)
        {
            roomMeshEvent.OnRoomMeshLoadCompleted.AddListener(OnRoomMeshLoaded);
            Log("?? Subscribed to room mesh load event");
        }
    }

    /// <summary>
    /// Called when the room mesh loads successfully
    /// </summary>
    /// <param name="meshFilter">The MeshFilter containing the room mesh data</param>
    private void OnRoomMeshLoaded(MeshFilter meshFilter)
    {
        Log("?? Room mesh loaded successfully!");
        
        if (meshFilter == null || meshFilter.sharedMesh == null)
        {
            LogError("? Mesh filter is null or has no mesh data");
            return;
        }

        extractedMeshFilter = meshFilter;
        
        Log($"?? Mesh data: {meshFilter.sharedMesh.vertexCount} vertices, {meshFilter.sharedMesh.triangles.Length / 3} triangles");

        // Extract and export the mesh
        StartCoroutine(ExtractAndExportMesh());
    }

    /// <summary>
    /// Extract and export the mesh data
    /// </summary>
    private IEnumerator ExtractAndExportMesh()
    {
        if (extractedMeshFilter == null || extractedMeshFilter.sharedMesh == null)
        {
            LogError("❌ No mesh data to extract");
            yield break;
        }

        var mesh = extractedMeshFilter.sharedMesh;
        var transform = extractedMeshFilter.transform;

        Log("🔧 Processing mesh data...");
 
        ExportMeshAsOBJ(mesh, transform, exportFileName);
        Log("💾 Original mesh exported");

        hasExtracted = true;
        LogSuccess(mesh);
    }

    /// <summary>
    /// Export mesh as OBJ format
    /// </summary>
    private void ExportMeshAsOBJ(Mesh mesh, Transform meshTransform, string filename)
    {
        string path = Path.Combine(Application.persistentDataPath, $"{filename}.obj");

        var obj = new StringBuilder();
        obj.AppendLine($"# Meta Quest Room Mesh Export (Official Building Blocks)");
        obj.AppendLine($"# Generated: {System.DateTime.Now:yyyy-MM-dd HH:mm:ss}");
        obj.AppendLine($"# Source: Meta.XR.BuildingBlocks.RoomMeshController");
        obj.AppendLine($"# Vertices: {mesh.vertexCount}");
        obj.AppendLine($"# Triangles: {mesh.triangles.Length / 3}");
        obj.AppendLine($"# Has Colors: {mesh.colors.Length > 0}");
        obj.AppendLine($"# Has Normals: {mesh.normals.Length > 0}");
        obj.AppendLine($"# Has UVs: {mesh.uv.Length > 0}");
        obj.AppendLine($"# Coordinate System: Fixed for OBJ format (Z-flipped)");
        obj.AppendLine();

        obj.AppendLine($"o RoomMesh");

        // Write vertices (transform to world space and flip Z for OBJ format)
        foreach (var vertex in mesh.vertices)
        {
            Vector3 worldVertex = meshTransform.TransformPoint(vertex);
            // Flip Z-axis to convert from Unity's left-handed to OBJ's right-handed coordinate system
            obj.AppendLine($"v {worldVertex.x:F6} {worldVertex.y:F6} {-worldVertex.z:F6}");
        }

        // Write normals (transform to world space and flip Z)
        if (mesh.normals.Length > 0)
        {
            foreach (var normal in mesh.normals)
            {
                Vector3 worldNormal = meshTransform.TransformDirection(normal).normalized;
                // Flip Z-axis for normals as well
                obj.AppendLine($"vn {worldNormal.x:F6} {worldNormal.y:F6} {-worldNormal.z:F6}");
            }
        }

        // Write UV coordinates
        if (mesh.uv.Length > 0)
        {
            foreach (var uv in mesh.uv)
            {
                obj.AppendLine($"vt {uv.x:F6} {uv.y:F6}");
            }
        }

        // Write faces (reverse winding order to fix inside-out faces)
        for (int i = 0; i < mesh.triangles.Length; i += 3)
        {
            int v1 = mesh.triangles[i] + 1;
            int v2 = mesh.triangles[i + 1] + 1;
            int v3 = mesh.triangles[i + 2] + 1;

            // Reverse triangle winding order (v1, v3, v2 instead of v1, v2, v3)
            if (mesh.normals.Length > 0 && mesh.uv.Length > 0)
            {
                obj.AppendLine($"f {v1}/{v1}/{v1} {v3}/{v3}/{v3} {v2}/{v2}/{v2}");
            }
            else if (mesh.normals.Length > 0)
            {
                obj.AppendLine($"f {v1}//{v1} {v3}//{v3} {v2}//{v2}");
            }
            else
            {
                obj.AppendLine($"f {v1} {v3} {v2}");
            }
        }

        File.WriteAllText(path, obj.ToString());
        Log($"📁 OBJ exported to: {path}");
        SendObj(path);
    }

    private void SendObj(string filePath)
    {
        if (fileSender != null)
        {
            Log("Sending OBJ file over network...");
            fileSender.SendFileToAll(filePath);
        }
        else
        {
            Log("FileSender component not assigned, skipping network send.");
        }
    }

    #region Manual Extraction Methods

    [ContextMenu("Manual Extract Mesh")]
    public void ManualExtractMesh()
    {
        if (hasExtracted && extractedMeshFilter != null)
        {
            // Re-export existing mesh
            StartCoroutine(ExtractAndExportMesh());
        }
        else
        {
            // Try to find existing room mesh in scene
            var existingMeshFilters = FindObjectsByType<MeshFilter>(FindObjectsSortMode.None);
            
            foreach (var mf in existingMeshFilters)
            {
                if (mf.sharedMesh != null)
                {
                    Log($"?? Found potential room mesh: {mf.name} with {mf.sharedMesh.vertexCount} vertices");

                    extractedMeshFilter = mf;
                    OnRoomMeshLoaded(mf);
                    return;
                }
            }
            
            LogError("? No room mesh found! Make sure RoomMeshController has loaded the mesh first.");
        }
    }

    [ContextMenu("Check Scene Setup")]
    public void CheckSceneSetup()
    {
        Log("=== SCENE SETUP CHECK ===");
        
        // Check for RoomMeshController
        var controllers = FindObjectsByType<RoomMeshController>(FindObjectsSortMode.None);
        Log($"?? Found {controllers.Length} RoomMeshController(s)");
        
        foreach (var controller in controllers)
        {
            Log($"  ?? {controller.name}");
        }
        
        // Check for RoomMeshEvent
        var events = FindObjectsByType<RoomMeshEvent>(FindObjectsSortMode.None);
        Log($"?? Found {events.Length} RoomMeshEvent(s)");
        
        foreach (var evt in events)
        {
            Log($"  ?? {evt.name}");
        }
        
        // Check for existing mesh data
        var allMeshFilters = FindObjectsByType<MeshFilter>(FindObjectsSortMode.None);
        var largeMeshes = allMeshFilters.Where(mf => mf.sharedMesh != null && mf.sharedMesh.vertexCount > 100).ToArray();
        
        Log($"?? Found {largeMeshes.Length} large meshes that could be room data:");
        
        foreach (var mf in largeMeshes)
        {
            var mesh = mf.sharedMesh;
            Log($"  ?? {mf.name}: {mesh.vertexCount}v, {mesh.triangles.Length/3}t, Colors: {mesh.colors.Length > 0}");
        }
        
        // Check permissions
        bool hasScenePermission = OVRPermissionsRequester.IsPermissionGranted(OVRPermissionsRequester.Permission.Scene);
        Log($"?? Scene Permission: {(hasScenePermission ? "? Granted" : "? Not Granted")}");
        
        if (!hasScenePermission)
        {
            LogError("?? Scene permission required! Enable in Quest settings or grant in app.");
        }
        
        Log("========================");
    }

    #endregion

    #region Logging Methods

    private void Log(string message)
    {
        if (enableLogging)
        {
            Debug.Log($"[MetaRoomMeshExtractor] {message}");
        }
    }

    private void LogError(string message)
    {
        Debug.LogError($"[MetaRoomMeshExtractor] {message}");
    }

    private void LogSuccess(Mesh mesh)
    {
        Log("🎉 SUCCESS! Room mesh extraction completed!");
        Log($"📊 Mesh Statistics:");
        Log($"   Vertices: {mesh.vertexCount}");
        Log($"   Triangles: {mesh.triangles.Length / 3}");
        Log($"   Has Colors: {mesh.colors.Length > 0} ({mesh.colors.Length} colors)");
        Log($"   Has Normals: {mesh.normals.Length > 0} ({mesh.normals.Length} normals)");
        Log($"   Has UVs: {mesh.uv.Length > 0} ({mesh.uv.Length} UVs)");
        Log($"📁 Files saved to: {Application.persistentDataPath}");
        
        // Show file paths
        Log($"📄 Exported files:");
        Log($"   {exportFileName}.obj");
    }

    #endregion

    #region Public API

    /// <summary>
    /// Check if mesh has been extracted
    /// </summary>
    public bool HasExtractedMesh()
    {
        return hasExtracted && extractedMeshFilter != null;
    }

    /// <summary>
    /// Get the extracted mesh filter
    /// </summary>
    public MeshFilter GetExtractedMeshFilter()
    {
        return extractedMeshFilter;
    }

    /// <summary>
    /// Get vertex count of extracted mesh
    /// </summary>
    public int GetVertexCount()
    {
        return extractedMeshFilter?.sharedMesh?.vertexCount ?? 0;
    }

    /// <summary>
    /// Get triangle count of extracted mesh
    /// </summary>
    public int GetTriangleCount()
    {
        return extractedMeshFilter?.sharedMesh?.triangles.Length / 3 ?? 0;
    }

    #endregion
}