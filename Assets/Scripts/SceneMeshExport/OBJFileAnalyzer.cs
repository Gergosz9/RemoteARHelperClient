using UnityEngine;
using System.IO;
using System.Linq;

/// <summary>
/// Quick OBJ file analyzer to verify exported mesh data
/// </summary>
public class OBJFileAnalyzer : MonoBehaviour
{
    [Header("Analysis Settings")]
    [Tooltip("Path to the OBJ file to analyze")]
    public string objFilePath = "meta_room_mesh_original.obj";
    
    [Tooltip("Show detailed analysis in console")]
    public bool showDetailedAnalysis = true;

    [ContextMenu("Analyze OBJ File")]
    public void AnalyzeOBJFile()
    {
        string fullPath = Path.Combine(Application.persistentDataPath, objFilePath);
        
        if (!File.Exists(fullPath))
        {
            Debug.LogError($"? OBJ file not found: {fullPath}");
            return;
        }

        Debug.Log("=== OBJ FILE ANALYSIS ===");
        Debug.Log($"?? File: {objFilePath}");
        
        try
        {
            var lines = File.ReadAllLines(fullPath);
            
            // Count different types of data
            int vertexCount = lines.Count(line => line.StartsWith("v "));
            int normalCount = lines.Count(line => line.StartsWith("vn "));
            int uvCount = lines.Count(line => line.StartsWith("vt "));
            int faceCount = lines.Count(line => line.StartsWith("f "));
            int objectCount = lines.Count(line => line.StartsWith("o "));
            int commentCount = lines.Count(line => line.StartsWith("#"));
            
            // File size
            var fileInfo = new FileInfo(fullPath);
            long fileSizeKB = fileInfo.Length / 1024;
            long fileSizeMB = fileSizeKB / 1024;
            
            // Analysis
            Debug.Log($"?? MESH STATISTICS:");
            Debug.Log($"   Vertices (v): {vertexCount:N0}");
            Debug.Log($"   Faces (f): {faceCount:N0}");
            Debug.Log($"   Normals (vn): {normalCount:N0}");
            Debug.Log($"   UVs (vt): {uvCount:N0}");
            Debug.Log($"   Objects (o): {objectCount:N0}");
            Debug.Log($"   Comments (#): {commentCount:N0}");
            Debug.Log($"   Total Lines: {lines.Length:N0}");
            
            Debug.Log($"?? FILE SIZE:");
            Debug.Log($"   {fileSizeKB:N0} KB ({fileSizeMB:N0} MB)");
            
            // Quality assessment
            Debug.Log($"? QUALITY ASSESSMENT:");
            
            if (vertexCount > 10000)
                Debug.Log($"   ?? EXCELLENT vertex count ({vertexCount:N0}) - This looks like real room data!");
            else if (vertexCount > 1000)
                Debug.Log($"   ? Good vertex count ({vertexCount:N0}) - Likely real mesh data");
            else if (vertexCount > 100)
                Debug.Log($"   ?? Low vertex count ({vertexCount:N0}) - May be simplified or synthetic");
            else
                Debug.Log($"   ? Very low vertex count ({vertexCount:N0}) - Likely test/synthetic data");
            
            if (faceCount > 0)
                Debug.Log($"   ? Has face data ({faceCount:N0} faces)");
            else
                Debug.Log($"   ? No face data - mesh won't be visible");
            
            if (normalCount > 0)
                Debug.Log($"   ? Has normal data ({normalCount:N0} normals)");
            else
                Debug.Log($"   ?? No normal data - may appear flat");
            
            if (fileSizeMB > 1)
                Debug.Log($"   ?? Large file size ({fileSizeMB}MB) - Rich mesh data!");
            else if (fileSizeKB > 100)
                Debug.Log($"   ? Good file size ({fileSizeKB}KB)");
            else
                Debug.Log($"   ?? Small file size ({fileSizeKB}KB) - Limited data");
            
            // Header analysis
            if (showDetailedAnalysis)
            {
                Debug.Log($"?? HEADER INFORMATION:");
                var headerLines = lines.Take(20).Where(line => line.StartsWith("#")).ToArray();
                foreach (var header in headerLines)
                {
                    Debug.Log($"   {header}");
                }
                
                // Sample vertex data
                Debug.Log($"?? SAMPLE VERTEX DATA:");
                var vertexLines = lines.Where(line => line.StartsWith("v ")).Take(5).ToArray();
                foreach (var vertex in vertexLines)
                {
                    Debug.Log($"   {vertex}");
                }
                
                // Vertex range analysis
                AnalyzeVertexRange(lines);
            }
            
            Debug.Log("=== ANALYSIS COMPLETE ===");
            
        }
        catch (System.Exception e)
        {
            Debug.LogError($"? Error analyzing file: {e.Message}");
        }
    }
    
    private void AnalyzeVertexRange(string[] lines)
    {
        var vertexLines = lines.Where(line => line.StartsWith("v ")).ToArray();
        
        if (vertexLines.Length == 0) return;
        
        float minX = float.MaxValue, maxX = float.MinValue;
        float minY = float.MaxValue, maxY = float.MinValue;
        float minZ = float.MaxValue, maxZ = float.MinValue;
        
        foreach (var line in vertexLines.Take(1000)) // Sample first 1000 vertices
        {
            var parts = line.Split(' ');
            if (parts.Length >= 4)
            {
                if (float.TryParse(parts[1], out float x) &&
                    float.TryParse(parts[2], out float y) &&
                    float.TryParse(parts[3], out float z))
                {
                    minX = Mathf.Min(minX, x); maxX = Mathf.Max(maxX, x);
                    minY = Mathf.Min(minY, y); maxY = Mathf.Max(maxY, y);
                    minZ = Mathf.Min(minZ, z); maxZ = Mathf.Max(maxZ, z);
                }
            }
        }
        
        Debug.Log($"?? MESH BOUNDS (sampled):");
        Debug.Log($"   X: {minX:F2} to {maxX:F2} (size: {(maxX-minX):F2}m)");
        Debug.Log($"   Y: {minY:F2} to {maxY:F2} (size: {(maxY-minY):F2}m)");
        Debug.Log($"   Z: {minZ:F2} to {maxZ:F2} (size: {(maxZ-minZ):F2}m)");
        
        // Room size assessment
        float roomWidth = Mathf.Max(maxX - minX, maxZ - minZ);
        float roomHeight = maxY - minY;
        
        if (roomWidth > 2 && roomHeight > 2)
            Debug.Log($"   ?? Room-like dimensions detected! ~{roomWidth:F1}m x {roomHeight:F1}m");
        else
            Debug.Log($"   ?? Unusual dimensions for a room");
    }

    [ContextMenu("Show File Path")]
    public void ShowFilePath()
    {
        string fullPath = Path.Combine(Application.persistentDataPath, objFilePath);
        Debug.Log($"?? Full file path: {fullPath}");
        Debug.Log($"?? Persistent data path: {Application.persistentDataPath}");
        
        if (File.Exists(fullPath))
        {
            var fileInfo = new FileInfo(fullPath);
            Debug.Log($"? File exists: {fileInfo.Length / 1024} KB");
            Debug.Log($"?? Created: {fileInfo.CreationTime}");
        }
        else
        {
            Debug.Log($"? File not found");
        }
    }
}