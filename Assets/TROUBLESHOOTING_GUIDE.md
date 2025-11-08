# Point Cloud System - Step-by-Step Setup Guide

## ?? Troubleshooting "Nothing Happens"

### **Step 1: Add the Debugger (FIRST!)**
1. Add the `PointCloudDebugger` component to any GameObject in your scene
2. Run your scene
3. Press `F1` to see detailed diagnostics
4. Press `F2` to generate test points

This will immediately tell you what's missing!

### **Step 2: Verify Components Are in Scene**

**Required Components:**
1. `DepthPointCloudGenerator` - The main component
2. `DepthKitDriver` - Provides depth data  
3. `OVRManager` - Meta XR manager
4. `OVRCameraRig` - VR camera setup

**How to Add Missing Components:**

```csharp
// If DepthPointCloudGenerator is missing:
var pcgGO = new GameObject("Point Cloud Generator");
var generator = pcgGO.AddComponent<DepthPointCloudGenerator>();

// If DepthKitDriver is missing:
var dkdGO = new GameObject("Depth Kit Driver");
var driver = dkdGO.AddComponent<DepthKitDriver>();
```

### **Step 3: Check Console Messages**

**Look for these messages:**
- ? `"Found shader: PointVisualization/Standard"` - Shader working
- ? `"Using compute kernel: GeneratePoints"` - Compute shader working
- ? `"DepthKitDriver.DepthAvailable is false"` - **Main issue!**
- ? `"No suitable shader found"` - Shader issue

### **Step 4: Fix Depth Data Issues**

If `DepthAvailable` is false:

1. **Add EnvironmentDepthManager:**
```csharp
var edmGO = new GameObject("Environment Depth Manager");
var edm = edmGO.AddComponent<Meta.XR.EnvironmentDepth.EnvironmentDepthManager>();
edm.enabled = true;
```

2. **Check VR Setup:**
   - Make sure you're running on Quest 3+
   - Ensure scene permissions are granted
   - Check that passthrough/mixed reality is enabled

3. **For Testing in Editor:**
   - Use the test points feature: Right-click component ? "Generate Test Points"

### **Step 5: Test Without Real Depth Data**

**Quick Test (Works Immediately):**
1. Select your `DepthPointCloudGenerator` in the Inspector
2. Right-click the component ? "Generate Test Points"
3. Make sure "Visualize Points" is checked
4. You should see a 10x10 grid of colored points in front of the camera

**If test points don't show:**
- Check that a Camera exists in the scene
- Verify the points aren't behind the camera
- Try increasing "Point Size" to 1.0 temporarily

### **Step 6: Common Issues & Fixes**

| Problem | Solution |
|---------|----------|
| No test points visible | Increase Point Size to 1.0, check camera position |
| Shader errors in console | Use `PointVisualization/Standard` shader manually |
| "Compute kernel not found" | Assign the `DepthPointCloudCompute.compute` file |
| Points appear too small | Increase Point Size or reduce maxDepth |
| No depth data in VR | Add EnvironmentDepthManager, check permissions |

### **Step 7: Manual Setup (If Auto-Setup Fails)**

1. **Create Materials Manually:**
   - Right-click in Project ? Create ? Material
   - Set shader to `PointVisualization/Standard`
   - Assign to `Point Material` field

2. **Assign Compute Shader:**
   - Drag `DepthPointCloudCompute.compute` to `Point Cloud Compute` field

3. **Check Settings:**
   - Max Points: 100000
   - Min Depth: 0.1
   - Max Depth: 10
   - Point Size: 0.01 (increase if nothing visible)
   - Visualize Points: ? Enabled

## ?? Expected Results

### **With Test Points (Should Always Work):**
- 100 colored points arranged in a 10x10 grid
- Points positioned 3 meters in front of camera
- Colors range from red (near) to blue (far)

### **With Real Depth Data (Quest 3+ Only):**
- Thousands of points showing environment surfaces
- Points update in real-time as you move
- Depth-based coloring of real world surfaces

## ?? Debugging Checklist

Run through this list:

- [ ] Added `PointCloudDebugger` to scene
- [ ] Pressed F1 to run diagnostics  
- [ ] All required components present
- [ ] Console shows `"Found shader"` message
- [ ] Test points generate successfully (F2 key)
- [ ] Test points are visible
- [ ] If using VR: EnvironmentDepthManager added and enabled
- [ ] If no depth data: Using test points for now

## ?? Most Common Issue: Missing EnvironmentDepthManager

If you're getting `DepthAvailable = false`, you likely need:

```csharp
// Add this to your scene
var go = new GameObject("Environment Depth Manager");
var edm = go.AddComponent<Meta.XR.EnvironmentDepth.EnvironmentDepthManager>();
edm.enabled = true;
```

This is separate from your DepthKitDriver and is required for Meta's depth system to work.

---

**Need Help?** Run the debugger (`F1`) and check the console output. The diagnostic messages will tell you exactly what's missing!