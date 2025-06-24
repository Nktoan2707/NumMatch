# Unity Naming Convention/Style

# Table of Contents

-   [Asset Naming](#asset-naming)
    -   [Folders](#folders)
    -   [Source code](#source-code)
    -   [Non-code assets](#non-code-assets)
-   [Directory/File structure](#directory-file-structure)
    -   [Assets](#assets)
    -   [Scripts](#scripts)
-   [References](#references)

# Asset Naming

First of all, no\ spaces\ on file or directory names.

## Folders

`PascalCase`

Prefer a deep folder structure over having long asset names.

Directory names should be as concise as possible, prefer one or two words. If a directory name is too long, it probably makes sense to split it into sub directories.

Try to have only one file type per folder. Use `Textures/Trees`, `Models/Trees` and not `Trees/Textures`, `Trees/Models`. That way its easy to set up root directories for the different software involved, for example, Substance Painter would always be set to save to the Textures directory.

If your project contains multiple environments or art sets, use the asset type for the parent directory: `Trees/Jungle`, `Trees/City` not `Jungle/Trees`, `City/Trees`. Since it makes it easier to compare similar assets from different art sets to ensure continuity across art sets.

### Debug Folders

`[PascalCase]`

This signifies that the folder only contains assets that are not ready for production. For example, having an `[Assets]` and `Assets` folder.

## Source Code

Use the naming convention of the programming language. For C# and shader files use `PascalCase`, as per C# convention.

## Non-Code Assets

`Snake_Case`

Use `Tree_Small` not `Small_Tree`. While the latter sound better in English, it is much more effective to group all tree objects together instead of all small objects.

Use `Weapon_MiniGun` instead of `Weapon_Gun_Mini`.

Avoid this if possible, for example, `Vehicles_FighterJet` should be `Vehicles_Jet_Fighter` if you plan to have multiple types of jets.

Prefer using descriptive suffixes instead of iterative: `Vehicle_Truck_Damaged` not `Vehicle_Truck_01`. If using numbers as a suffix, always use 2 digits. And **do not** use it as a versioning system! Use `git` or something similar.

### Persistent/Important GameObjects

`_Snake_Case`

Use a leading underscore to make object instances that are not specific to the current scene stand out.

### Debug Objects

`[SNAKE_CASE]`

Enclose objects that are only being used for debugging/testing and are not part of the release with brackets.

# Directory/File Structure

```
Root
+---Assets
+---Build
\---Tools           # Programs to aid development: compilers, asset managers etc.
```

## Assets

`WIP, PLEASE REFER TO THE REAL FOLDER`

```
Assets
+---Art
|   +---Materials
|   +---Models      # FBX and BLEND files
|   +---Textures    # PNG files
+---Audio
|   +---Music
|   \---Sound       # Samples and sound effects
+---Code
|   +---Scripts     # C# scripts
|   \---Shaders     # Shader files and shader graphs
+---Docs            # Wiki, concept art, marketing material
+---Level           # Anything related to game design in Unity
|   +---Prefabs
|   +---Scenes
|   \---UI
\---Resources       # Configuration files, localization text and other user files.
```

## Scripts

Use namespaces that match your directory structure.

A Framework directory is great for having code that can be reused across projects.

The Scripts folder varies depending on the project, however, `Environment`, `Framework`, `Tools` and `UI` should be consistent across projects.

```
Scripts
+---Environment
+---Framework
+---NPC
+---Player
+---Tools
\---UI
```

# References

-   [Unity Style Guide](https://github.com/stillwwater/UnityStyleGuide)

---
