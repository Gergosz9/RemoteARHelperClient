using UnityEngine;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Meta.XR.MRUtilityKit;
using System.Collections;
using System.Linq;

public class EnhancedAutoRoomMeshExporter : MonoBehaviour
{
    [Header("Settings")]
    public string fileName = "room_mesh";
    public float autoExportDelay = 5f;
    public bool enableDebugLogs = true;

    [Header("Scene Mesh Access Methods")]
    [Tooltip("Try to access mesh data directly from OVR Scene components")]
    public bool useOVRSceneAPI = true;
    [Tooltip("Try to access mesh data directly from MRUK room geometry")]
    public bool accessDirectMeshData = true;
    [Tooltip("Generate procedural mesh from room layout as fallback")]
    public bool generateRoomLayout = true;

    [Header("Export Options")]
    [Tooltip("Export as separate objects per anchor")]
    public bool exportSeparateObjects = true;
    [Tooltip("Merge all meshes into single object")]
    public bool mergeAllMeshes = false;

    void Start()
    {
        StartCoroutine(AutoExport());
    }

    private IEnumerator AutoExport()
    {
        if (enableDebugLogs) Debug.Log("🔍 Waiting for MRUK Instance...");

        // Wait for MRUK
        while (MRUK.Instance == null)
        {
            yield return new WaitForSeconds(0.5f);
        }

        if (enableDebugLogs) Debug.Log("✅ MRUK Instance found!");

        // Wait for room
        while (MRUK.Instance.GetCurrentRoom() == null)
        {
            if (enableDebugLogs) Debug.Log("⏳ Waiting for room data...");
            yield return new WaitForSeconds(0.5f);
        }

        var room = MRUK.Instance.GetCurrentRoom();
        if (enableDebugLogs) Debug.Log($"🏠 Room found with {room.Anchors.Count} anchors");

        // Wait longer for mesh data to be available
        if (enableDebugLogs) Debug.Log($"⏳ Waiting {autoExportDelay} seconds for mesh data...");
        yield return new WaitForSeconds(autoExportDelay);

        // Debug what we have
        DebugSceneInfo();

        // Try export
        ExportRoomMesh();
    }

    private void DebugSceneInfo()
    {
        if (!enableDebugLogs) return;

        var room = MRUK.Instance?.GetCurrentRoom();
        if (room == null)
        {
            Debug.Log("❌ No room available");
            return;
        }

        Debug.Log("=== QUEST SCENE MESH ACCESS DEBUG ===");

        // Check MeshFilters
        var allMeshFilters = FindObjectsByType<MeshFilter>(FindObjectsSortMode.None);
        Debug.Log($"🔍 Unity MeshFilters in scene: {allMeshFilters.Length}");

        // Check for OVR Scene components
        CheckOVRSceneComponents();

        // Check MRUK room data directly
        Debug.Log($"🏠 MRUK Room anchors: {room.Anchors.Count}");
        
        // Check each anchor for geometry data
        foreach (var anchor in room.Anchors)
        {
            Debug.Log($"🏷️ Anchor: {anchor.Label} at {anchor.transform.position}");
            
            // Try to access mesh data through different methods
            TryAccessAnchorMeshData(anchor);
        }

        // Check room bounds/layout
        var roomBounds = GetRoomBounds(room);
        if (roomBounds.HasValue)
        {
            Debug.Log($"📏 Room bounds: {roomBounds.Value.size} at {roomBounds.Value.center}");
        }

        Debug.Log("========================");
    }

    private void TryAccessAnchorMeshData(MRUKAnchor anchor)
    {
        try
        {
            // Method 1: Check for OVR Scene components
            CheckForOVRSceneComponents(anchor);

            // Method 2: Check for TriangleMesh components
            CheckForTriangleMeshComponents(anchor);

            // Method 3: Check for standard mesh components
            CheckForMeshComponents(anchor);

            // Method 4: Check for mesh data properties via reflection
            var meshDataProperty = anchor.GetType().GetProperty("MeshData");
            if (meshDataProperty != null)
            {
                var meshData = meshDataProperty.GetValue(anchor);
                Debug.Log($"  🔍 Found MeshData property: {meshData?.GetType()}");
            }

            // Method 5: Check bounds properties
            CheckAnchorBounds(anchor);

            // Method 6: Check all properties for anything mesh-related
            CheckAllMeshRelatedProperties(anchor);

        }
        catch (System.Exception e)
        {
            Debug.Log($"  ❌ Error accessing anchor data: {e.Message}");
        }
    }

    private void CheckForOVRSceneComponents(MRUKAnchor anchor)
    {
        // Check for OVRSceneAnchor component
        var ovrSceneAnchor = anchor.GetComponent<MonoBehaviour>();
        if (ovrSceneAnchor != null && ovrSceneAnchor.GetType().Name.Contains("OVRScene"))
        {
            Debug.Log($"  🎯 Found OVR Scene component: {ovrSceneAnchor.GetType().Name}");
            
            // Try to access mesh data from OVR component
            TryAccessOVRMeshData(ovrSceneAnchor);
        }

        // Check children for OVR components
        var ovrComponents = anchor.GetComponentsInChildren<MonoBehaviour>()
            .Where(c => c.GetType().Name.Contains("OVR") || c.GetType().Name.Contains("Scene"))
            .ToArray();

        foreach (var comp in ovrComponents)
        {
            Debug.Log($"  🔍 OVR Component: {comp.GetType().Name} on {comp.name}");
            TryAccessOVRMeshData(comp);
        }
    }

    private void TryAccessOVRMeshData(MonoBehaviour ovrComponent)
    {
        var type = ovrComponent.GetType();
        
        // Look for mesh-related properties and methods
        var meshProperties = type.GetProperties()
            .Where(p => p.Name.ToLower().Contains("mesh") || 
                       p.Name.ToLower().Contains("triangle") ||
                       p.Name.ToLower().Contains("vertex"))
            .ToArray();

        foreach (var prop in meshProperties)
        {
            try
            {
                var value = prop.GetValue(ovrComponent);
                Debug.Log($"    🔧 {prop.Name}: {value?.GetType()} = {value}");
                
                // If it's a mesh-like object, try to extract data
                if (value != null)
                {
                    TryExtractMeshFromObject(value, prop.Name);
                }
            }
            catch (System.Exception e)
            {
                Debug.Log($"    ❌ Could not access {prop.Name}: {e.Message}");
            }
        }

        // Look for mesh-related methods
        var meshMethods = type.GetMethods()
            .Where(m => m.Name.ToLower().Contains("mesh") || 
                       m.Name.ToLower().Contains("triangle") ||
                       m.Name.ToLower().Contains("vertex"))
            .ToArray();

        foreach (var method in meshMethods)
        {
            Debug.Log($"    🔧 Method: {method.Name}");
        }
    }

    private void TryExtractMeshFromObject(object meshObject, string propertyName)
    {
        if (meshObject == null) return;

        var type = meshObject.GetType();
        Debug.Log($"      🔍 Analyzing {propertyName} object type: {type.Name}");

        // Check if it has vertex data
        var vertexProperty = type.GetProperty("vertices") ?? type.GetProperty("Vertices");
        if (vertexProperty != null)
        {
            try
            {
                var vertices = vertexProperty.GetValue(meshObject);
                Debug.Log($"        ✅ Found vertices: {vertices}");
            }
            catch (System.Exception e)
            {
                Debug.Log($"        ❌ Could not access vertices: {e.Message}");
            }
        }

        // Check if it has triangle data
        var triangleProperty = type.GetProperty("triangles") ?? type.GetProperty("Triangles");
        if (triangleProperty != null)
        {
            try
            {
                var triangles = triangleProperty.GetValue(meshObject);
                Debug.Log($"        ✅ Found triangles: {triangles}");
            }
            catch (System.Exception e)
            {
                Debug.Log($"        ❌ Could not access triangles: {e.Message}");
            }
        }

        // List all properties of this mesh object
        var properties = type.GetProperties();
        foreach (var prop in properties)
        {
            try
            {
                var value = prop.GetValue(meshObject);
                Debug.Log($"        📋 {prop.Name}: {value}");
            }
            catch { /* ignore errors */ }
        }
    }

    private void CheckForTriangleMeshComponents(MRUKAnchor anchor)
    {
        // Look for any component with "Triangle" or "Mesh" in the name
        var allComponents = anchor.GetComponentsInChildren<MonoBehaviour>();
        
        foreach (var component in allComponents)
        {
            var typeName = component.GetType().Name;
            if (typeName.ToLower().Contains("triangle") || 
                typeName.ToLower().Contains("mesh"))
            {
                Debug.Log($"  🔺 Found mesh-related component: {typeName}");
                TryAccessOVRMeshData(component);
            }
        }
    }

    private void CheckForMeshComponents(MRUKAnchor anchor)
    {
        // Check for standard Unity mesh components
        var meshFilter = anchor.GetComponent<MeshFilter>();
        if (meshFilter?.sharedMesh != null)
        {
            Debug.Log($"  ✅ Found MeshFilter with {meshFilter.sharedMesh.vertexCount} vertices");
        }

        var meshRenderer = anchor.GetComponent<MeshRenderer>();
        if (meshRenderer != null)
        {
            Debug.Log($"  🎨 Found MeshRenderer");
        }

        // Check children
        var childMeshFilters = anchor.GetComponentsInChildren<MeshFilter>();
        foreach (var mf in childMeshFilters)
        {
            if (mf.sharedMesh != null)
            {
                Debug.Log($"  ✅ Child MeshFilter with {mf.sharedMesh.vertexCount} vertices on {mf.name}");
            }
        }
    }

    private void CheckAllMeshRelatedProperties(MRUKAnchor anchor)
    {
        var properties = anchor.GetType().GetProperties();
        foreach (var prop in properties)
        {
            if (prop.Name.ToLower().Contains("mesh") || 
                prop.Name.ToLower().Contains("vertex") ||
                prop.Name.ToLower().Contains("triangle") ||
                prop.Name.ToLower().Contains("geometry"))
            {
                try
                {
                    var value = prop.GetValue(anchor);
                    Debug.Log($"  🔍 Property {prop.Name}: {value?.GetType()} = {value}");
                    
                    if (value != null)
                    {
                        TryExtractMeshFromObject(value, prop.Name);
                    }
                }
                catch (System.Exception e)
                {
                    Debug.Log($"  ❌ Could not access {prop.Name}: {e.Message}");
                }
            }
        }
    }

    private void CheckAnchorBounds(MRUKAnchor anchor)
    {
        try
        {
            // Try to get volume bounds
            var volumeBoundsProperty = anchor.GetType().GetProperty("VolumeBounds");
            if (volumeBoundsProperty != null)
            {
                var volumeBounds = volumeBoundsProperty.GetValue(anchor);
                Debug.Log($"  📦 VolumeBounds: {volumeBounds}");
            }

            // Try to get plane rect
            var planeRectProperty = anchor.GetType().GetProperty("PlaneRect");
            if (planeRectProperty != null)
            {
                var planeRect = planeRectProperty.GetValue(anchor);
                Debug.Log($"  📋 PlaneRect: {planeRect}");
            }

            // Check for other bounds-related properties
            var properties = anchor.GetType().GetProperties();
            foreach (var prop in properties)
            {
                if (prop.Name.ToLower().Contains("bound") || 
                    prop.Name.ToLower().Contains("rect") ||
                    prop.Name.ToLower().Contains("size"))
                {
                    try
                    {
                        var value = prop.GetValue(anchor);
                        Debug.Log($"  📐 {prop.Name}: {value}");
                    }
                    catch { /* ignore errors */ }
                }
            }
        }
        catch (System.Exception e)
        {
            Debug.Log($"  ❌ Error checking bounds: {e.Message}");
        }
    }

    private Bounds? GetRoomBounds(MRUKRoom room)
    {
        try
        {
            // Try to get overall room bounds
            var boundsProperty = room.GetType().GetProperty("RoomBounds");
            if (boundsProperty != null)
            {
                var bounds = boundsProperty.GetValue(room);
                if (bounds is Bounds)
                    return (Bounds)bounds;
            }

            // Calculate bounds from anchors
            if (room.Anchors.Count > 0)
            {
                var positions = room.Anchors.Select(a => a.transform.position).ToList();
                var min = new Vector3(
                    positions.Min(p => p.x),
                    positions.Min(p => p.y),
                    positions.Min(p => p.z)
                );
                var max = new Vector3(
                    positions.Max(p => p.x),
                    positions.Max(p => p.y),
                    positions.Max(p => p.z)
                );
                var center = (min + max) / 2;
                var size = max - min;
                return new Bounds(center, size);
            }
        }
        catch (System.Exception e)
        {
            Debug.Log($"❌ Error getting room bounds: {e.Message}");
        }

        return null;
    }

    private void CheckOVRSceneComponents()
    {
        // Look for any OVR or Scene Understanding components in the entire scene
        var allComponents = FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None);
        
        var ovrComponents = allComponents
            .Where(c => c.GetType().Name.Contains("OVR") && 
                       (c.GetType().Name.Contains("Scene") || c.GetType().Name.Contains("Mesh")))
            .ToArray();

        Debug.Log($"🔍 Found {ovrComponents.Length} potential OVR/Scene components:");
        
        foreach (var component in ovrComponents)
        {
            Debug.Log($"  🔧 {component.GetType().Name} on {component.name}");
            
            // Check if it has mesh-related methods or properties
            var type = component.GetType();
            
            var meshMethods = type.GetMethods().Where(m => 
                m.Name.ToLower().Contains("mesh") || 
                m.Name.ToLower().Contains("geometry") ||
                m.Name.ToLower().Contains("vertex") ||
                m.Name.ToLower().Contains("triangle")).ToArray();
            
            foreach (var method in meshMethods)
            {
                Debug.Log($"    🔧 Method: {method.Name}");
            }

            var meshProperties = type.GetProperties().Where(p => 
                p.Name.ToLower().Contains("mesh") || 
                p.Name.ToLower().Contains("geometry") ||
                p.Name.ToLower().Contains("vertex") ||
                p.Name.ToLower().Contains("triangle")).ToArray();
            
            foreach (var prop in meshProperties)
            {
                try
                {
                    var value = prop.GetValue(component);
                    Debug.Log($"    🔧 Property {prop.Name}: {value?.GetType()} = {value}");
                }
                catch (System.Exception e)
                {
                    Debug.Log($"    ❌ Could not access {prop.Name}: {e.Message}");
                }
            }
        }
    }

    [ContextMenu("Export Room Mesh")]
    public void ExportRoomMesh()
    {
        var room = MRUK.Instance?.GetCurrentRoom();
        if (room == null)
        {
            Debug.LogError("❌ No room available for export!");
            return;
        }

        List<Vector3> vertices = new List<Vector3>();
        List<int> triangles = new List<int>();
        List<MeshExportData> meshData = new List<MeshExportData>();
        int vertexOffset = 0;
        int processedMeshes = 0;

        if (enableDebugLogs) Debug.Log("🔍 Attempting to access Quest scene mesh data...");

        // Method 1: Try to access mesh data through OVR Scene API
        if (useOVRSceneAPI)
        {
            var ovrMeshData = TryExtractOVRSceneMeshes();
            if (ovrMeshData.Count > 0)
            {
                meshData.AddRange(ovrMeshData);
                processedMeshes += ovrMeshData.Count;
                
                if (enableDebugLogs)
                    Debug.Log($"✅ Extracted {ovrMeshData.Count} meshes from OVR Scene API");
            }
        }

        // Method 2: Try to access mesh data directly from MRUK
        if (accessDirectMeshData && meshData.Count == 0)
        {
            foreach (var anchor in room.Anchors)
            {
                var anchorMesh = TryExtractMeshFromAnchor(anchor);
                if (anchorMesh != null)
                {
                    var exportData = new MeshExportData
                    {
                        mesh = anchorMesh,
                        transform = anchor.transform,
                        name = anchor.Label.ToString(),
                        anchorLabel = anchor.Label
                    };
                    meshData.Add(exportData);
                    processedMeshes++;
                    
                    if (enableDebugLogs)
                        Debug.Log($"✅ Extracted mesh from {anchor.Label}: {anchorMesh.vertexCount} vertices");
                }
            }
        }

        // Method 3: Generate procedural room layout from anchor positions
        if (generateRoomLayout && meshData.Count == 0)
        {
            if (enableDebugLogs) Debug.Log("🔨 Generating procedural room mesh from anchor data...");
            
            var roomMesh = GenerateRoomMeshFromAnchors(room);
            if (roomMesh != null)
            {
                var exportData = new MeshExportData
                {
                    mesh = roomMesh,
                    transform = this.transform,
                    name = "Generated_Room",
                    anchorLabel = MRUKAnchor.SceneLabels.FLOOR
                };
                meshData.Add(exportData);
                processedMeshes = 1;
                
                if (enableDebugLogs)
                    Debug.Log($"✅ Generated room mesh: {roomMesh.vertexCount} vertices");
            }
        }

        if (meshData.Count == 0)
        {
            Debug.LogError("❌ Could not access any scene mesh data!");
            Debug.LogError("💡 Troubleshooting suggestions:");
            Debug.LogError("   • Ensure Quest Scene Understanding is enabled in device settings");
            Debug.LogError("   • Make sure you have scanned your room recently");
            Debug.LogError("   • Check if OVRSceneManager is properly configured in your scene");
            Debug.LogError("   • Verify that scene mesh rendering is enabled in Quest settings");
            Debug.LogError("   • Try using the latest Meta XR All-in-One SDK");
            return;
        }

        // Convert mesh data to vertices and triangles
        foreach (var data in meshData)
        {
            AddMeshData(data.mesh, data.transform, vertices, triangles, vertexOffset);
            vertexOffset += data.mesh.vertexCount;
        }

        // Export
        string path = Path.Combine(Application.persistentDataPath, $"{fileName}.obj");
        
        if (exportSeparateObjects)
        {
            ExportToOBJWithSeparateObjects(meshData, path);
        }
        else
        {
            ExportToOBJ(vertices, triangles, path);
        }

        Debug.Log($"🎉 SUCCESS! Scene mesh exported!");
        Debug.Log($"📁 File: {path}");
        Debug.Log($"📊 {vertices.Count} vertices, {triangles.Count / 3} triangles from {processedMeshes} sources");
        
        var fileInfo = new FileInfo(path);
        if (fileInfo.Exists)
        {
            Debug.Log($"💾 File size: {fileInfo.Length / 1024}KB");
        }
    }

    private class MeshExportData
    {
        public Mesh mesh;
        public Transform transform;
        public string name;
        public MRUKAnchor.SceneLabels anchorLabel;
    }

    private List<MeshExportData> TryExtractOVRSceneMeshes()
    {
        var meshData = new List<MeshExportData>();
        
        try
        {
            // Look for OVR Scene components that might contain mesh data
            var ovrComponents = FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None)
                .Where(c => c.GetType().Name.Contains("OVR") && 
                           (c.GetType().Name.Contains("Scene") || c.GetType().Name.Contains("Mesh")))
                .ToArray();

            if (enableDebugLogs) Debug.Log($"🔍 Found {ovrComponents.Length} OVR Scene components");

            foreach (var component in ovrComponents)
            {
                var extractedMesh = TryExtractMeshFromOVRComponent(component);
                if (extractedMesh != null)
                {
                    var exportData = new MeshExportData
                    {
                        mesh = extractedMesh,
                        transform = component.transform,
                        name = $"OVR_{component.GetType().Name}",
                        anchorLabel = MRUKAnchor.SceneLabels.OTHER
                    };
                    meshData.Add(exportData);
                }
            }
        }
        catch (System.Exception e)
        {
            if (enableDebugLogs)
                Debug.Log($"❌ Error extracting OVR scene meshes: {e.Message}");
        }
        
        return meshData;
    }

    private Mesh TryExtractMeshFromOVRComponent(MonoBehaviour ovrComponent)
    {
        try
        {
            var type = ovrComponent.GetType();
            
            // Try to find mesh-related properties
            var meshProperties = type.GetProperties()
                .Where(p => p.Name.ToLower().Contains("mesh") || 
                           p.Name.ToLower().Contains("triangle") ||
                           p.Name.ToLower().Contains("vertex"))
                .ToArray();

            foreach (var prop in meshProperties)
            {
                try
                {
                    var value = prop.GetValue(ovrComponent);
                    if (value != null)
                    {
                        // Try to convert to Unity Mesh
                        var mesh = ConvertToUnityMesh(value);
                        if (mesh != null && mesh.vertexCount > 0)
                        {
                            return mesh;
                        }
                    }
                }
                catch { /* continue trying other properties */ }
            }

            // Try to find mesh data through methods
            var meshMethods = type.GetMethods()
                .Where(m => m.Name.ToLower().Contains("getmesh") || 
                           m.Name.ToLower().Contains("mesh") && m.GetParameters().Length == 0)
                .ToArray();

            foreach (var method in meshMethods)
            {
                try
                {
                    var result = method.Invoke(ovrComponent, null);
                    if (result != null)
                    {
                        var mesh = ConvertToUnityMesh(result);
                        if (mesh != null && mesh.vertexCount > 0)
                        {
                            return mesh;
                        }
                    }
                }
                catch { /* continue trying other methods */ }
            }
        }
        catch (System.Exception e)
        {
            if (enableDebugLogs)
                Debug.Log($"❌ Error extracting mesh from {ovrComponent.GetType().Name}: {e.Message}");
        }

        return null;
    }

    private Mesh ConvertToUnityMesh(object meshObject)
    {
        if (meshObject == null) return null;

        try
        {
            // If it's already a Unity Mesh, return it
            if (meshObject is Mesh unityMesh)
                return unityMesh;

            var type = meshObject.GetType();
            
            // Try to extract vertex and triangle data
            Vector3[] vertices = null;
            int[] triangles = null;

            // Look for vertices
            var vertexProperty = type.GetProperty("vertices") ?? 
                               type.GetProperty("Vertices") ??
                               type.GetProperty("points") ??
                               type.GetProperty("Points");
            
            if (vertexProperty != null)
            {
                var vertexData = vertexProperty.GetValue(meshObject);
                vertices = ConvertToVector3Array(vertexData);
            }

            // Look for triangles
            var triangleProperty = type.GetProperty("triangles") ?? 
                                 type.GetProperty("Triangles") ??
                                 type.GetProperty("indices") ??
                                 type.GetProperty("Indices");
            
            if (triangleProperty != null)
            {
                var triangleData = triangleProperty.GetValue(meshObject);
                triangles = ConvertToIntArray(triangleData);
            }

            // Create Unity mesh if we have data
            if (vertices != null && vertices.Length > 0 && triangles != null && triangles.Length > 0)
            {
                var mesh = new Mesh();
                mesh.vertices = vertices;
                mesh.triangles = triangles;
                mesh.RecalculateNormals();
                mesh.RecalculateBounds();
                return mesh;
            }
        }
        catch (System.Exception e)
        {
            if (enableDebugLogs)
                Debug.Log($"❌ Error converting to Unity mesh: {e.Message}");
        }

        return null;
    }

    private Vector3[] ConvertToVector3Array(object vertexData)
    {
        if (vertexData == null) return null;

        try
        {
            // Handle different possible vertex data formats
            if (vertexData is Vector3[] vector3Array)
                return vector3Array;

            if (vertexData is System.Collections.IEnumerable enumerable)
            {
                var list = new List<Vector3>();
                foreach (var item in enumerable)
                {
                    if (item is Vector3 v3)
                        list.Add(v3);
                    // Add more conversion logic here for other vector types
                }
                return list.ToArray();
            }
        }
        catch (System.Exception e)
        {
            if (enableDebugLogs)
                Debug.Log($"❌ Error converting vertex data: {e.Message}");
        }

        return null;
    }

    private int[] ConvertToIntArray(object triangleData)
    {
        if (triangleData == null) return null;

        try
        {
            // Handle different possible triangle data formats
            if (triangleData is int[] intArray)
                return intArray;

            if (triangleData is uint[] uintArray)
                return uintArray.Select(u => (int)u).ToArray();

            if (triangleData is System.Collections.IEnumerable enumerable)
            {
                var list = new List<int>();
                foreach (var item in enumerable)
                {
                    if (item is int i)
                        list.Add(i);
                    else if (item is uint ui)
                        list.Add((int)ui);
                }
                return list.ToArray();
            }
        }
        catch (System.Exception e)
        {
            if (enableDebugLogs)
                Debug.Log($"❌ Error converting triangle data: {e.Message}");
        }

        return null;
    }

    private Mesh TryExtractMeshFromAnchor(MRUKAnchor anchor)
    {
        // Try various methods to extract actual mesh data
        try
        {
            // Method 1: Direct mesh access (if available)
            var meshFilter = anchor.GetComponent<MeshFilter>();
            if (meshFilter?.sharedMesh != null)
                return meshFilter.sharedMesh;

            // Method 2: Check children for mesh data
            var childMeshFilters = anchor.GetComponentsInChildren<MeshFilter>();
            if (childMeshFilters.Length > 0)
            {
                foreach (var mf in childMeshFilters)
                {
                    if (mf.sharedMesh != null)
                        return mf.sharedMesh;
                }
            }

            // Method 3: Try to access mesh data through OVR components on this anchor
            var ovrComponents = anchor.GetComponentsInChildren<MonoBehaviour>()
                .Where(c => c.GetType().Name.Contains("OVR") || c.GetType().Name.Contains("Triangle"))
                .ToArray();

            foreach (var ovrComp in ovrComponents)
            {
                var mesh = TryExtractMeshFromOVRComponent(ovrComp);
                if (mesh != null && mesh.vertexCount > 0)
                    return mesh;
            }
            
        }
        catch (System.Exception e)
        {
            if (enableDebugLogs)
                Debug.Log($"❌ Could not extract mesh from {anchor.Label}: {e.Message}");
        }

        return null;
    }

    private Mesh GenerateRoomMeshFromAnchors(MRUKRoom room)
    {
        // Generate a basic room mesh from anchor positions and labels
        var mesh = new Mesh();
        var vertices = new List<Vector3>();
        var triangles = new List<int>();

        // Group anchors by type
        var walls = room.Anchors.Where(a => a.Label.ToString().Contains("WALL")).ToList();
        var floor = room.Anchors.Where(a => a.Label.ToString().Contains("FLOOR")).FirstOrDefault();
        var ceiling = room.Anchors.Where(a => a.Label.ToString().Contains("CEILING")).FirstOrDefault();

        int vertexOffset = 0;

        // Generate floor mesh
        if (floor != null)
        {
            var floorMesh = GenerateFloorMesh(room);
            if (floorMesh != null)
            {
                vertices.AddRange(floorMesh.vertices);
                foreach (var triangle in floorMesh.triangles)
                    triangles.Add(triangle + vertexOffset);
                vertexOffset += floorMesh.vertexCount;
            }
        }

        // Generate wall meshes
        foreach (var wall in walls)
        {
            var wallMesh = GenerateWallMesh(wall);
            if (wallMesh != null)
            {
                vertices.AddRange(wallMesh.vertices);
                foreach (var triangle in wallMesh.triangles)
                    triangles.Add(triangle + vertexOffset);
                vertexOffset += wallMesh.vertexCount;
            }
        }

        if (vertices.Count > 0)
        {
            mesh.vertices = vertices.ToArray();
            mesh.triangles = triangles.ToArray();
            mesh.RecalculateNormals();
            return mesh;
        }

        return null;
    }

    private Mesh GenerateFloorMesh(MRUKRoom room)
    {
        // Create a simple rectangular floor based on room bounds
        var bounds = GetRoomBounds(room);
        if (!bounds.HasValue) return null;

        var b = bounds.Value;
        var mesh = new Mesh();

        var vertices = new Vector3[]
        {
            new Vector3(b.min.x, b.min.y, b.min.z),
            new Vector3(b.max.x, b.min.y, b.min.z),
            new Vector3(b.max.x, b.min.y, b.max.z),
            new Vector3(b.min.x, b.min.y, b.max.z)
        };

        var triangles = new int[] { 0, 1, 2, 0, 2, 3 };

        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.RecalculateNormals();

        return mesh;
    }

    private Mesh GenerateWallMesh(MRUKAnchor wallAnchor)
    {
        // Generate a simple wall mesh based on anchor position
        var mesh = new Mesh();
        var pos = wallAnchor.transform.position;
        
        // Create a simple wall panel
        var height = 2.5f; // Default wall height
        var width = 1.0f;  // Default wall width

        var vertices = new Vector3[]
        {
            pos + new Vector3(-width/2, 0, 0),
            pos + new Vector3(width/2, 0, 0),
            pos + new Vector3(width/2, height, 0),
            pos + new Vector3(-width/2, height, 0)
        };

        var triangles = new int[] { 0, 1, 2, 0, 2, 3 };

        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.RecalculateNormals();

        return mesh;
    }

    private void AddMeshData(Mesh mesh, Transform meshTransform, List<Vector3> vertices, List<int> triangles, int vertexOffset)
    {
        // Add vertices in world space
        foreach (Vector3 localVertex in mesh.vertices)
        {
            vertices.Add(meshTransform.TransformPoint(localVertex));
        }

        // Add triangles with offset
        foreach (int triangle in mesh.triangles)
        {
            triangles.Add(triangle + vertexOffset);
        }
    }

    private void ExportToOBJWithSeparateObjects(List<MeshExportData> meshData, string path)
    {
        StringBuilder obj = new StringBuilder();

        obj.AppendLine($"# Quest 3 Scene Mesh Export (Enhanced Direct Access)");
        obj.AppendLine($"# Generated: {System.DateTime.Now:yyyy-MM-dd HH:mm:ss}");
        obj.AppendLine($"# User: Enhanced Export System");
        obj.AppendLine($"# SDK: Meta XR All-in-One SDK v78.0");
        obj.AppendLine($"# Type: Scene Understanding Geometry with OVR API Access");
        obj.AppendLine($"# Objects: {meshData.Count}");
        obj.AppendLine();

        int totalVertexOffset = 0;

        foreach (var data in meshData)
        {
            obj.AppendLine($"# Object: {data.name}");
            obj.AppendLine($"# Label: {data.anchorLabel}");
            obj.AppendLine($"# Vertices: {data.mesh.vertexCount}");
            obj.AppendLine($"o {data.name}");

            // Transform vertices to world space and write them
            foreach (Vector3 localVertex in data.mesh.vertices)
            {
                Vector3 worldVertex = data.transform.TransformPoint(localVertex);
                obj.AppendLine($"v {worldVertex.x:F6} {worldVertex.y:F6} {worldVertex.z:F6}");
            }

            // Write faces with proper vertex offset
            for (int i = 0; i < data.mesh.triangles.Length; i += 3)
            {
                int v1 = data.mesh.triangles[i] + totalVertexOffset + 1;
                int v2 = data.mesh.triangles[i + 1] + totalVertexOffset + 1;
                int v3 = data.mesh.triangles[i + 2] + totalVertexOffset + 1;
                obj.AppendLine($"f {v1} {v2} {v3}");
            }

            totalVertexOffset += data.mesh.vertexCount;
            obj.AppendLine();
        }

        File.WriteAllText(path, obj.ToString());
    }

    private void ExportToOBJ(List<Vector3> vertices, List<int> triangles, string path)
    {
        StringBuilder obj = new StringBuilder();

        obj.AppendLine($"# Quest 3 Scene Mesh Export (Enhanced Direct Access)");
        obj.AppendLine($"# Generated: {System.DateTime.Now:yyyy-MM-dd HH:mm:ss}");
        obj.AppendLine($"# User: Enhanced Export System");
        obj.AppendLine($"# SDK: Meta XR All-in-One SDK v78.0");
        obj.AppendLine($"# Type: Scene Understanding Geometry with OVR API Access");
        obj.AppendLine($"# Vertices: {vertices.Count}");
        obj.AppendLine($"# Faces: {triangles.Count / 3}");
        obj.AppendLine();

        // Write vertices
        foreach (Vector3 v in vertices)
        {
            obj.AppendLine($"v {v.x:F6} {v.y:F6} {v.z:F6}");
        }

        // Write faces
        for (int i = 0; i < triangles.Count; i += 3)
        {
            obj.AppendLine($"f {triangles[i] + 1} {triangles[i + 1] + 1} {triangles[i + 2] + 1}");
        }

        File.WriteAllText(path, obj.ToString());
    }

    // Public methods
    public void ManualExport() => ExportRoomMesh();
    public void ShowDebugInfo() => DebugSceneInfo();

    [ContextMenu("Deep Anchor Analysis")]
    public void DeepAnchorAnalysis()
    {
        var room = MRUK.Instance?.GetCurrentRoom();
        if (room == null) return;

        Debug.Log("=== DEEP ANCHOR ANALYSIS ===");
        foreach (var anchor in room.Anchors)
        {
            Debug.Log($"🏷️ Analyzing {anchor.Label}:");
            
            // List ALL properties and their values
            var properties = anchor.GetType().GetProperties();
            foreach (var prop in properties)
            {
                try
                {
                    var value = prop.GetValue(anchor);
                    Debug.Log($"  {prop.Name} ({prop.PropertyType.Name}): {value}");
                }
                catch (System.Exception e)
                {
                    Debug.Log($"  {prop.Name}: Error - {e.Message}");
                }
            }
        }
        Debug.Log("========================");
    }

    [ContextMenu("Find All Scene Components")]
    public void FindAllSceneComponents()
    {
        Debug.Log("=== ALL SCENE COMPONENTS ANALYSIS ===");
        
        var allGameObjects = FindObjectsByType<GameObject>(FindObjectsSortMode.None);
        
        foreach (var go in allGameObjects)
        {
            var components = go.GetComponents<Component>();
            
            foreach (var comp in components)
            {
                if (comp == null) continue;
                
                var typeName = comp.GetType().Name;
                if (typeName.Contains("OVR") || 
                    typeName.Contains("Scene") || 
                    typeName.Contains("Meta") ||
                    typeName.Contains("Triangle") ||
                    typeName.Contains("Mesh") ||
                    typeName.Contains("MRUK"))
                {
                    Debug.Log($"🔍 Found: {typeName} on {go.name}");
                    
                    // Check for mesh-related content
                    if (comp is MonoBehaviour mb)
                    {
                        TryAccessOVRMeshData(mb);
                    }
                }
            }
        }
        
        Debug.Log("========================");
    }
}