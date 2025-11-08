# Environment Depth Point Cloud Setup Guide

This guide explains how to set up and use the Environment Depth Point Cloud system to extract 3D point cloud data from Meta Quest environment depth.

## Overview

The system consists of three main scripts:

1. **EnvironmentDepthPointCloud.cs** - Main component that captures depth data and converts it to point clouds
2. **PointCloudExample.cs** - Example usage and file export functionality  
3. **PointCloudUtils.cs** - Utility functions for processing point cloud data

## Setup Instructions

### 1. Prerequisites
- Meta XR SDK Core package installed
- Environment Depth feature enabled in your project
- A VR scene with EnvironmentDepthManager component

### 2. Adding the Point Cloud System

1. **Add the EnvironmentDepthPointCloud component** to the same GameObject that has the EnvironmentDepthManager:
   ```csharp
   // In the Inspector, add the EnvironmentDepthPointCloud component
   // Configure the settings:
   // - Generate Point Cloud: true
   // - Subsample Rate: 4 (adjust for performance vs quality)
   // - Max Depth Distance: 10.0f
   // - Filter Invalid Depth: true
   ```

2. **Set up eye cameras** (optional but recommended for better accuracy):
   - Assign Left Eye Camera and Right Eye Camera in the inspector
   - The script will try to find these automatically if not assigned

3. **Add the PointCloudExample component** to any GameObject to handle point cloud events:
   ```csharp
   // Configure in Inspector:
   // - Point Cloud Generator: Reference to EnvironmentDepthPointCloud component
   // - Save To File: true (if you want to export PLY files)
   // - Log Point Count: true (for debugging)
   // - Capture Interval: 1.0f (seconds between captures)
   ```

### 3. Using the Point Cloud Data

#### A. Subscribe to Events
```csharp
public class MyPointCloudHandler : MonoBehaviour
{
    private EnvironmentDepthPointCloud pointCloudGen;
    
    void Start()
    {
        pointCloudGen = FindFirstObjectByType<EnvironmentDepthPointCloud>();
        pointCloudGen.OnPointCloudGenerated += OnPointCloudReceived;
    }
    
    void OnPointCloudReceived(Vector3[] points, Color[] colors)
    {
        Debug.Log($"Received {points.Length} points");
        
        // Process your point cloud data here
        ProcessPointCloud(points, colors);
    }
    
    void ProcessPointCloud(Vector3[] points, Color[] colors)
    {
        // Your custom processing logic
        // Examples:
        // - Mesh reconstruction
        // - Object detection
        // - Surface analysis
        // - Spatial mapping
    }
}
```

#### B. Manual Point Cloud Capture
```csharp
// Trigger point cloud generation manually
EnvironmentDepthPointCloud pointCloud = GetComponent<EnvironmentDepthPointCloud>();
pointCloud.GeneratePointCloud();

// Get the latest captured data
Vector3[] points = pointCloud.GetPointCloudData();
Color[] colors = pointCloud.GetPointCloudColors();
```

#### C. Using Utility Functions
```csharp
using static PointCloudUtils;

// Filter points by distance
var (filteredPoints, filteredColors) = FilterByDistance(points, colors, 
    Camera.main.transform.position, 0.5f, 5.0f);

// Downsample for performance
var (downsampled, downsampledColors) = Downsample(points, colors, 2);

// Remove outliers
var (cleaned, cleanedColors) = RemoveOutliers(points, colors, 2.0f);

// Get bounding box
Bounds bounds = GetBoundingBox(points);

// Estimate normals
Vector3[] normals = EstimateNormals(points, 0.1f);

// Create mesh from point cloud
Mesh mesh = CreateMeshFromPointCloud(points, colors);
```

### 4. Performance Optimization

#### Settings to Adjust for Performance:
- **Subsample Rate**: Higher values = fewer points but better performance
- **Max Depth Distance**: Smaller values = fewer points processed
- **Capture Interval**: Longer intervals = less frequent updates

#### Code Optimizations:
```csharp
// Process point clouds on a separate thread (advanced)
// Use Compute Shaders for large point clouds
// Implement spatial data structures (octrees, KD-trees) for fast queries
```

### 5. File Export

The system can export point clouds as PLY files:

```csharp
// PLY files will be saved to Application.persistentDataPath
// Files are named: pointcloud_YYYYMMDD_HHMMSS.ply
// Compatible with software like MeshLab, CloudCompare, Blender
```

### 6. Troubleshooting

#### Common Issues:

1. **No point cloud data generated**:
   - Ensure EnvironmentDepthManager.IsSupported returns true
   - Check that depth textures are available (EnvironmentDepthManager.IsDepthAvailable)
   - Verify proper permissions are granted

2. **Poor quality point clouds**:
   - Reduce subsample rate for more dense points
   - Adjust max depth distance
   - Ensure good lighting conditions for depth sensing

3. **Performance issues**:
   - Increase subsample rate
   - Reduce capture frequency
   - Filter points by distance or region of interest

#### Debug Information:
```csharp
// Check depth manager status
Debug.Log($"Depth Supported: {EnvironmentDepthManager.IsSupported}");
Debug.Log($"Depth Available: {depthManager.IsDepthAvailable}");

// Monitor point cloud generation
Debug.Log($"Point count: {points.Length}");
Debug.Log($"Bounding box: {PointCloudUtils.GetBoundingBox(points)}");
```

### 7. Integration Examples

#### A. Real-time Mesh Reconstruction
```csharp
void OnPointCloudReceived(Vector3[] points, Color[] colors)
{
    Mesh mesh = PointCloudUtils.CreateMeshFromPointCloud(points, colors);
    GetComponent<MeshFilter>().mesh = mesh;
}
```

#### B. Collision Detection
```csharp
void OnPointCloudReceived(Vector3[] points, Color[] colors)
{
    // Check if any points are near the player
    Vector3 playerPos = transform.position;
    foreach (Vector3 point in points)
    {
        if (Vector3.Distance(playerPos, point) < 0.5f)
        {
            // Handle collision/proximity
            OnEnvironmentCollision(point);
        }
    }
}
```

#### C. Spatial Analysis
```csharp
void OnPointCloudReceived(Vector3[] points, Color[] colors)
{
    // Find floor plane
    Vector3[] floorPoints = FindFloorPoints(points);
    
    // Detect walls
    Vector3[] wallPoints = FindWallPoints(points);
    
    // Calculate room boundaries
    Bounds roomBounds = PointCloudUtils.GetBoundingBox(points);
}
```

## API Reference

### EnvironmentDepthPointCloud Properties
- `OnPointCloudGenerated` - Event fired when new point cloud is available
- `GeneratePointCloud()` - Manually trigger point cloud generation
- `GetPointCloudData()` - Get latest point cloud positions
- `GetPointCloudColors()` - Get latest point cloud colors

### PointCloudUtils Static Methods
- `FilterByDistance()` - Filter points by distance from reference point
- `Downsample()` - Reduce point density
- `RemoveOutliers()` - Remove statistically abnormal points
- `GetBoundingBox()` - Calculate bounding box
- `EstimateNormals()` - Calculate surface normals
- `CreateMeshFromPointCloud()` - Convert points to mesh
- `GetCentroid()` - Calculate center point

## Notes

- The system works with both Oculus and OpenXR providers
- Point cloud coordinates are in world space
- Colors are generated based on depth (blue = close, red = far)
- PLY export format is compatible with most 3D software
- Performance scales with image resolution and subsample rate