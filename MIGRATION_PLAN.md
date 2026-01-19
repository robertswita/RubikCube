# Migration Plan: Windows Forms .NET 8 + OpenTK 4.x

## Status: IN PROGRESS

## Completed Steps

### Step 1: Convert csproj to SDK-style .NET 8 ✓
- Converted RubikCubeND.csproj to SDK-style format
- Target net8.0-windows
- Added OpenTK and OpenTK.WinForms packages

### Step 2: Replace TGLView with GLControl ✓
- Replaced custom UserControl with OpenTK's GLControl
- Updated TGLView.cs and TGLView.Designer.cs

### Step 3: Modernize OpenGL calls with OpenTK ✓
- Rewrote TGLContext.cs to use modern OpenGL with shaders
- Created vertex and fragment shaders (embedded in code)
- Converted immediate mode (glBegin/glEnd) to VBO-based rendering

### Step 4: Remove custom OpenGL wrapper ✓
- Removed OpenGL.cs (custom P/Invoke wrapper)
- Removed Win32.cs (WGL context creation)
- Removed GA/IsExternalInit.cs (not needed in .NET 8)

### Step 5: Replace DataVisualization with LiveCharts ✓
- Updated Form1.Designer.cs to use CartesianChart
- Updated Form1.cs to use ObservableCollection for chart data
- Removed DataVisualization references from TMove.cs

## Files Modified
| File | Status |
|------|--------|
| RubikCubeND.csproj | ✓ Converted to SDK-style .NET 8 |
| TGLView.cs | ✓ Now inherits from GLControl |
| TGLView.Designer.cs | ✓ Updated |
| TGLContext.cs | ✓ Rewritten with modern OpenGL |
| Form1.cs | ✓ Updated for LiveCharts |
| Form1.Designer.cs | ✓ Updated for LiveCharts |
| TMove.cs | ✓ Removed DataVisualization reference |

## Files Removed
| File | Reason |
|------|--------|
| OpenGL.cs | Replaced by OpenTK GL class |
| Win32.cs | GLControl handles context creation |
| GA/IsExternalInit.cs | Not needed in .NET 8 |

## Files Unchanged
- TShape.cs, TAffine.cs, TVector.cs, TMatrix.cs (pure math)
- All GA/* files (no OpenGL dependency)
- TRubikCube.cs, TCubie.cs (no OpenGL dependency)

## Next Steps
1. Build and test on Windows
2. Fix any remaining compilation errors
3. Test rendering functionality
