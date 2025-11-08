using UnityEngine;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Linq;
using Meta.XR.MRUtilityKit;

/// <summary>
/// Quest Spatial Mesh Extractor
/// 
/// This script attempts to extract the actual spatial mesh data from Quest 3
/// using multiple approaches including direct OVR API access and MRUK integration.
/// 
/// Based on the Needle Engine sample approach but adapted for Unity/Meta XR SDK.
/// </summary>
public class QuestSpatialMeshExtractor : MonoBehaviour
{
    [Header("Extraction Settings")]
    [Tooltip("Base filename for exported meshes")]
    public string baseFileName = "quest_spatial_mesh";
    
    [Tooltip("Export format")]
    public ExportFormat exportFormat = ExportFormat.OBJ;
    
    [Tooltip("Auto-extract after delay")]
    public bool autoExtract = true;
    
    [Tooltip("Delay before auto-extraction")]
    public float autoExtractDelay = 8f;
    
    [Tooltip("Enable detailed logging")]
    public bool verboseLogging = true;

    [Header("Mesh Sources")]
    [Tooltip("Try to extract from OVR Scene API")]
    public bool extractFromOVRScene = true;
    
    [Tooltip("Try to extract from MRUK anchors")]
    public bool extractFromMRUK = true;
    
    [Tooltip("Try to extract from Unity MeshFilters")]
    public bool extractFromMeshFilters = true;
    
    [Tooltip("Generate synthetic mesh from room bounds")]
    public bool generateSyntheticMesh = true;

    [Header("Advanced Options")]
    [Tooltip("Use reflection to access private APIs")]
    public bool useReflection = true;
    
    [Tooltip("Search for hidden mesh components")]
    public bool searchHiddenComponents = true;
    
    [Tooltip("Export vertex colors if available")]
    public bool exportVertexColors = false;
    
    [Tooltip("Export UV coordinates")]
    public bool exportUVs = true;

    public enum ExportFormat
    {
        OBJ,
        PLY,
        Both
    }

    private List<SpatialMeshData> extractedMeshes = new List<SpatialMeshData>();
    private MRUKRoom currentRoom;

    [System.Serializable]
    public class SpatialMeshData
    {
        public string meshName;
        public string sourceType;
        public Vector3[] vertices;
        public int[] triangles;
        public Vector3[] normals;
        public Vector2[] uvs;
        public Color[] colors;
        public Transform sourceTransform;
        public Bounds worldBounds;
        public string anchorLabel;
        public int vertexCount => vertices?.Length ?? 0;
        public int triangleCount => triangles?.Length / 3 ?? 0;
    }

    void Start()
    {
        if (autoExtract)
        {
            StartCoroutine(AutoExtractRoutine());
        }
    }

    private System.Collections.IEnumerator AutoExtractRoutine()
    {
        LogVerbose("?? Quest Spatial Mesh Extractor starting...");
        
        // Wait for MRUK to initialize
        while (MRUK.Instance == null)
        {
            LogVerbose("? Waiting for MRUK Instance...");
            yield return new WaitForSeconds(0.5f);
        }

        LogVerbose("? MRUK Instance found");

        // Wait for room data
        while (MRUK.Instance.GetCurrentRoom() == null)
        {
            LogVerbose("? Waiting for room data...");
            yield return new WaitForSeconds(0.5f);
        }

        currentRoom = MRUK.Instance.GetCurrentRoom();
        LogVerbose($"?? Room loaded with {currentRoom.Anchors.Count} anchors");

        // Wait for mesh data to be fully loaded
        LogVerbose($"? Waiting {autoExtractDelay} seconds for spatial data to stabilize...");
        yield return new WaitForSeconds(autoExtractDelay);

        // Attempt extraction
        ExtractSpatialMeshes();
    }

    [ContextMenu("Extract Spatial Meshes")]
    public void ExtractSpatialMeshes()
    {
        LogVerbose("=== QUEST SPATIAL MESH EXTRACTION STARTED ===");
        
        extractedMeshes.Clear();
        
        // Try multiple extraction methods
        if (extractFromOVRScene)
        {
            ExtractFromOVRSceneAPI();
        }
        
        if (extractFromMRUK)
        {
            ExtractFromMRUKAnchors();
        }
        
        if (extractFromMeshFilters)
        {
            ExtractFromMeshFilters();
        }
        
        if (searchHiddenComponents)
        {
            SearchForHiddenMeshes();
        }
        
        if (generateSyntheticMesh && extractedMeshes.Count == 0)
        {
            GenerateSyntheticRoomMesh();
        }

        // Report results
        LogVerbose($"?? Extraction complete: {extractedMeshes.Count} meshes found");
        
        if (extractedMeshes.Count > 0)
        {
            int totalVertices = extractedMeshes.Sum(m => m.vertexCount);
            int totalTriangles = extractedMeshes.Sum(m => m.triangleCount);
            
            LogVerbose($"   Total vertices: {totalVertices}");
            LogVerbose($"   Total triangles: {totalTriangles}");
            
            foreach (var mesh in extractedMeshes)
            {
                LogVerbose($"   ?? {mesh.meshName} ({mesh.sourceType}): {mesh.vertexCount}v, {mesh.triangleCount}t");
            }
            
            // Export the meshes
            ExportMeshes();
        }
        else
        {
            LogError("? No spatial mesh data found!");
            LogSuggestions();
        }
        
        LogVerbose("=== EXTRACTION COMPLETE ===");
    }

    private void ExtractFromOVRSceneAPI()
    {
        LogVerbose("?? Extracting from OVR Scene API...");
        
        try
        {
            // Look for OVRSceneManager or similar components
            var ovrManagers = FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None)
                .Where(c => c.GetType().Name.Contains("OVRScene") || c.GetType().Name.Contains("SceneManager"))
                .ToArray();

            LogVerbose($"Found {ovrManagers.Length} potential OVR scene managers");

            foreach (var manager in ovrManagers)
            {
                ExtractMeshFromOVRComponent(manager, "OVRSceneManager");
            }

            // Look for OVRSceneAnchor components
            var sceneAnchors = FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None)
                .Where(c => c.GetType().Name.Contains("OVRSceneAnchor") || c.GetType().Name.Contains("OVRAnchor"))
                .ToArray();

            LogVerbose($"Found {sceneAnchors.Length} OVR scene anchors");

            foreach (var anchor in sceneAnchors)
            {
                ExtractMeshFromOVRComponent(anchor, "OVRSceneAnchor");
            }

            // Look for triangle mesh components
            var triangleMeshes = FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None)
                .Where(c => c.GetType().Name.Contains("TriangleMesh"))
                .ToArray();

            LogVerbose($"Found {triangleMeshes.Length} triangle mesh components");

            foreach (var triangleMesh in triangleMeshes)
            {
                ExtractMeshFromOVRComponent(triangleMesh, "TriangleMesh");
            }
        }
        catch (System.Exception e)
        {
            LogVerbose($"? Error in OVR Scene API extraction: {e.Message}");
        }
    }

    private void ExtractMeshFromOVRComponent(MonoBehaviour component, string componentType)
    {
        if (component == null) return;

        try
        {
            var type = component.GetType();
            LogVerbose($"  Analyzing {componentType}: {type.Name}");

            // Use reflection to find mesh data
            if (useReflection)
            {
                ExtractUsingReflection(component, type, componentType);
            }

            // Check for standard Unity components
            var meshFilter = component.GetComponent<MeshFilter>();
            if (meshFilter?.sharedMesh != null)
            {
                CreateSpatialMeshData(meshFilter.sharedMesh, component.transform, $"{componentType}_MeshFilter", componentType);
            }

            // Check children
            var childMeshFilters = component.GetComponentsInChildren<MeshFilter>();
            foreach (var childMF in childMeshFilters)
            {
                if (childMF.sharedMesh != null)
                {
                    CreateSpatialMeshData(childMF.sharedMesh, childMF.transform, $"{componentType}_Child_{childMF.name}", componentType);
                }
            }
        }
        catch (System.Exception e)
        {
            LogVerbose($"  ? Error extracting from {componentType}: {e.Message}");
        }
    }

    private void ExtractUsingReflection(MonoBehaviour component, System.Type type, string componentType)
    {
        try
        {
            // Search for properties that might contain mesh data
            var allProperties = type.GetProperties(System.Reflection.BindingFlags.Public | 
                                                  System.Reflection.BindingFlags.NonPublic | 
                                                  System.Reflection.BindingFlags.Instance);

            foreach (var prop in allProperties)
            {
                if (IsMeshRelatedProperty(prop.Name))
                {
                    try
                    {
                        var value = prop.GetValue(component);
                        if (value != null)
                        {
                            LogVerbose($"    Found property {prop.Name}: {value.GetType().Name}");
                            
                            var meshData = ExtractMeshFromObject(value, $"{componentType}_{prop.Name}");
                            if (meshData != null)
                            {
                                meshData.sourceTransform = component.transform;
                                meshData.sourceType = componentType;
                                extractedMeshes.Add(meshData);
                                LogVerbose($"    ? Extracted mesh from {prop.Name}: {meshData.vertexCount} vertices");
                            }
                        }
                    }
                    catch (System.Exception e)
                    {
                        LogVerbose($"    ? Could not access {prop.Name}: {e.Message}");
                    }
                }
            }

            // Search for methods that might return mesh data
            var allMethods = type.GetMethods(System.Reflection.BindingFlags.Public | 
                                           System.Reflection.BindingFlags.NonPublic | 
                                           System.Reflection.BindingFlags.Instance);

            foreach (var method in allMethods)
            {
                if (IsMeshRelatedMethod(method) && method.GetParameters().Length == 0)
                {
                    try
                    {
                        var result = method.Invoke(component, null);
                        if (result != null)
                        {
                            LogVerbose($"    Method {method.Name} returned: {result.GetType().Name}");
                            
                            var meshData = ExtractMeshFromObject(result, $"{componentType}_{method.Name}");
                            if (meshData != null)
                            {
                                meshData.sourceTransform = component.transform;
                                meshData.sourceType = componentType;
                                extractedMeshes.Add(meshData);
                                LogVerbose($"    ? Extracted mesh from method {method.Name}: {meshData.vertexCount} vertices");
                            }
                        }
                    }
                    catch (System.Exception e)
                    {
                        LogVerbose($"    ? Could not invoke {method.Name}: {e.Message}");
                    }
                }
            }
        }
        catch (System.Exception e)
        {
            LogVerbose($"? Reflection error on {componentType}: {e.Message}");
        }
    }

    private bool IsMeshRelatedProperty(string propertyName)
    {
        var lower = propertyName.ToLower();
        return lower.Contains("mesh") || lower.Contains("triangle") || lower.Contains("vertex") ||
               lower.Contains("geometry") || lower.Contains("surface") || lower.Contains("polygon");
    }

    private bool IsMeshRelatedMethod(System.Reflection.MethodInfo method)
    {
        var lower = method.Name.ToLower();
        return (lower.Contains("getmesh") || lower.Contains("mesh") || lower.Contains("triangle") ||
                lower.Contains("geometry") || lower.Contains("surface")) &&
               method.ReturnType != typeof(void);
    }

    private SpatialMeshData ExtractMeshFromObject(object obj, string meshName)
    {
        if (obj == null) return null;

        try
        {
            // If it's already a Unity Mesh
            if (obj is Mesh unityMesh && unityMesh.vertexCount > 0)
            {
                return CreateSpatialMeshData(unityMesh, null, meshName, "UnityMesh");
            }

            // Try to extract from custom mesh objects
            var type = obj.GetType();
            
            Vector3[] vertices = null;
            int[] triangles = null;
            Vector3[] normals = null;
            Vector2[] uvs = null;
            Color[] colors = null;

            // Extract vertices
            vertices = ExtractVector3Array(obj, type, new[] { "vertices", "Vertices", "points", "Points", "positions", "Positions" });
            
            // Extract triangles
            triangles = ExtractIntArray(obj, type, new[] { "triangles", "Triangles", "indices", "Indices", "faces", "Faces" });
            
            // Extract normals (optional)
            normals = ExtractVector3Array(obj, type, new[] { "normals", "Normals" });
            
            // Extract UVs (optional)
            uvs = ExtractVector2Array(obj, type, new[] { "uvs", "UVs", "texCoords", "TexCoords", "uv", "UV" });
            
            // Extract colors (optional)
            colors = ExtractColorArray(obj, type, new[] { "colors", "Colors", "vertexColors", "VertexColors" });

            if (vertices != null && vertices.Length > 0 && triangles != null && triangles.Length > 0)
            {
                var meshData = new SpatialMeshData
                {
                    meshName = meshName,
                    sourceType = type.Name,
                    vertices = vertices,
                    triangles = triangles,
                    normals = normals,
                    uvs = uvs,
                    colors = colors,
                    worldBounds = CalculateBounds(vertices)
                };

                // Generate normals if not available
                if (meshData.normals == null)
                {
                    meshData.normals = CalculateNormals(vertices, triangles);
                }

                return meshData;
            }
        }
        catch (System.Exception e)
        {
            LogVerbose($"? Error extracting mesh from object: {e.Message}");
        }

        return null;
    }

    private Vector3[] ExtractVector3Array(object obj, System.Type type, string[] propertyNames)
    {
        foreach (var propName in propertyNames)
        {
            try
            {
                var prop = type.GetProperty(propName) ?? type.GetProperty(propName, System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (prop != null)
                {
                    var value = prop.GetValue(obj);
                    var result = ConvertToVector3Array(value);
                    if (result != null && result.Length > 0)
                        return result;
                }
            }
            catch { continue; }
        }
        return null;
    }

    private int[] ExtractIntArray(object obj, System.Type type, string[] propertyNames)
    {
        foreach (var propName in propertyNames)
        {
            try
            {
                var prop = type.GetProperty(propName) ?? type.GetProperty(propName, System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (prop != null)
                {
                    var value = prop.GetValue(obj);
                    var result = ConvertToIntArray(value);
                    if (result != null && result.Length > 0)
                        return result;
                }
            }
            catch { continue; }
        }
        return null;
    }

    private Vector2[] ExtractVector2Array(object obj, System.Type type, string[] propertyNames)
    {
        foreach (var propName in propertyNames)
        {
            try
            {
                var prop = type.GetProperty(propName) ?? type.GetProperty(propName, System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (prop != null)
                {
                    var value = prop.GetValue(obj);
                    var result = ConvertToVector2Array(value);
                    if (result != null && result.Length > 0)
                        return result;
                }
            }
            catch { continue; }
        }
        return null;
    }

    private Color[] ExtractColorArray(object obj, System.Type type, string[] propertyNames)
    {
        foreach (var propName in propertyNames)
        {
            try
            {
                var prop = type.GetProperty(propName) ?? type.GetProperty(propName, System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (prop != null)
                {
                    var value = prop.GetValue(obj);
                    var result = ConvertToColorArray(value);
                    if (result != null && result.Length > 0)
                        return result;
                }
            }
            catch { continue; }
        }
        return null;
    }

    private void ExtractFromMRUKAnchors()
    {
        LogVerbose("?? Extracting from MRUK anchors...");
        
        if (currentRoom == null)
        {
            LogVerbose("? No MRUK room available");
            return;
        }

        foreach (var anchor in currentRoom.Anchors)
        {
            try
            {
                LogVerbose($"  Checking anchor: {anchor.Label}");
                
                // Check for mesh components on the anchor
                var meshFilter = anchor.GetComponent<MeshFilter>();
                if (meshFilter?.sharedMesh != null)
                {
                    CreateSpatialMeshData(meshFilter.sharedMesh, anchor.transform, $"MRUK_{anchor.Label}", "MRUK", anchor.Label.ToString());
                    LogVerbose($"    ? Found mesh on {anchor.Label}: {meshFilter.sharedMesh.vertexCount} vertices");
                }

                // Check children
                var childMeshFilters = anchor.GetComponentsInChildren<MeshFilter>();
                foreach (var childMF in childMeshFilters)
                {
                    if (childMF.sharedMesh != null)
                    {
                        CreateSpatialMeshData(childMF.sharedMesh, childMF.transform, $"MRUK_{anchor.Label}_Child", "MRUK", anchor.Label.ToString());
                        LogVerbose($"    ? Found child mesh on {anchor.Label}: {childMF.sharedMesh.vertexCount} vertices");
                    }
                }

                // Try to extract using reflection
                if (useReflection)
                {
                    ExtractUsingReflection(anchor, anchor.GetType(), "MRUKAnchor");
                }
            }
            catch (System.Exception e)
            {
                LogVerbose($"  ? Error processing anchor {anchor.Label}: {e.Message}");
            }
        }
    }

    private void ExtractFromMeshFilters()
    {
        LogVerbose("?? Extracting from all MeshFilters in scene...");
        
        var allMeshFilters = FindObjectsByType<MeshFilter>(FindObjectsSortMode.None);
        LogVerbose($"Found {allMeshFilters.Length} MeshFilters");

        foreach (var mf in allMeshFilters)
        {
            if (mf.sharedMesh != null && mf.sharedMesh.vertexCount > 0)
            {
                CreateSpatialMeshData(mf.sharedMesh, mf.transform, $"MeshFilter_{mf.name}", "MeshFilter");
                LogVerbose($"  ? {mf.name}: {mf.sharedMesh.vertexCount} vertices");
            }
        }
    }

    private void SearchForHiddenMeshes()
    {
        LogVerbose("?? Searching for hidden mesh components...");
        
        var allGameObjects = FindObjectsByType<GameObject>(FindObjectsSortMode.None);
        
        foreach (var go in allGameObjects)
        {
            // Skip if already processed
            if (go.GetComponent<MeshFilter>() != null) continue;
            
            // Look for any component that might contain mesh data
            var components = go.GetComponents<MonoBehaviour>();
            foreach (var comp in components)
            {
                if (comp == null) continue;
                
                var typeName = comp.GetType().Name.ToLower();
                if (typeName.Contains("mesh") || typeName.Contains("triangle") || 
                    typeName.Contains("geometry") || typeName.Contains("surface"))
                {
                    LogVerbose($"  Found potential mesh component: {comp.GetType().Name} on {go.name}");
                    ExtractMeshFromOVRComponent(comp, "HiddenMesh");
                }
            }
        }
    }

    private void GenerateSyntheticRoomMesh()
    {
        LogVerbose("?? Generating synthetic room mesh from spatial data...");
        
        if (currentRoom == null)
        {
            LogVerbose("? No room data available for synthetic mesh generation");
            return;
        }

        try
        {
            // Create a simple room mesh based on anchor positions
            var walls = currentRoom.Anchors.Where(a => a.Label.ToString().Contains("WALL")).ToList();
            var floor = currentRoom.Anchors.Where(a => a.Label.ToString().Contains("FLOOR")).FirstOrDefault();
            var ceiling = currentRoom.Anchors.Where(a => a.Label.ToString().Contains("CEILING")).FirstOrDefault();

            if (walls.Count > 0 || floor != null)
            {
                var syntheticMesh = CreateSyntheticRoomMesh(walls, floor, ceiling);
                if (syntheticMesh != null)
                {
                    CreateSpatialMeshData(syntheticMesh, transform, "Synthetic_Room", "Generated");
                    LogVerbose($"? Generated synthetic room mesh: {syntheticMesh.vertexCount} vertices");
                }
            }
        }
        catch (System.Exception e)
        {
            LogVerbose($"? Error generating synthetic mesh: {e.Message}");
        }
    }

    private Mesh CreateSyntheticRoomMesh(List<MRUKAnchor> walls, MRUKAnchor floor, MRUKAnchor ceiling)
    {
        var mesh = new Mesh();
        var vertices = new List<Vector3>();
        var triangles = new List<int>();
        
        // Add floor
        if (floor != null)
        {
            AddFloorMesh(vertices, triangles, floor);
        }
        
        // Add walls
        foreach (var wall in walls)
        {
            AddWallMesh(vertices, triangles, wall);
        }
        
        // Add ceiling
        if (ceiling != null)
        {
            AddCeilingMesh(vertices, triangles, ceiling);
        }
        
        if (vertices.Count > 0)
        {
            mesh.vertices = vertices.ToArray();
            mesh.triangles = triangles.ToArray();
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
        }
        
        return mesh.vertexCount > 0 ? mesh : null;
    }

    private void AddFloorMesh(List<Vector3> vertices, List<int> triangles, MRUKAnchor floor)
    {
        // Create a simple quad for the floor
        var pos = floor.transform.position;
        var size = new Vector3(3f, 0f, 3f); // Default size
        
        int startVertex = vertices.Count;
        
        vertices.Add(pos + new Vector3(-size.x/2, 0, -size.z/2));
        vertices.Add(pos + new Vector3(size.x/2, 0, -size.z/2));
        vertices.Add(pos + new Vector3(size.x/2, 0, size.z/2));
        vertices.Add(pos + new Vector3(-size.x/2, 0, size.z/2));
        
        triangles.AddRange(new int[] { 
            startVertex, startVertex + 1, startVertex + 2,
            startVertex, startVertex + 2, startVertex + 3
        });
    }

    private void AddWallMesh(List<Vector3> vertices, List<int> triangles, MRUKAnchor wall)
    {
        // Create a simple quad for the wall
        var pos = wall.transform.position;
        var height = 2.5f;
        var width = 2f;
        
        int startVertex = vertices.Count;
        
        vertices.Add(pos + new Vector3(-width/2, 0, 0));
        vertices.Add(pos + new Vector3(width/2, 0, 0));
        vertices.Add(pos + new Vector3(width/2, height, 0));
        vertices.Add(pos + new Vector3(-width/2, height, 0));
        
        triangles.AddRange(new int[] { 
            startVertex, startVertex + 1, startVertex + 2,
            startVertex, startVertex + 2, startVertex + 3
        });
    }

    private void AddCeilingMesh(List<Vector3> vertices, List<int> triangles, MRUKAnchor ceiling)
    {
        // Similar to floor but higher up
        var pos = ceiling.transform.position;
        var size = new Vector3(3f, 0f, 3f);
        
        int startVertex = vertices.Count;
        
        vertices.Add(pos + new Vector3(-size.x/2, 0, -size.z/2));
        vertices.Add(pos + new Vector3(-size.x/2, 0, size.z/2));
        vertices.Add(pos + new Vector3(size.x/2, 0, size.z/2));
        vertices.Add(pos + new Vector3(size.x/2, 0, -size.z/2));
        
        triangles.AddRange(new int[] { 
            startVertex, startVertex + 1, startVertex + 2,
            startVertex, startVertex + 2, startVertex + 3
        });
    }

    private SpatialMeshData CreateSpatialMeshData(Mesh mesh, Transform sourceTransform, string meshName, string sourceType, string anchorLabel = "")
    {
        if (mesh == null || mesh.vertexCount == 0) return null;

        var meshData = new SpatialMeshData
        {
            meshName = meshName,
            sourceType = sourceType,
            vertices = mesh.vertices,
            triangles = mesh.triangles,
            normals = mesh.normals.Length > 0 ? mesh.normals : null,
            uvs = mesh.uv.Length > 0 ? mesh.uv : null,
            colors = mesh.colors.Length > 0 ? mesh.colors : null,
            sourceTransform = sourceTransform,
            worldBounds = mesh.bounds,
            anchorLabel = anchorLabel
        };

        // Transform to world space if we have a transform
        if (sourceTransform != null)
        {
            for (int i = 0; i < meshData.vertices.Length; i++)
            {
                meshData.vertices[i] = sourceTransform.TransformPoint(meshData.vertices[i]);
            }
            
            if (meshData.normals != null)
            {
                for (int i = 0; i < meshData.normals.Length; i++)
                {
                    meshData.normals[i] = sourceTransform.TransformDirection(meshData.normals[i]).normalized;
                }
            }
            
            meshData.worldBounds = TransformBounds(mesh.bounds, sourceTransform);
        }

        // Generate normals if missing
        if (meshData.normals == null)
        {
            meshData.normals = CalculateNormals(meshData.vertices, meshData.triangles);
        }

        extractedMeshes.Add(meshData);
        return meshData;
    }

    private Bounds TransformBounds(Bounds localBounds, Transform transform)
    {
        var center = transform.TransformPoint(localBounds.center);
        var size = localBounds.size;
        size.Scale(transform.lossyScale);
        return new Bounds(center, size);
    }

    // Helper conversion methods
    private Vector3[] ConvertToVector3Array(object data)
    {
        if (data == null) return null;
        if (data is Vector3[] array) return array;
        
        if (data is System.Collections.IEnumerable enumerable)
        {
            var list = new List<Vector3>();
            foreach (var item in enumerable)
            {
                if (item is Vector3 v3) list.Add(v3);
            }
            return list.Count > 0 ? list.ToArray() : null;
        }
        
        return null;
    }

    private int[] ConvertToIntArray(object data)
    {
        if (data == null) return null;
        if (data is int[] array) return array;
        if (data is uint[] uintArray) return uintArray.Select(u => (int)u).ToArray();
        
        if (data is System.Collections.IEnumerable enumerable)
        {
            var list = new List<int>();
            foreach (var item in enumerable)
            {
                if (item is int i) list.Add(i);
                else if (item is uint ui) list.Add((int)ui);
            }
            return list.Count > 0 ? list.ToArray() : null;
        }
        
        return null;
    }

    private Vector2[] ConvertToVector2Array(object data)
    {
        if (data == null) return null;
        if (data is Vector2[] array) return array;
        
        if (data is System.Collections.IEnumerable enumerable)
        {
            var list = new List<Vector2>();
            foreach (var item in enumerable)
            {
                if (item is Vector2 v2) list.Add(v2);
            }
            return list.Count > 0 ? list.ToArray() : null;
        }
        
        return null;
    }

    private Color[] ConvertToColorArray(object data)
    {
        if (data == null) return null;
        if (data is Color[] array) return array;
        
        if (data is System.Collections.IEnumerable enumerable)
        {
            var list = new List<Color>();
            foreach (var item in enumerable)
            {
                if (item is Color c) list.Add(c);
            }
            return list.Count > 0 ? list.ToArray() : null;
        }
        
        return null;
    }

    private Vector3[] CalculateNormals(Vector3[] vertices, int[] triangles)
    {
        var normals = new Vector3[vertices.Length];
        
        for (int i = 0; i < triangles.Length; i += 3)
        {
            int i1 = triangles[i];
            int i2 = triangles[i + 1];
            int i3 = triangles[i + 2];
            
            if (i1 < vertices.Length && i2 < vertices.Length && i3 < vertices.Length)
            {
                Vector3 v1 = vertices[i1];
                Vector3 v2 = vertices[i2];
                Vector3 v3 = vertices[i3];
                
                Vector3 normal = Vector3.Cross(v2 - v1, v3 - v1).normalized;
                
                normals[i1] += normal;
                normals[i2] += normal;
                normals[i3] += normal;
            }
        }
        
        for (int i = 0; i < normals.Length; i++)
        {
            normals[i] = normals[i].normalized;
        }
        
        return normals;
    }

    private Bounds CalculateBounds(Vector3[] vertices)
    {
        if (vertices == null || vertices.Length == 0)
            return new Bounds();
        
        var min = vertices[0];
        var max = vertices[0];
        
        foreach (var vertex in vertices)
        {
            min = Vector3.Min(min, vertex);
            max = Vector3.Max(max, vertex);
        }
        
        return new Bounds((min + max) / 2, max - min);
    }

    private void ExportMeshes()
    {
        LogVerbose("?? Exporting extracted meshes...");
        
        if (exportFormat == ExportFormat.OBJ || exportFormat == ExportFormat.Both)
        {
            ExportAsOBJ();
        }
        
        if (exportFormat == ExportFormat.PLY || exportFormat == ExportFormat.Both)
        {
            ExportAsPLY();
        }
    }

    private void ExportAsOBJ()
    {
        string path = Path.Combine(Application.persistentDataPath, $"{baseFileName}.obj");
        
        var obj = new StringBuilder();
        obj.AppendLine($"# Quest 3 Spatial Mesh Export (Advanced Extraction)");
        obj.AppendLine($"# Generated: {System.DateTime.Now:yyyy-MM-dd HH:mm:ss}");
        obj.AppendLine($"# Extractor: QuestSpatialMeshExtractor");
        obj.AppendLine($"# Meshes: {extractedMeshes.Count}");
        obj.AppendLine($"# Total Vertices: {extractedMeshes.Sum(m => m.vertexCount)}");
        obj.AppendLine($"# Total Triangles: {extractedMeshes.Sum(m => m.triangleCount)}");
        obj.AppendLine();

        int vertexOffset = 0;
        int normalOffset = 0;
        int uvOffset = 0;

        foreach (var mesh in extractedMeshes)
        {
            obj.AppendLine($"# Mesh: {mesh.meshName} ({mesh.sourceType})");
            if (!string.IsNullOrEmpty(mesh.anchorLabel))
                obj.AppendLine($"# Anchor: {mesh.anchorLabel}");
            obj.AppendLine($"# Vertices: {mesh.vertexCount}, Triangles: {mesh.triangleCount}");
            obj.AppendLine($"o {mesh.meshName}");

            // Write vertices
            foreach (var vertex in mesh.vertices)
            {
                obj.AppendLine($"v {vertex.x:F6} {vertex.y:F6} {vertex.z:F6}");
            }

            // Write normals
            if (mesh.normals != null)
            {
                foreach (var normal in mesh.normals)
                {
                    obj.AppendLine($"vn {normal.x:F6} {normal.y:F6} {normal.z:F6}");
                }
            }

            // Write UVs
            if (exportUVs && mesh.uvs != null)
            {
                foreach (var uv in mesh.uvs)
                {
                    obj.AppendLine($"vt {uv.x:F6} {uv.y:F6}");
                }
            }

            // Write faces
            for (int i = 0; i < mesh.triangles.Length; i += 3)
            {
                int v1 = mesh.triangles[i] + vertexOffset + 1;
                int v2 = mesh.triangles[i + 1] + vertexOffset + 1;
                int v3 = mesh.triangles[i + 2] + vertexOffset + 1;

                if (mesh.normals != null && exportUVs && mesh.uvs != null)
                {
                    int n1 = mesh.triangles[i] + normalOffset + 1;
                    int n2 = mesh.triangles[i + 1] + normalOffset + 1;
                    int n3 = mesh.triangles[i + 2] + normalOffset + 1;
                    int uv1 = mesh.triangles[i] + uvOffset + 1;
                    int uv2 = mesh.triangles[i + 1] + uvOffset + 1;
                    int uv3 = mesh.triangles[i + 2] + uvOffset + 1;
                    obj.AppendLine($"f {v1}/{uv1}/{n1} {v2}/{uv2}/{n2} {v3}/{uv3}/{n3}");
                }
                else if (mesh.normals != null)
                {
                    int n1 = mesh.triangles[i] + normalOffset + 1;
                    int n2 = mesh.triangles[i + 1] + normalOffset + 1;
                    int n3 = mesh.triangles[i + 2] + normalOffset + 1;
                    obj.AppendLine($"f {v1}//{n1} {v2}//{n2} {v3}//{n3}");
                }
                else
                {
                    obj.AppendLine($"f {v1} {v2} {v3}");
                }
            }

            vertexOffset += mesh.vertexCount;
            if (mesh.normals != null) normalOffset += mesh.normals.Length;
            if (mesh.uvs != null) uvOffset += mesh.uvs.Length;
            obj.AppendLine();
        }

        File.WriteAllText(path, obj.ToString());
        LogVerbose($"? OBJ exported to: {path}");
    }

    private void ExportAsPLY()
    {
        string path = Path.Combine(Application.persistentDataPath, $"{baseFileName}.ply");
        
        int totalVertices = extractedMeshes.Sum(m => m.vertexCount);
        int totalFaces = extractedMeshes.Sum(m => m.triangleCount);
        bool hasColors = extractedMeshes.Any(m => m.colors != null);

        var ply = new StringBuilder();
        ply.AppendLine("ply");
        ply.AppendLine("format ascii 1.0");
        ply.AppendLine($"comment Quest 3 Spatial Mesh Export - {System.DateTime.Now:yyyy-MM-dd HH:mm:ss}");
        ply.AppendLine($"element vertex {totalVertices}");
        ply.AppendLine("property float x");
        ply.AppendLine("property float y");
        ply.AppendLine("property float z");
        ply.AppendLine("property float nx");
        ply.AppendLine("property float ny");
        ply.AppendLine("property float nz");
        
        if (hasColors)
        {
            ply.AppendLine("property uchar red");
            ply.AppendLine("property uchar green");
            ply.AppendLine("property uchar blue");
        }
        
        ply.AppendLine($"element face {totalFaces}");
        ply.AppendLine("property list uchar int vertex_indices");
        ply.AppendLine("end_header");

        // Write vertices
        foreach (var mesh in extractedMeshes)
        {
            for (int i = 0; i < mesh.vertices.Length; i++)
            {
                var vertex = mesh.vertices[i];
                var normal = mesh.normals != null && i < mesh.normals.Length ? mesh.normals[i] : Vector3.up;
                
                ply.Append($"{vertex.x:F6} {vertex.y:F6} {vertex.z:F6} {normal.x:F6} {normal.y:F6} {normal.z:F6}");
                
                if (hasColors && mesh.colors != null && i < mesh.colors.Length)
                {
                    var color = mesh.colors[i];
                    ply.Append($" {(int)(color.r * 255)} {(int)(color.g * 255)} {(int)(color.b * 255)}");
                }
                else if (hasColors)
                {
                    ply.Append(" 128 128 128");
                }
                
                ply.AppendLine();
            }
        }

        // Write faces
        int vertexOffset = 0;
        foreach (var mesh in extractedMeshes)
        {
            for (int i = 0; i < mesh.triangles.Length; i += 3)
            {
                int v1 = mesh.triangles[i] + vertexOffset;
                int v2 = mesh.triangles[i + 1] + vertexOffset;
                int v3 = mesh.triangles[i + 2] + vertexOffset;
                ply.AppendLine($"3 {v1} {v2} {v3}");
            }
            vertexOffset += mesh.vertexCount;
        }

        File.WriteAllText(path, ply.ToString());
        LogVerbose($"? PLY exported to: {path}");
    }

    private void LogVerbose(string message)
    {
        if (verboseLogging)
        {
            Debug.Log(message);
        }
    }

    private void LogError(string message)
    {
        Debug.LogError(message);
    }

    private void LogSuggestions()
    {
        LogError("?? Troubleshooting suggestions:");
        LogError("   ?? Ensure OVRSceneManager is in your scene");
        LogError("   ?? Enable Scene Understanding in Quest settings");
        LogError("   ?? Make sure room has been scanned recently");
        LogError("   ?? Check if mesh visualization is enabled");
        LogError("   ?? Try increasing the extraction delay");
        LogError("   ?? Verify Meta XR SDK is up to date");
        LogError("   ?? Check device permissions for spatial data");
    }

    // Public methods
    [ContextMenu("Manual Extract")]
    public void ManualExtract()
    {
        ExtractSpatialMeshes();
    }

    [ContextMenu("List All Scene Components")]
    public void ListAllSceneComponents()
    {
        LogVerbose("=== ALL SCENE COMPONENTS ===");
        var allComponents = FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None);
        
        foreach (var comp in allComponents)
        {
            var typeName = comp.GetType().Name;
            if (typeName.Contains("OVR") || typeName.Contains("Meta") || typeName.Contains("Scene") || 
                typeName.Contains("Mesh") || typeName.Contains("MRUK"))
            {
                LogVerbose($"?? {typeName} on {comp.name}");
            }
        }
        LogVerbose("=== END COMPONENTS ===");
    }
}