---
name: build-workflow
description: "User builds/runs the project themselves in Visual Studio; don't run dotnet build to \"verify\""
metadata: 
  node_type: memory
  type: feedback
  originSessionId: 95217005-1638-45fd-b8b9-7a2254e5a463
  modified: 2026-07-25T19:04:31.321Z
---

The user compiles and runs RubikCubeND themselves in Visual Studio (this is a .NET WinForms + GLSL-at-runtime project). After editing shader (`.glsl.c`) or C# files, DON'T run `dotnet build` as a verification step — the user rejected those calls repeatedly and just wants the edits saved to disk.

**Why:** GLSL compiles at runtime (embedded resources), so a `dotnet build` only checks the C# side and re-embeds the shader — redundant with the user's own VS build/run. It wastes a turn.

**How to apply:** Make the edits, state plainly that the changes are saved and they can build/run in VS. If a shader edit has a subtle GLSL-syntax risk worth flagging, describe it in text instead of building. Only build if the user explicitly asks. Relatedly, [[pace-explain-before-code]] — keep them in the loop with plain explanation, not tool churn.
