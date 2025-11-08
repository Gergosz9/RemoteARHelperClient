# Quest Scene Mesh Extractor Suite

A comprehensive set of Unity scripts designed to extract actual scene mesh data from Meta Quest's spatial understanding system, similar to the [Needle Engine XR mesh detection sample](https://engine.needle.tools/samples/xr-mesh-detection/?room=needle802).

## ?? **BREAKTHROUGH: Official Meta API Access!**

**The `RoomMeshController.cs` file you found is THE KEY!** This shows us exactly how Meta accesses the actual triangle mesh data. Based on this discovery, we now have a script that uses Meta's official Building Blocks to extract the real scene mesh.

## Scripts Overview

### 1. **MetaRoomMeshExtractor.cs** ? **RECOMMENDED** ?

**The Official Solution - Works with Meta's Building Blocks:**
- **Direct Integration**: Works with `RoomMeshController` and `RoomMeshEvent`
- **Official API**: Uses Meta's proper triangle mesh extraction pipeline
- **Automatic Extraction**: Hooks into the room mesh load event
- **Multiple Formats**: Exports OBJ and PLY with vertex colors
- **Full Mesh Data**: Gets the actual processed triangle mesh from Quest

**Key Features:**
- Subscribes to `OnRoomMeshLoadCompleted` event
- Extracts mesh directly from Meta's `MeshFilter` component
- Supports both original and processed (colored) mesh export
- Includes comprehensive scene setup verification

### 2. EnhancedAutoRoomMeshExporter.cs (Enhanced Version)

**Main Features:**
- **OVR Scene API Access**: Attempts to access mesh data through OVR Scene components
- **MRUK Integration**: Works with Meta's MR Utility Kit
- **Reflection-based Extraction**: Uses reflection to access private/internal APIs
- **Multiple Fallback Methods**: Tries various approaches to find mesh data
- **Comprehensive Debugging**: Detailed logging to understand what's available

### 3. OVRSceneMeshExtractor.cs (OVR-Focused)

**Specialized for OVR Scene API:**
- **OVRSceneManager Detection**: Finds and extracts from OVRSceneManager
- **OVRSceneAnchor Processing**: Processes individual scene anchors
- **TriangleMesh Components**: Specifically looks for triangle mesh data
- **Deep Reflection Analysis**: Thorough property and method inspection

### 4. QuestSpatialMeshExtractor.cs (Comprehensive)

**Most Advanced Script:**
- **Multi-Method Extraction**: Combines all available extraction techniques
- **Spatial Data Analysis**: Advanced mesh data detection and conversion
- **Robust Format Support**: OBJ and PLY export with full feature support
- **Hidden Component Search**: Finds mesh data in unexpected places
- **Synthetic Mesh Generation**: Creates room layout when real mesh unavailable

## ?? **Setup Instructions (OFFICIAL METHOD)**

### **Step 1: Add Room Mesh Building Block**

This is the **CRITICAL** step that was missing from previous approaches:

1. **In Unity Editor:**
   - Go to **Meta XR > Building Blocks**
   - Find and add the **"Room Mesh"** Building Block to your scene
   - This automatically adds `RoomMeshController` and `RoomMeshEvent` components

2. **Alternative Manual Setup:**
   - Create an empty GameObject
   - Add `RoomMeshController` component
   - Add `RoomMeshEvent` component
   - Configure the mesh prefab in RoomMeshController

### **Step 2: Add MetaRoomMeshExtractor**

1. **Add the extractor script** to any GameObject in your scene
2. **Configure settings** in inspector:
   - Set `exportFileName` (default: "meta_room_mesh")
   - Enable `autoExtractOnLoad` (recommended)
   - Enable `enableLogging` for debugging
3. **Component References** (auto-found):
   - Script will automatically find `RoomMeshController`
   - Script will automatically find `RoomMeshEvent`

### **Step 3: Quest Device Setup**

**Critical Requirements:**
1. **Enable Scene Understanding** in Quest settings:
   - Go to Settings > Device > Scene Understanding
   - Turn on "Scene Understanding"
   
2. **Scan Your Room**:
   - Use the Room Setup in Quest settings
   - Ensure recent scan data is available
   
3. **Grant Spatial Data Permission**:
   - Allow your app to access spatial data when prompted

## ?? **Usage Guide**

### **Automatic Extraction (Recommended)**

The script automatically hooks into Meta's room mesh loading:

```csharp
// When room mesh loads, OnRoomMeshLoadCompleted is called
// Mesh is automatically extracted and exported
// No manual intervention required!
```

### **Manual Extraction**

Use context menu options:

```csharp
// Right-click MetaRoomMeshExtractor component:
// - "Manual Extract Mesh" - Extract currently loaded mesh
// - "Check Scene Setup" - Verify all components are present
```

### **Verification**

Use the built-in diagnostics:

```csharp
// The "Check Scene Setup" context menu will show:
// - RoomMeshController status
// - RoomMeshEvent status  
// - Available mesh data
// - Scene permissions
```

## ?? **What You Get**

### **Real Scene Mesh Data:**
- **Actual Triangle Mesh**: Real geometry from Quest's spatial understanding
- **Vertex Colors**: Meta's processing adds RGB vertex colors
- **World Space Coordinates**: Properly transformed mesh data
- **Professional Export**: Clean OBJ and PLY files

### **Export Files:**
- `meta_room_mesh_original.obj` - Raw mesh data
- `meta_room_mesh_processed.obj` - Mesh with vertex colors (if available)
- `meta_room_mesh_colored.ply` - PLY format with RGB vertex colors

## ?? **How It Works (Technical)**

Based on Meta's `RoomMeshController.cs`, the extraction process:

1. **Permission Check**: Verifies `OVRPermissionsRequester.Permission.Scene`
2. **Anchor Fetching**: Uses `OVRAnchor.FetchAnchorsAsync` with `OVRTriangleMesh` filter
3. **Mesh Processing**: `RoomMeshController` processes raw triangle data
4. **Event Notification**: `OnRoomMeshLoadCompleted` fires with `MeshFilter`
5. **Data Extraction**: We capture the processed mesh from the `MeshFilter`

The key insight from the official implementation:
```csharp
// This is the actual API call Meta uses:
var task = OVRAnchor.FetchAnchorsAsync(anchors, new OVRAnchor.FetchOptions
{
    SingleComponentType = typeof(OVRTriangleMesh)  // THE triangle mesh data!
});
```

## ?? **File Locations**

Exported files are saved to:
```
Application.persistentDataPath
// On Quest: /storage/emulated/0/Android/data/[YourAppPackage]/files/
// On Windows: %userprofile%/AppData/LocalLow/[CompanyName]/[ProductName]/
```

## ?? **Troubleshooting**

### **No Room Mesh Building Block**

**Error:** `"RoomMeshController not found in scene!"`

**Solution:**
1. Add Room Mesh Building Block via Meta XR menu
2. Or manually add `RoomMeshController` and `RoomMeshEvent` components

### **No Mesh Data**

**Error:** `"No room mesh found!"`

**Solutions:**
1. **Check Permissions:** Run "Check Scene Setup" context menu
2. **Verify Room Scan:** Ensure room is scanned on Quest device
3. **Enable Scene Understanding:** Check Quest device settings
4. **Wait Longer:** Room mesh loading can take time

### **Permission Issues**

**Error:** `"Scene Permission: ? Not Granted"`

**Solutions:**
1. Grant permission when app prompts
2. Check Quest privacy settings
3. Enable spatial data access for your app

## ?? **Success Indicators**

**You'll know it's working when you see:**
```
[MetaRoomMeshExtractor] ?? Room mesh loaded successfully!
[MetaRoomMeshExtractor] ?? Mesh data: 15420 vertices, 5140 triangles
[MetaRoomMeshExtractor] ?? SUCCESS! Room mesh extraction completed!
[MetaRoomMeshExtractor] ?? Files saved to: [path]
```

**Expected Results:**
- **High vertex count** (thousands of vertices for a real room)
- **Vertex colors present** (RGB data from Meta's processing)
- **Multiple export files** (OBJ and PLY formats)
- **Large file sizes** (room meshes are detailed)

## ?? **Comparison with Needle Engine Sample**

**Similarities:**
- Both access raw spatial mesh data
- Both export to standard formats
- Both provide real room geometry

**Differences:**
- **API Access**: Needle uses WebXR, this uses Meta XR SDK Building Blocks
- **Platform**: Needle runs in browser, this runs natively on Quest
- **Data Source**: Both use spatial understanding but through different APIs
- **Processing**: Meta's Building Blocks add vertex coloring and optimization

## ?? **Previous vs. Current Approach**

**Previous Issues:**
- ? Trying to find mesh data through reflection
- ? Searching for hidden OVR components
- ? Generating synthetic room layouts
- ? No official API pathway

**Current Solution:**
- ? Uses Meta's official Building Blocks
- ? Hooks into documented event system
- ? Gets processed, real triangle mesh data
- ? Works exactly like Meta's own implementation

## ?? **Key Takeaway**

**The `RoomMeshController.cs` file you found was the missing piece!** It showed us that:

1. **Meta has official Building Blocks** for room mesh access
2. **The data flows through `RoomMeshEvent.OnRoomMeshLoadCompleted`**
3. **Real mesh data IS available** through the proper API
4. **No reflection or hacks needed** - just use the official components

This is now a **proper, supported method** for extracting Quest scene mesh data, just like the Needle Engine sample but using Meta's native APIs.

## ?? **Additional Resources**

- **Meta XR Building Blocks Documentation**
- **Scene Understanding Guide**
- **Spatial Data Permissions Setup**
- **Room Setup Instructions for Quest**

---

**Result:** You now have access to the actual, real triangle mesh data from Quest's spatial understanding system through Meta's official APIs! ??