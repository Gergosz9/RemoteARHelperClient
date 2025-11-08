using UnityEngine;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Meta.XR.MRUtilityKit;
using System.Collections;

public class SceneMeshExporter : MonoBehaviour
{
    [Header("Export Settings")]
    public string exportFileName = "quest_room_mesh";
    public ExportFormat exportFormat = ExportFormat.OBJ;
    public bool exportOnStart = false;
    public KeyCode exportKey = KeyCode.E;

    [Header("Mesh Filtering")]
    public bool includeWalls = true;
    public bool includeFloor = true;
    public bool includeCeiling = true;
    public bool includeFurniture = false;
    public bool includeInvisibleWalls = false;

    [Header("Debug")]
    public bool showDebugLogs = true;

    private MRUKRoom currentRoom;
    private List<MeshData> collectedMeshes = new List<MeshData>();

    public enum ExportFormat
    {
        OBJ,
        PLY
    }

    [System.Serializable]
    public class MeshData
    {
        public Vector3[] vertices;
        public int[] triangles;
        public Vector3[] normals;
        public Vector2[] uvs;
        public string objectName;
        public MRUKAnchor.SceneLabels label;
    }

    void Start()
    {
        StartCoroutine(InitializeSceneUnderstanding());

        if (exportOnStart)
        {
            StartCoroutine(DelayedExport());
        }
    }

    void Update()
    {
        if (Input.GetKeyDown(exportKey))
        {
            ExportSceneMesh();
        }
    }

    private IEnumerator InitializeSceneUnderstanding()
    {
        // Wait for MRUK singleton to be available
        while (MRUK.Instance == null)
        {
            if (showDebugLogs) Debug.Log("Waiting for MRUK Instance...");
            yield return new WaitForSeconds(0.5f);
        }

        if (showDebugLogs) Debug.Log("MRUK Instance found, waiting for room...");

        // Wait for room data to be loaded
        while (MRUK.Instance.GetCurrentRoom() == null)
        {
            yield return new WaitForSeconds(0.5f);
        }

        currentRoom = MRUK.Instance.GetCurrentRoom();

        // Wait a bit more for anchors to be fully loaded
        yield return new WaitForSeconds(2.0f);

        if (showDebugLogs)
        {
            Debug.Log($"Scene mesh exporter ready. Room loaded with {currentRoom.Anchors.Count} anchors.");
            LogAvailableAnchors();
        }
    }

    private IEnumerator DelayedExport()
    {
        yield return new WaitForSeconds(5.0f); // Give time for scene to fully load
        ExportSceneMesh();
    }

    private void LogAvailableAnchors()
    {
        if (!showDebugLogs) return;

        Debug.Log("=== Available Scene Anchors ===");
        foreach (var anchor in currentRoom.Anchors)
        {
            MeshFilter meshFilter = anchor.GetComponent<MeshFilter>();
            bool hasMesh = meshFilter != null && meshFilter.sharedMesh != null;
            Debug.Log($"Anchor: {anchor.Label} | Has Mesh: {hasMesh} | Position: {anchor.transform.position}");
        }
        Debug.Log("================================");
    }

    [ContextMenu("Export Scene Mesh")]
    public void ExportSceneMesh()
    {
        if (currentRoom == null)
        {
            Debug.LogError("No room available for export. Make sure scene understanding is initialized.");
            return;
        }

        CollectSceneMeshes();

        if (collectedMeshes.Count == 0)
        {
            Debug.LogWarning("No meshes found to export. Check if scene anchors have mesh data.");
            LogAvailableAnchors();
            return;
        }

        string filePath = GetExportPath();

        try
        {
            switch (exportFormat)
            {
                case ExportFormat.OBJ:
                    ExportToOBJ(filePath);
                    break;
                case ExportFormat.PLY:
                    ExportToPLY(filePath);
                    break;
            }

            if (showDebugLogs)
            {
                Debug.Log($"? Scene mesh exported successfully!");
                Debug.Log($"?? File path: {filePath}");
                Debug.Log($"?? Exported {collectedMeshes.Count} mesh objects with {GetTotalVertexCount()} vertices.");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"? Export failed: {e.Message}");
        }
    }

    private void CollectSceneMeshes()
    {
        collectedMeshes.Clear();

        if (currentRoom.Anchors == null || currentRoom.Anchors.Count == 0)
        {
            Debug.LogWarning("No anchors found in current room.");
            return;
        }

        foreach (MRUKAnchor anchor in currentRoom.Anchors)
        {
            if (!ShouldIncludeAnchor(anchor))
                continue;

            // Try to get mesh from different possible sources
            MeshFilter meshFilter = anchor.GetComponent<MeshFilter>();
            if (meshFilter == null || meshFilter.sharedMesh == null)
            {
                // Some anchors might have mesh data in child objects
                meshFilter = anchor.GetComponentInChildren<MeshFilter>();
                if (meshFilter == null || meshFilter.sharedMesh == null)
                    continue;
            }

            Mesh mesh = meshFilter.sharedMesh;
            Transform meshTransform = meshFilter.transform;

            // Skip if mesh has no vertices
            if (mesh.vertexCount == 0)
                continue;

            MeshData meshData = CreateMeshData(mesh, meshTransform, anchor);
            collectedMeshes.Add(meshData);

            if (showDebugLogs)
            {
                Debug.Log($"? Collected mesh: {meshData.objectName} ({mesh.vertexCount} vertices, {mesh.triangles.Length / 3} faces)");
            }
        }
    }

    private MeshData CreateMeshData(Mesh mesh, Transform meshTransform, MRUKAnchor anchor)
    {
        MeshData meshData = new MeshData
        {
            vertices = new Vector3[mesh.vertexCount],
            triangles = mesh.triangles,
            normals = new Vector3[mesh.normals.Length],
            uvs = mesh.uv.Length > 0 ? mesh.uv : new Vector2[mesh.vertexCount],
            objectName = $"{anchor.Label}_{anchor.GetInstanceID()}",
            label = anchor.Label
        };

        // Transform vertices and normals to world space
        for (int i = 0; i < mesh.vertices.Length; i++)
        {
            meshData.vertices[i] = meshTransform.TransformPoint(mesh.vertices[i]);
        }

        for (int i = 0; i < mesh.normals.Length; i++)
        {
            meshData.normals[i] = meshTransform.TransformDirection(mesh.normals[i]).normalized;
        }

        // Generate UVs if none exist
        if (meshData.uvs.Length == 0)
        {
            meshData.uvs = GenerateBasicUVs(meshData.vertices);
        }

        return meshData;
    }

    private Vector2[] GenerateBasicUVs(Vector3[] vertices)
    {
        Vector2[] uvs = new Vector2[vertices.Length];
        for (int i = 0; i < vertices.Length; i++)
        {
            // Simple planar UV mapping
            uvs[i] = new Vector2(vertices[i].x % 1.0f, vertices[i].z % 1.0f);
        }
        return uvs;
    }

    private bool ShouldIncludeAnchor(MRUKAnchor anchor)
    {
        switch (anchor.Label)
        {
            case MRUKAnchor.SceneLabels.WALL_FACE:
                return includeWalls;
            case MRUKAnchor.SceneLabels.INVISIBLE_WALL_FACE:
                return includeInvisibleWalls;
            case MRUKAnchor.SceneLabels.FLOOR:
                return includeFloor;
            case MRUKAnchor.SceneLabels.CEILING:
                return includeCeiling;
            case MRUKAnchor.SceneLabels.TABLE:
            case MRUKAnchor.SceneLabels.COUCH:
            case MRUKAnchor.SceneLabels.BED:
            case MRUKAnchor.SceneLabels.STORAGE:
                return includeFurniture;
            default:
                // Include any other labeled objects if furniture is enabled
                return includeFurniture;
        }
    }

    private void ExportToOBJ(string filePath)
    {
        StringBuilder objContent = new StringBuilder();

        // OBJ Header
        objContent.AppendLine($"# Meta Quest 3 Room Mesh Export");
        objContent.AppendLine($"# Generated on {System.DateTime.Now:yyyy-MM-dd HH:mm:ss}");
        objContent.AppendLine($"# SDK: Meta XR All-in-One SDK v78.0");
        objContent.AppendLine($"# Total objects: {collectedMeshes.Count}");
        objContent.AppendLine($"# Total vertices: {GetTotalVertexCount()}");
        objContent.AppendLine();

        int vertexOffset = 0;

        foreach (MeshData meshData in collectedMeshes)
        {
            objContent.AppendLine($"# Object: {meshData.objectName}");
            objContent.AppendLine($"# Label: {meshData.label}");
            objContent.AppendLine($"# Vertices: {meshData.vertices.Length}");
            objContent.AppendLine($"o {meshData.objectName}");

            // Write vertices
            foreach (Vector3 vertex in meshData.vertices)
            {
                objContent.AppendLine($"v {vertex.x:F6} {vertex.y:F6} {vertex.z:F6}");
            }

            // Write normals
            foreach (Vector3 normal in meshData.normals)
            {
                objContent.AppendLine($"vn {normal.x:F6} {normal.y:F6} {normal.z:F6}");
            }

            // Write UVs
            foreach (Vector2 uv in meshData.uvs)
            {
                objContent.AppendLine($"vt {uv.x:F6} {uv.y:F6}");
            }

            // Write faces
            for (int i = 0; i < meshData.triangles.Length; i += 3)
            {
                int v1 = meshData.triangles[i] + vertexOffset + 1;
                int v2 = meshData.triangles[i + 1] + vertexOffset + 1;
                int v3 = meshData.triangles[i + 2] + vertexOffset + 1;

                if (meshData.normals.Length > 0)
                {
                    objContent.AppendLine($"f {v1}/{v1}/{v1} {v2}/{v2}/{v2} {v3}/{v3}/{v3}");
                }
                else
                {
                    objContent.AppendLine($"f {v1} {v2} {v3}");
                }
            }

            vertexOffset += meshData.vertices.Length;
            objContent.AppendLine();
        }

        File.WriteAllText(filePath, objContent.ToString());
    }

    private void ExportToPLY(string filePath)
    {
        int totalVertices = GetTotalVertexCount();
        int totalFaces = GetTotalFaceCount();

        StringBuilder plyContent = new StringBuilder();

        // PLY Header
        plyContent.AppendLine("ply");
        plyContent.AppendLine("format ascii 1.0");
        plyContent.AppendLine($"comment Meta Quest 3 Room Mesh Export - {System.DateTime.Now:yyyy-MM-dd HH:mm:ss}");
        plyContent.AppendLine($"comment SDK: Meta XR All-in-One SDK v78.0");
        plyContent.AppendLine($"element vertex {totalVertices}");
        plyContent.AppendLine("property float x");
        plyContent.AppendLine("property float y");
        plyContent.AppendLine("property float z");
        plyContent.AppendLine("property float nx");
        plyContent.AppendLine("property float ny");
        plyContent.AppendLine("property float nz");
        plyContent.AppendLine($"element face {totalFaces}");
        plyContent.AppendLine("property list uchar int vertex_indices");
        plyContent.AppendLine("end_header");

        // Write vertices with normals
        foreach (MeshData meshData in collectedMeshes)
        {
            for (int i = 0; i < meshData.vertices.Length; i++)
            {
                Vector3 vertex = meshData.vertices[i];
                Vector3 normal = i < meshData.normals.Length ? meshData.normals[i] : Vector3.up;

                plyContent.AppendLine($"{vertex.x:F6} {vertex.y:F6} {vertex.z:F6} {normal.x:F6} {normal.y:F6} {normal.z:F6}");
            }
        }

        // Write faces
        int vertexOffset = 0;
        foreach (MeshData meshData in collectedMeshes)
        {
            for (int i = 0; i < meshData.triangles.Length; i += 3)
            {
                int v1 = meshData.triangles[i] + vertexOffset;
                int v2 = meshData.triangles[i + 1] + vertexOffset;
                int v3 = meshData.triangles[i + 2] + vertexOffset;

                plyContent.AppendLine($"3 {v1} {v2} {v3}");
            }
            vertexOffset += meshData.vertices.Length;
        }

        File.WriteAllText(filePath, plyContent.ToString());
    }

    private int GetTotalVertexCount()
    {
        int total = 0;
        foreach (var mesh in collectedMeshes)
        {
            total += mesh.vertices.Length;
        }
        return total;
    }

    private int GetTotalFaceCount()
    {
        int total = 0;
        foreach (var mesh in collectedMeshes)
        {
            total += mesh.triangles.Length / 3;
        }
        return total;
    }

    public string GetExportPath()
    {
        return Path.Combine(Application.persistentDataPath, $"{exportFileName}.{exportFormat.ToString().ToLower()}");
    }

    // Public methods for external access
    public bool IsReady()
    {
        return currentRoom != null && currentRoom.Anchors.Count > 0;
    }

    public int GetAnchorCount()
    {
        return currentRoom?.Anchors?.Count ?? 0;
    }
}