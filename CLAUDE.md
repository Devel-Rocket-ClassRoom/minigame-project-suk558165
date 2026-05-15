# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project

Unity 6 project (`6000.3.14f1`) — a minigame project, currently at the scaffold stage. `Assets/Imported/` is empty; no gameplay scripts, scenes, or prefabs exist yet. Treat new work as greenfield Unity development unless files have appeared.

## Editor / build

There is no CLI build script. Open the project in Unity Hub with editor version **6000.3.14f1** (see [ProjectVersion.txt](ProjectSettings/ProjectVersion.txt)). Builds and Play-mode testing happen inside the Editor; the Unity MCP tools (`mcp__unity-mcp__*`) can drive the running Editor when one is attached.

Package dependencies are declared in [manifest.json](Packages/manifest.json) — notable: `com.unity.ai.assistant`, `com.unity.multiplayer.center`, plus the standard Unity modules. No third-party rendering pipeline package (URP/HDRP) is installed; the project uses the built-in render pipeline.

## Repository conventions

- `gitignore.txt` exists at the repo root but is **not** named `.gitignore` — Unity's generated folders (`Library/`, `Temp/`, `Logs/`, `UserSettings/`, `Obj/`, `Build/`) currently show as untracked. If committing, rename or copy to `.gitignore` first rather than committing those folders.
- `settings.json` at the repo root holds Claude Code permissions: reads under `Library/`, `Temp/`, `Logs/`, `UserSettings/`, `bin/`, `obj/`, and `*.meta`/`*.csproj`/`*.sln` files are denied. Don't try to read those — generate or inspect their source-of-truth instead (e.g., the `.asset` files in `ProjectSettings/`).
- `.meta` files are sibling metadata for every Unity asset. They must be committed alongside their asset, but Claude cannot read them due to the deny rule above; rely on the asset file itself.

## When adding code

- Place gameplay scripts under `Assets/` (create subfolders like `Assets/Scripts/`); Unity will auto-generate the `.csproj`/`.sln` on next Editor open.
- Scenes go under `Assets/Scenes/` and must be registered in `ProjectSettings/EditorBuildSettings.asset` to be included in builds.
