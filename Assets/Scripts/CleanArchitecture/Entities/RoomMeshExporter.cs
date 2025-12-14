using Assets.Scripts.CleanArchitecture.Gateways;
using System.Collections;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Events;

namespace Assets.Scripts.CleanArchitecture.Entities
{
    internal class RoomMeshExporter
    {
        bool hasExtracted = false;
        bool exportOnLoad;
        private MeshFilter extractedMeshFilter;
        private const string exportFileName = "RoomMesh_Exported";
        public UnityEvent OnExportCompleted = new UnityEvent();

        public RoomMeshExporter(IRoomMeshGateway roomMeshGateway, bool autoExport = false) 
        {
            roomMeshGateway.RoomMeshCreated().AddListener(OnRoomMeshLoaded);
            exportOnLoad = autoExport;
        }

        /// <summary>
        /// Called when the room mesh loads successfully
        /// </summary>
        /// <param name="meshFilter">The MeshFilter containing the room mesh data</param>
        private void OnRoomMeshLoaded(MeshFilter meshFilter)
        {
            Debug.Log("Room mesh loaded successfully!");

            if (meshFilter == null || meshFilter.sharedMesh == null)
            {
                Debug.LogError("Mesh filter is null or has no mesh data");
                return;
            }

            extractedMeshFilter = meshFilter;

            Debug.Log($"Mesh data: {meshFilter.sharedMesh.vertexCount} vertices, {meshFilter.sharedMesh.triangles.Length / 3} triangles");

            if (exportOnLoad && !hasExtracted)
            {
                Debug.Log("Auto-exporting mesh...");
                _ = ExtractAndExportMesh();
            }
        }

        /// <summary>
        /// Extract and export the mesh data
        /// </summary>
        public async Task ExtractAndExportMesh()
        {
            if (extractedMeshFilter == null || extractedMeshFilter.sharedMesh == null)
            {
                Debug.LogError("No mesh data to extract");
                return;
            }

            var mesh = extractedMeshFilter.sharedMesh;
            var transform = extractedMeshFilter.transform;

            Debug.Log("Processing mesh data...");

            await ExportMeshAsOBJ(mesh, transform, exportFileName);
            Debug.Log("Mesh exported");

            hasExtracted = true;
            LogSuccess(mesh);

            OnExportCompleted.Invoke();
        }

        /// <summary>
        /// Export mesh as OBJ format
        /// </summary>
        private async Task ExportMeshAsOBJ(Mesh mesh, Transform meshTransform, string filename)
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

            // Write vertices (transform to world space for OBJ format)
            foreach (var vertex in mesh.vertices)
            {
                Vector3 worldVertex = meshTransform.TransformPoint(vertex);
                obj.AppendLine($"v {worldVertex.x:F6} {worldVertex.y:F6} {worldVertex.z:F6}");
            }

            // Write normals (transform to world space and flip Z)
            if (mesh.normals.Length > 0)
            {
                foreach (var normal in mesh.normals)
                {
                    Vector3 worldNormal = meshTransform.TransformDirection(normal).normalized;
                    obj.AppendLine($"vn {worldNormal.x:F6} {worldNormal.y:F6} {worldNormal.z:F6}");
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
                    obj.AppendLine($"f {v1}/{v1}/{v1} {v2}/{v2}/{v2} {v3}/{v3}/{v3}");
                }
                else if (mesh.normals.Length > 0)
                {
                    obj.AppendLine($"f {v1}//{v1} {v2}//{v2} {v3}//{v3}");
                }
                else
                {
                    obj.AppendLine($"f {v1} {v2} {v3}");
                }
            }

            await File.WriteAllTextAsync(path, obj.ToString());
            Debug.Log($"OBJ exported to: {path}");
        }

        private void LogSuccess(Mesh mesh)
        {
            Debug.Log("SUCCESS! Room mesh extraction completed!");
            Debug.Log($"Mesh Statistics:");
            Debug.Log($"   Vertices: {mesh.vertexCount}");
            Debug.Log($"   Triangles: {mesh.triangles.Length / 3}");
            Debug.Log($"   Has Colors: {mesh.colors.Length > 0} ({mesh.colors.Length} colors)");
            Debug.Log($"   Has Normals: {mesh.normals.Length > 0} ({mesh.normals.Length} normals)");
            Debug.Log($"   Has UVs: {mesh.uv.Length > 0} ({mesh.uv.Length} UVs)");
            Debug.Log($"Files saved to: {Application.persistentDataPath}");

            // Show file paths
            Debug.Log($"Exported files:");
            Debug.Log($"   {exportFileName}.obj");
        }

        public string GetExportedFilePath()
        {
            return Path.Combine(Application.persistentDataPath, $"{exportFileName}.obj");
        }
    }
}
