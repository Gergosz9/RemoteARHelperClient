using UnityEngine;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Linq;
using System.Reflection;

/// <summary>
/// Advanced scene mesh extractor that specifically targets OVR Scene API components
/// to extract the actual triangle mesh data from Meta Quest scene understanding.
/// 
/// This script uses reflection to access OVR internal APIs since they may not be
/// directly exposed in the public API.
/// </summary>
public class OVRSceneMeshExtractor : MonoBehaviour
{
    [Header("OVR Scene Mesh Extraction")]
    [Tooltip("Export filename")]
    public string exportFileName = "ovr_scene_mesh";
    
    [Tooltip("Enable detailed debug logging")]
    public bool enableDebugLogs = true;
    
    [Tooltip("Wait time before attempting extraction")]
    public float extractionDelay = 3f;
    
    [Tooltip("Export separate files for each scene object")]
    public bool exportSeparateFiles = true;

    [Header("Scene Object Filters")]
    [Tooltip("Include wall meshes")]
    public bool includeWalls = true;
    
    [Tooltip("Include floor meshes")]
    public bool includeFloor = true;
    
    [Tooltip("Include ceiling meshes")]
    public bool includeCeiling = true;
    
    [Tooltip("Include furniture meshes")]
    public bool includeFurniture = true;

    private List<ExtractedMeshData> extractedMeshes = new List<ExtractedMeshData>();

    [System.Serializable]
    public class ExtractedMeshData
    {
        public string objectName;
        public string objectType;
        public Vector3[] vertices;
        public int[] triangles;
        public Vector3[] normals;
        public Transform transform;
        public Bounds bounds;
    }

    void Start()
    {
        StartCoroutine(DelayedExtraction());
    }

    private System.Collections.IEnumerator DelayedExtraction()
    {
        if (enableDebugLogs) Debug.Log("?? OVR Scene Mesh Extractor - Waiting before extraction...");
        
        yield return new WaitForSeconds(extractionDelay);
        
        if (enableDebugLogs) Debug.Log("?? Starting OVR scene mesh extraction...");
        
        ExtractOVRSceneMeshes();
    }

    [ContextMenu("Extract OVR Scene Meshes")]
    public void ExtractOVRSceneMeshes()
    {
        extractedMeshes.Clear();
        
        if (enableDebugLogs) Debug.Log("=== OVR SCENE MESH EXTRACTION ===");

        // Method 1: Find OVRSceneManager
        ExtractFromOVRSceneManager();
        
        // Method 2: Find all OVRSceneAnchor components
        ExtractFromOVRSceneAnchors();
        
        // Method 3: Find TriangleMesh components
        ExtractFromTriangleMeshComponents();
        
        // Method 4: Search all MonoBehaviours for mesh data
        ExtractFromAllComponents();

        if (enableDebugLogs)
        {
            Debug.Log($"? Extraction complete! Found {extractedMeshes.Count} meshes");
            foreach (var mesh in extractedMeshes)
            {
                Debug.Log($"  ?? {mesh.objectName} ({mesh.objectType}): {mesh.vertices?.Length ?? 0} vertices");
            }
        }

        if (extractedMeshes.Count > 0)
        {
            ExportExtractedMeshes();
        }
        else
        {
            Debug.LogWarning("? No OVR scene meshes found!");
            Debug.LogWarning("?? Make sure:");
            Debug.LogWarning("   • OVRSceneManager is in your scene");
            Debug.LogWarning("   • Scene understanding is enabled");
            Debug.LogWarning("   • Room has been scanned on the device");
            Debug.LogWarning("   • OVR SDK is properly imported");
        }
    }

    private void ExtractFromOVRSceneManager()
    {
        if (enableDebugLogs) Debug.Log("?? Looking for OVRSceneManager...");
        
        var sceneManager = FindObjectByType<MonoBehaviour>(m => m.GetType().Name == "OVRSceneManager");
        if (sceneManager != null)
        {
            if (enableDebugLogs) Debug.Log($"? Found OVRSceneManager: {sceneManager.name}");
            ExtractMeshFromComponent(sceneManager, "OVRSceneManager");
        }
        else
        {
            if (enableDebugLogs) Debug.Log("? No OVRSceneManager found");
        }
    }

    private void ExtractFromOVRSceneAnchors()
    {
        if (enableDebugLogs) Debug.Log("?? Looking for OVRSceneAnchor components...");
        
        var sceneAnchors = FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None)
            .Where(c => c.GetType().Name.Contains("OVRSceneAnchor") || c.GetType().Name.Contains("OVRAnchor"))
            .ToArray();

        if (enableDebugLogs) Debug.Log($"Found {sceneAnchors.Length} OVR anchor components");

        foreach (var anchor in sceneAnchors)
        {
            ExtractMeshFromComponent(anchor, "OVRSceneAnchor");
        }
    }

    private void ExtractFromTriangleMeshComponents()
    {
        if (enableDebugLogs) Debug.Log("?? Looking for TriangleMesh components...");
        
        var triangleMeshes = FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None)
            .Where(c => c.GetType().Name.Contains("TriangleMesh") || c.GetType().Name.Contains("Mesh"))
            .ToArray();

        if (enableDebugLogs) Debug.Log($"Found {triangleMeshes.Length} potential triangle mesh components");

        foreach (var meshComp in triangleMeshes)
        {
            ExtractMeshFromComponent(meshComp, "TriangleMesh");
        }
    }

    private void ExtractFromAllComponents()
    {
        if (enableDebugLogs) Debug.Log("?? Scanning all components for mesh data...");
        
        var allComponents = FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None)
            .Where(c => c.GetType().Name.Contains("OVR") || 
                       c.GetType().Name.Contains("Scene") ||
                       c.GetType().Name.Contains("Meta"))
            .ToArray();

        if (enableDebugLogs) Debug.Log($"Found {allComponents.Length} OVR/Meta/Scene components");

        foreach (var component in allComponents)
        {
            ExtractMeshFromComponent(component, component.GetType().Name);
        }
    }

    private void ExtractMeshFromComponent(MonoBehaviour component, string componentType)
    {
        if (component == null) return;

        try
        {
            var type = component.GetType();
            if (enableDebugLogs) Debug.Log($"?? Analyzing {componentType}: {type.Name}");

            // Method 1: Look for direct mesh properties
            ExtractFromMeshProperties(component, type, componentType);
            
            // Method 2: Look for mesh data methods
            ExtractFromMeshMethods(component, type, componentType);
            
            // Method 3: Look for triangle/vertex data
            ExtractFromGeometryData(component, type, componentType);
            
            // Method 4: Check child components
            ExtractFromChildComponents(component, componentType);

        }
        catch (System.Exception e)
        {
            if (enableDebugLogs) Debug.Log($"? Error extracting from {componentType}: {e.Message}");
        }
    }

    private void ExtractFromMeshProperties(MonoBehaviour component, System.Type type, string componentType)
    {
        var meshProperties = type.GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
            .Where(p => p.Name.ToLower().Contains("mesh") || 
                       p.Name.ToLower().Contains("triangle") ||
                       p.Name.ToLower().Contains("geometry"))
            .ToArray();

        foreach (var prop in meshProperties)
        {
            try
            {
                var value = prop.GetValue(component);
                if (value != null)
                {
                    if (enableDebugLogs) Debug.Log($"  ?? Found property {prop.Name}: {value.GetType().Name}");
                    
                    var meshData = ExtractMeshDataFromObject(value, $"{componentType}_{prop.Name}");
                    if (meshData != null)
                    {
                        meshData.transform = component.transform;
                        extractedMeshes.Add(meshData);
                        
                        if (enableDebugLogs) Debug.Log($"  ? Extracted mesh from {prop.Name}: {meshData.vertices.Length} vertices");
                    }
                }
            }
            catch (System.Exception e)
            {
                if (enableDebugLogs) Debug.Log($"  ? Could not access property {prop.Name}: {e.Message}");
            }
        }
    }

    private void ExtractFromMeshMethods(MonoBehaviour component, System.Type type, string componentType)
    {
        var meshMethods = type.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
            .Where(m => (m.Name.ToLower().Contains("getmesh") || 
                        m.Name.ToLower().Contains("mesh") ||
                        m.Name.ToLower().Contains("triangle")) &&
                       m.GetParameters().Length == 0 &&
                       m.ReturnType != typeof(void))
            .ToArray();

        foreach (var method in meshMethods)
        {
            try
            {
                var result = method.Invoke(component, null);
                if (result != null)
                {
                    if (enableDebugLogs) Debug.Log($"  ?? Method {method.Name} returned: {result.GetType().Name}");
                    
                    var meshData = ExtractMeshDataFromObject(result, $"{componentType}_{method.Name}");
                    if (meshData != null)
                    {
                        meshData.transform = component.transform;
                        extractedMeshes.Add(meshData);
                        
                        if (enableDebugLogs) Debug.Log($"  ? Extracted mesh from method {method.Name}: {meshData.vertices.Length} vertices");
                    }
                }
            }
            catch (System.Exception e)
            {
                if (enableDebugLogs) Debug.Log($"  ? Could not invoke method {method.Name}: {e.Message}");
            }
        }
    }

    private void ExtractFromGeometryData(MonoBehaviour component, System.Type type, string componentType)
    {
        try
        {
            // Look for vertex arrays
            var vertexProperties = type.GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                .Where(p => p.Name.ToLower().Contains("vertex") || 
                           p.Name.ToLower().Contains("point"))
                .ToArray();

            // Look for triangle/index arrays
            var triangleProperties = type.GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                .Where(p => p.Name.ToLower().Contains("triangle") || 
                           p.Name.ToLower().Contains("index") ||
                           p.Name.ToLower().Contains("indices"))
                .ToArray();

            Vector3[] vertices = null;
            int[] triangles = null;

            // Extract vertices
            foreach (var vertexProp in vertexProperties)
            {
                try
                {
                    var vertexData = vertexProp.GetValue(component);
                    vertices = ConvertToVector3Array(vertexData);
                    if (vertices != null && vertices.Length > 0)
                    {
                        if (enableDebugLogs) Debug.Log($"  ? Found vertices in {vertexProp.Name}: {vertices.Length}");
                        break;
                    }
                }
                catch { /* continue */ }
            }

            // Extract triangles
            foreach (var triangleProp in triangleProperties)
            {
                try
                {
                    var triangleData = triangleProp.GetValue(component);
                    triangles = ConvertToIntArray(triangleData);
                    if (triangles != null && triangles.Length > 0)
                    {
                        if (enableDebugLogs) Debug.Log($"  ? Found triangles in {triangleProp.Name}: {triangles.Length}");
                        break;
                    }
                }
                catch { /* continue */ }
            }

            // Create mesh if we have both vertices and triangles
            if (vertices != null && vertices.Length > 0 && triangles != null && triangles.Length > 0)
            {
                var meshData = new ExtractedMeshData
                {
                    objectName = $"{componentType}_Geometry",
                    objectType = componentType,
                    vertices = vertices,
                    triangles = triangles,
                    transform = component.transform,
                    bounds = CalculateBounds(vertices)
                };

                // Generate normals
                meshData.normals = CalculateNormals(vertices, triangles);

                extractedMeshes.Add(meshData);
                
                if (enableDebugLogs) Debug.Log($"  ? Created mesh from geometry data: {vertices.Length} vertices, {triangles.Length/3} faces");
            }
        }
        catch (System.Exception e)
        {
            if (enableDebugLogs) Debug.Log($"  ? Error extracting geometry data: {e.Message}");
        }
    }

    private void ExtractFromChildComponents(MonoBehaviour component, string componentType)
    {
        var childComponents = component.GetComponentsInChildren<MonoBehaviour>()
            .Where(c => c != component && 
                       (c.GetType().Name.Contains("Mesh") || 
                        c.GetType().Name.Contains("Triangle") ||
                        c.GetType().Name.Contains("Geometry")))
            .ToArray();

        foreach (var child in childComponents)
        {
            ExtractMeshFromComponent(child, $"{componentType}_Child");
        }
    }

    private ExtractedMeshData ExtractMeshDataFromObject(object meshObject, string objectName)
    {
        if (meshObject == null) return null;

        try
        {
            var type = meshObject.GetType();
            
            // If it's a Unity Mesh, convert it directly
            if (meshObject is Mesh unityMesh && unityMesh.vertexCount > 0)
            {
                return new ExtractedMeshData
                {
                    objectName = objectName,
                    objectType = "UnityMesh",
                    vertices = unityMesh.vertices,
                    triangles = unityMesh.triangles,
                    normals = unityMesh.normals.Length > 0 ? unityMesh.normals : null,
                    bounds = unityMesh.bounds
                };
            }

            // Try to extract vertex and triangle data from custom objects
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

            // Create mesh data if we have valid data
            if (vertices != null && vertices.Length > 0 && triangles != null && triangles.Length > 0)
            {
                return new ExtractedMeshData
                {
                    objectName = objectName,
                    objectType = type.Name,
                    vertices = vertices,
                    triangles = triangles,
                    normals = CalculateNormals(vertices, triangles),
                    bounds = CalculateBounds(vertices)
                };
            }
        }
        catch (System.Exception e)
        {
            if (enableDebugLogs) Debug.Log($"? Error extracting mesh data from object: {e.Message}");
        }

        return null;
    }

    private Vector3[] ConvertToVector3Array(object vertexData)
    {
        if (vertexData == null) return null;

        try
        {
            if (vertexData is Vector3[] vector3Array)
                return vector3Array;

            if (vertexData is System.Collections.IEnumerable enumerable)
            {
                var list = new List<Vector3>();
                foreach (var item in enumerable)
                {
                    if (item is Vector3 v3)
                        list.Add(v3);
                    else if (item != null)
                    {
                        // Try to convert from other vector types (OVR might use different vector types)
                        var converted = ConvertToVector3(item);
                        if (converted.HasValue)
                            list.Add(converted.Value);
                    }
                }
                return list.Count > 0 ? list.ToArray() : null;
            }
        }
        catch (System.Exception e)
        {
            if (enableDebugLogs) Debug.Log($"? Error converting vertex data: {e.Message}");
        }

        return null;
    }

    private Vector3? ConvertToVector3(object obj)
    {
        if (obj == null) return null;

        try
        {
            var type = obj.GetType();
            
            // Try to get x, y, z properties or fields
            var xProp = type.GetProperty("x") ?? type.GetProperty("X");
            var yProp = type.GetProperty("y") ?? type.GetProperty("Y");
            var zProp = type.GetProperty("z") ?? type.GetProperty("Z");

            if (xProp != null && yProp != null && zProp != null)
            {
                var x = System.Convert.ToSingle(xProp.GetValue(obj));
                var y = System.Convert.ToSingle(yProp.GetValue(obj));
                var z = System.Convert.ToSingle(zProp.GetValue(obj));
                return new Vector3(x, y, z);
            }

            // Try fields instead of properties
            var xField = type.GetField("x") ?? type.GetField("X");
            var yField = type.GetField("y") ?? type.GetField("Y");
            var zField = type.GetField("z") ?? type.GetField("Z");

            if (xField != null && yField != null && zField != null)
            {
                var x = System.Convert.ToSingle(xField.GetValue(obj));
                var y = System.Convert.ToSingle(yField.GetValue(obj));
                var z = System.Convert.ToSingle(zField.GetValue(obj));
                return new Vector3(x, y, z);
            }
        }
        catch { /* ignore conversion errors */ }

        return null;
    }

    private int[] ConvertToIntArray(object triangleData)
    {
        if (triangleData == null) return null;

        try
        {
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
                    else if (item != null)
                    {
                        try
                        {
                            var converted = System.Convert.ToInt32(item);
                            list.Add(converted);
                        }
                        catch { /* ignore conversion errors */ }
                    }
                }
                return list.Count > 0 ? list.ToArray() : null;
            }
        }
        catch (System.Exception e)
        {
            if (enableDebugLogs) Debug.Log($"? Error converting triangle data: {e.Message}");
        }

        return null;
    }

    private Vector3[] CalculateNormals(Vector3[] vertices, int[] triangles)
    {
        if (vertices == null || triangles == null) return null;

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

        var center = (min + max) / 2;
        var size = max - min;

        return new Bounds(center, size);
    }

    private void ExportExtractedMeshes()
    {
        if (extractedMeshes.Count == 0) return;

        if (exportSeparateFiles)
        {
            ExportSeparateFiles();
        }
        else
        {
            ExportCombinedFile();
        }
    }

    private void ExportSeparateFiles()
    {
        foreach (var mesh in extractedMeshes)
        {
            if (ShouldIncludeMesh(mesh))
            {
                string filename = $"{exportFileName}_{mesh.objectName}.obj";
                string path = Path.Combine(Application.persistentDataPath, filename);
                
                ExportSingleMeshToOBJ(mesh, path);
                
                if (enableDebugLogs)
                {
                    Debug.Log($"?? Exported {mesh.objectName} to {path}");
                }
            }
        }
    }

    private void ExportCombinedFile()
    {
        string path = Path.Combine(Application.persistentDataPath, $"{exportFileName}_combined.obj");
        ExportCombinedMeshesToOBJ(path);
        
        if (enableDebugLogs)
        {
            Debug.Log($"?? Exported combined mesh to {path}");
        }
    }

    private bool ShouldIncludeMesh(ExtractedMeshData mesh)
    {
        var name = mesh.objectName.ToLower();
        var type = mesh.objectType.ToLower();
        
        if ((name.Contains("wall") || type.Contains("wall")) && !includeWalls) return false;
        if ((name.Contains("floor") || type.Contains("floor")) && !includeFloor) return false;
        if ((name.Contains("ceiling") || type.Contains("ceiling")) && !includeCeiling) return false;
        if ((name.Contains("furniture") || type.Contains("furniture") || 
             name.Contains("table") || name.Contains("chair") || name.Contains("couch")) && !includeFurniture) return false;
        
        return true;
    }

    private void ExportSingleMeshToOBJ(ExtractedMeshData mesh, string path)
    {
        StringBuilder obj = new StringBuilder();

        obj.AppendLine($"# OVR Scene Mesh Export - {mesh.objectName}");
        obj.AppendLine($"# Generated: {System.DateTime.Now:yyyy-MM-dd HH:mm:ss}");
        obj.AppendLine($"# Object Type: {mesh.objectType}");
        obj.AppendLine($"# Vertices: {mesh.vertices.Length}");
        obj.AppendLine($"# Faces: {mesh.triangles.Length / 3}");
        obj.AppendLine();

        obj.AppendLine($"o {mesh.objectName}");

        // Write vertices (transform to world space if we have transform)
        foreach (var vertex in mesh.vertices)
        {
            Vector3 worldVertex = mesh.transform != null ? mesh.transform.TransformPoint(vertex) : vertex;
            obj.AppendLine($"v {worldVertex.x:F6} {worldVertex.y:F6} {worldVertex.z:F6}");
        }

        // Write normals if available
        if (mesh.normals != null)
        {
            foreach (var normal in mesh.normals)
            {
                Vector3 worldNormal = mesh.transform != null ? mesh.transform.TransformDirection(normal) : normal;
                obj.AppendLine($"vn {worldNormal.x:F6} {worldNormal.y:F6} {worldNormal.z:F6}");
            }
        }

        // Write faces
        for (int i = 0; i < mesh.triangles.Length; i += 3)
        {
            int v1 = mesh.triangles[i] + 1;
            int v2 = mesh.triangles[i + 1] + 1;
            int v3 = mesh.triangles[i + 2] + 1;

            if (mesh.normals != null)
            {
                obj.AppendLine($"f {v1}//{v1} {v2}//{v2} {v3}//{v3}");
            }
            else
            {
                obj.AppendLine($"f {v1} {v2} {v3}");
            }
        }

        File.WriteAllText(path, obj.ToString());
    }

    private void ExportCombinedMeshesToOBJ(string path)
    {
        StringBuilder obj = new StringBuilder();

        obj.AppendLine($"# OVR Scene Mesh Export - Combined");
        obj.AppendLine($"# Generated: {System.DateTime.Now:yyyy-MM-dd HH:mm:ss}");
        obj.AppendLine($"# Objects: {extractedMeshes.Count}");
        obj.AppendLine($"# Total Vertices: {extractedMeshes.Sum(m => m.vertices?.Length ?? 0)}");
        obj.AppendLine($"# Total Faces: {extractedMeshes.Sum(m => (m.triangles?.Length ?? 0) / 3)}");
        obj.AppendLine();

        int vertexOffset = 0;

        foreach (var mesh in extractedMeshes)
        {
            if (!ShouldIncludeMesh(mesh)) continue;

            obj.AppendLine($"# Object: {mesh.objectName} ({mesh.objectType})");
            obj.AppendLine($"o {mesh.objectName}");

            // Write vertices
            foreach (var vertex in mesh.vertices)
            {
                Vector3 worldVertex = mesh.transform != null ? mesh.transform.TransformPoint(vertex) : vertex;
                obj.AppendLine($"v {worldVertex.x:F6} {worldVertex.y:F6} {worldVertex.z:F6}");
            }

            // Write normals
            if (mesh.normals != null)
            {
                foreach (var normal in mesh.normals)
                {
                    Vector3 worldNormal = mesh.transform != null ? mesh.transform.TransformDirection(normal) : normal;
                    obj.AppendLine($"vn {worldNormal.x:F6} {worldNormal.y:F6} {worldNormal.z:F6}");
                }
            }

            // Write faces
            for (int i = 0; i < mesh.triangles.Length; i += 3)
            {
                int v1 = mesh.triangles[i] + vertexOffset + 1;
                int v2 = mesh.triangles[i + 1] + vertexOffset + 1;
                int v3 = mesh.triangles[i + 2] + vertexOffset + 1;

                if (mesh.normals != null)
                {
                    obj.AppendLine($"f {v1}//{v1} {v2}//{v2} {v3}//{v3}");
                }
                else
                {
                    obj.AppendLine($"f {v1} {v2} {v3}");
                }
            }

            vertexOffset += mesh.vertices.Length;
            obj.AppendLine();
        }

        File.WriteAllText(path, obj.ToString());
    }

    // Helper method to find components by name using reflection
    private T FindObjectByType<T>(System.Func<T, bool> predicate) where T : Object
    {
        var objects = FindObjectsByType<T>(FindObjectsSortMode.None);
        return objects.FirstOrDefault(predicate);
    }

    [ContextMenu("Manual Extract")]
    public void ManualExtract()
    {
        ExtractOVRSceneMeshes();
    }

    [ContextMenu("List All OVR Components")]
    public void ListAllOVRComponents()
    {
        Debug.Log("=== ALL OVR COMPONENTS IN SCENE ===");
        
        var allComponents = FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None)
            .Where(c => c.GetType().Name.Contains("OVR") || 
                       c.GetType().Name.Contains("Meta") || 
                       c.GetType().Name.Contains("Scene"))
            .ToArray();

        foreach (var comp in allComponents)
        {
            Debug.Log($"?? {comp.GetType().Name} on {comp.name}");
            
            // List all properties
            var properties = comp.GetType().GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            foreach (var prop in properties)
            {
                if (prop.Name.ToLower().Contains("mesh") || 
                    prop.Name.ToLower().Contains("triangle") ||
                    prop.Name.ToLower().Contains("vertex") ||
                    prop.Name.ToLower().Contains("geometry"))
                {
                    try
                    {
                        var value = prop.GetValue(comp);
                        Debug.Log($"  ?? {prop.Name}: {value?.GetType().Name} = {value}");
                    }
                    catch (System.Exception e)
                    {
                        Debug.Log($"  ? {prop.Name}: {e.Message}");
                    }
                }
            }
        }
        
        Debug.Log("========================");
    }
}