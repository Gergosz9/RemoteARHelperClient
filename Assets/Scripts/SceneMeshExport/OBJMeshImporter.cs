using UnityEngine;
using System.IO;

/// <summary>
/// Import and visualize the exported OBJ file back into Unity
/// </summary>
public class OBJMeshImporter : MonoBehaviour
{
    [Header("Import Settings")]
    [Tooltip("Name of the OBJ file to import")]
    public string objFileName = "meta_room_mesh_original.obj";
    
    [Tooltip("Material to apply to the imported mesh")]
    public Material meshMaterial;
    
    [Tooltip("Scale factor for the imported mesh")]
    public float meshScale = 1.0f;
    
    [Tooltip("Enable wireframe view")]
    public bool showWireframe = false;

    private GameObject importedMeshObject;

    [ContextMenu("Import and Visualize OBJ")]
    public void ImportAndVisualizeOBJ()
    {
        string filePath = Path.Combine(Application.persistentDataPath, objFileName);
        
        if (!File.Exists(filePath))
        {
            Debug.LogError($"? OBJ file not found: {filePath}");
            return;
        }

        Debug.Log($"?? Importing OBJ file: {objFileName}");

        try
        {
            // Parse the OBJ file
            var mesh = ParseOBJFile(filePath);
            
            if (mesh == null)
            {
                Debug.LogError("? Failed to parse OBJ file");
                return;
            }

            // Create GameObject to display the mesh
            if (importedMeshObject != null)
            {
                DestroyImmediate(importedMeshObject);
            }

            importedMeshObject = new GameObject($"Imported_{objFileName}");
            importedMeshObject.transform.position = Vector3.zero;
            importedMeshObject.transform.localScale = Vector3.one * meshScale;

            // Add mesh components
            var meshFilter = importedMeshObject.AddComponent<MeshFilter>();
            var meshRenderer = importedMeshObject.AddComponent<MeshRenderer>();

            meshFilter.mesh = mesh;

            // Apply material
            if (meshMaterial != null)
            {
                meshRenderer.material = meshMaterial;
            }
            else
            {
                // Create default material
                var defaultMaterial = new Material(Shader.Find("Standard"));
                defaultMaterial.color = Color.white;
                meshRenderer.material = defaultMaterial;
            }

            // Enable wireframe if requested
            if (showWireframe)
            {
                meshRenderer.material.SetFloat("_Mode", 1); // Transparent
                meshRenderer.material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                meshRenderer.material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                meshRenderer.material.SetInt("_ZWrite", 0);
                meshRenderer.material.DisableKeyword("_ALPHATEST_ON");
                meshRenderer.material.EnableKeyword("_ALPHABLEND_ON");
                meshRenderer.material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
                meshRenderer.material.renderQueue = 3000;
                meshRenderer.material.color = new Color(1, 1, 1, 0.3f);
            }

            Debug.Log($"? Successfully imported mesh:");
            Debug.Log($"   Vertices: {mesh.vertexCount:N0}");
            Debug.Log($"   Triangles: {mesh.triangles.Length / 3:N0}");
            Debug.Log($"   Bounds: {mesh.bounds}");

            // Position camera to view the mesh
            PositionCameraToViewMesh(mesh.bounds);

        }
        catch (System.Exception e)
        {
            Debug.LogError($"? Error importing OBJ: {e.Message}");
        }
    }

    private Mesh ParseOBJFile(string filePath)
    {
        var lines = File.ReadAllLines(filePath);
        var vertices = new System.Collections.Generic.List<Vector3>();
        var normals = new System.Collections.Generic.List<Vector3>();
        var uvs = new System.Collections.Generic.List<Vector2>();
        var triangles = new System.Collections.Generic.List<int>();

        foreach (var line in lines)
        {
            if (line.StartsWith("v "))
            {
                // Parse vertex
                var parts = line.Split(' ');
                if (parts.Length >= 4)
                {
                    if (float.TryParse(parts[1], out float x) &&
                        float.TryParse(parts[2], out float y) &&
                        float.TryParse(parts[3], out float z))
                    {
                        vertices.Add(new Vector3(x, y, z));
                    }
                }
            }
            else if (line.StartsWith("vn "))
            {
                // Parse normal
                var parts = line.Split(' ');
                if (parts.Length >= 4)
                {
                    if (float.TryParse(parts[1], out float x) &&
                        float.TryParse(parts[2], out float y) &&
                        float.TryParse(parts[3], out float z))
                    {
                        normals.Add(new Vector3(x, y, z));
                    }
                }
            }
            else if (line.StartsWith("vt "))
            {
                // Parse UV
                var parts = line.Split(' ');
                if (parts.Length >= 3)
                {
                    if (float.TryParse(parts[1], out float u) &&
                        float.TryParse(parts[2], out float v))
                    {
                        uvs.Add(new Vector2(u, v));
                    }
                }
            }
            else if (line.StartsWith("f "))
            {
                // Parse face
                var parts = line.Split(' ');
                if (parts.Length >= 4) // Triangle or quad
                {
                    var faceVertices = new System.Collections.Generic.List<int>();
                    
                    for (int i = 1; i < parts.Length; i++)
                    {
                        var facePart = parts[i].Split('/')[0]; // Get vertex index only
                        if (int.TryParse(facePart, out int vertexIndex))
                        {
                            faceVertices.Add(vertexIndex - 1); // OBJ is 1-indexed
                        }
                    }

                    // Convert to triangles
                    if (faceVertices.Count >= 3)
                    {
                        // Triangle
                        triangles.Add(faceVertices[0]);
                        triangles.Add(faceVertices[1]);
                        triangles.Add(faceVertices[2]);

                        // If quad, add second triangle
                        if (faceVertices.Count == 4)
                        {
                            triangles.Add(faceVertices[0]);
                            triangles.Add(faceVertices[2]);
                            triangles.Add(faceVertices[3]);
                        }
                    }
                }
            }
        }

        if (vertices.Count == 0 || triangles.Count == 0)
        {
            Debug.LogError("? No valid mesh data found in OBJ file");
            return null;
        }

        // Create Unity mesh
        var mesh = new Mesh();
        mesh.name = objFileName;
        
        // Handle large meshes
        if (vertices.Count > 65535)
        {
            mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
        }

        mesh.vertices = vertices.ToArray();
        mesh.triangles = triangles.ToArray();

        if (normals.Count == vertices.Count)
        {
            mesh.normals = normals.ToArray();
        }
        else
        {
            mesh.RecalculateNormals();
        }

        if (uvs.Count == vertices.Count)
        {
            mesh.uv = uvs.ToArray();
        }

        mesh.RecalculateBounds();

        return mesh;
    }

    private void PositionCameraToViewMesh(Bounds meshBounds)
    {
        var camera = Camera.main;
        if (camera == null) return;

        // Position camera to view the entire mesh
        float distance = Mathf.Max(meshBounds.size.x, meshBounds.size.y, meshBounds.size.z) * 2f;
        
        camera.transform.position = meshBounds.center + Vector3.back * distance + Vector3.up * (meshBounds.size.y * 0.3f);
        camera.transform.LookAt(meshBounds.center);

        Debug.Log($"?? Positioned camera to view mesh at {camera.transform.position}");
    }

    [ContextMenu("Clear Imported Mesh")]
    public void ClearImportedMesh()
    {
        if (importedMeshObject != null)
        {
            DestroyImmediate(importedMeshObject);
            Debug.Log("??? Cleared imported mesh");
        }
    }

    [ContextMenu("Create Wireframe Material")]
    public void CreateWireframeMaterial()
    {
        var wireframeMaterial = new Material(Shader.Find("Standard"));
        wireframeMaterial.name = "WireframeMaterial";
        wireframeMaterial.SetFloat("_Mode", 1); // Transparent
        wireframeMaterial.color = new Color(0, 1, 0, 0.5f); // Green wireframe
        wireframeMaterial.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        wireframeMaterial.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        
        meshMaterial = wireframeMaterial;
        Debug.Log("? Created wireframe material");
    }
}