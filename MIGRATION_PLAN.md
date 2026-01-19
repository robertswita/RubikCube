# Migration Plan: Windows Forms .NET 8 + OpenTK 4.x

## Overview
Migrate the Windows Forms RubikCube project from .NET Framework 4.8 with custom P/Invoke OpenGL to .NET 8 with OpenTK 4.x.

## Current Architecture
- **OpenGL.cs**: Custom P/Invoke wrapper to opengl32.dll with both legacy (glBegin/glEnd) and modern (VBO, VAO, shader) functions
- **TGLContext.cs**: Renders using immediate mode (glBegin/glEnd/GL_QUADS) - DEPRECATED in modern OpenGL
- **TGLView.cs**: Windows Forms UserControl wrapper
- **Win32.cs**: WGL P/Invoke for context creation
- **TShape.cs**: Pure data class (vertices, faces, colors) - NO CHANGES NEEDED
- **TAffine.cs**: Pure math library - NO CHANGES NEEDED

## Migration Steps

### Step 1: Convert csproj to SDK-style .NET 8
- Convert RubikCubeND.csproj to SDK-style format
- Target net8.0-windows
- Add OpenTK and OpenTK.WinForms packages

### Step 2: Replace TGLView with GLControl
- Replace custom UserControl with OpenTK's GLControl
- Update Form1.Designer.cs references

### Step 3: Modernize OpenGL calls
- Replace P/Invoke OpenGL.cs with OpenTK's GL class
- Update TGLContext to use OpenTK bindings

### Step 4: Convert immediate mode to VBO rendering
- Create vertex/fragment shaders
- Replace glBegin/glEnd with VBO-based rendering
- Use shader uniforms for MVP matrix

### Step 5: Cleanup
- Remove unused Win32.cs WGL code
- Remove GA/IsExternalInit.cs polyfill (not needed in .NET 8)

## Files to Modify
| File | Action |
|------|--------|
| RubikCubeND.csproj | Convert to SDK-style .NET 8 |
| OpenGL.cs | Rewrite for OpenTK |
| Win32.cs | Remove WGL code |
| TGLView.cs | Replace with GLControl |
| TGLView.Designer.cs | Update |
| TGLContext.cs | Rewrite for modern OpenGL |
| Form1.Designer.cs | Update TGLView references |

## Files Unchanged
- TShape.cs, TAffine.cs, TVector.cs, TMatrix.cs (pure math)
- All GA/* files (no OpenGL dependency)
- TRubikCube.cs, TCubie.cs (no OpenGL dependency)
