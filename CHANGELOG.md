# NMJToolBox version history

## v2.1.0

### Added — Pooling (runtime)

  - Inspector-driven object pooling built **on top of** `UnityEngine.Pool.ObjectPool<T>` rather than
    replacing it. `PoolManager` holds a list of `PrefabPool`s configured entirely in the inspector;
    `PoolSpawner` and `ReturnToPool` cover spawning and despawning with no code at all, and every
    entry point is UnityEvent-compatible.
  - Instances are positioned and parented **before** activation, so `OnEnable`, particle systems and
    physics see the correct transform on their first frame — which is not what happens if you
    activate inside `ObjectPool<T>`'s own get callback.
  - `PoolManager.Despawn` falls back to `Destroy` for objects that were never pooled, so it is safe
    to use as a blanket replacement.
  - Optional `IPoolable` (`OnSpawned`/`OnDespawned`) for state that must reset after the pool has
    finished placing the instance.
  - `PoolManagerEditor` shows live active/pooled counts per pool in Play mode, with a usage bar and
    a warning when a pool has outgrown its max size.

### Added — Batch Rename (editor)

  - Renames many objects at once in both the Hierarchy and the Project window, with a live preview.
  - `{}` token expressions: `{N}` name, `{N:i,c}`/`{S:i,c}` substring, `{C}`/`{I}` counters with
    start/step/zero-padding, `{A}`/`{AU}` letter counters, `{T}` total, `{P}` parent, `{TY}` type,
    `{E}` extension, `{U}`/`{L}`/`{TC}` casing, `{B}` beautified, `{D}` date, `{{`/`}}` literals.
    Unknown tokens are echoed into the preview instead of vanishing.
  - Find & replace with optional regex, prefix/suffix, trim, and a selection order that decides how
    counters are assigned. Natural order reproduces the Hierarchy's own top-to-bottom order by
    comparing sibling-index chains.
  - Opens on **F2 when more than one object is selected**; a single selection still gets Unity's
    inline rename. Unity's shortcut system disables both entries when two shortcuts claim one key,
    so the F2 binding ships unassigned and is enabled by
    `Tools ▸ NMJ ▸ Shortcuts ▸ Batch Rename — Take Over F2`. That takeover is lossless and reversible.
  - Scene renames collapse into one Undo step. Asset renames are not undoable —
    `AssetDatabase.RenameAsset` sits outside the Undo system.

### Added — Create Collection (editor)

  - `Ctrl+G` groups the selection under a new empty parent; `Ctrl+Shift+G` dissolves one again.
  - The group is created as a **sibling of the selection**, not at the scene root, so grouping
    objects deep in the hierarchy leaves them where they were.
  - Keeps sibling order, places the pivot at the selection's bounds centre, filters out descendants
    of already-selected objects, produces a stretched `RectTransform` for UI selections, and drops
    into inline rename. Fully undoable.

### Added — Scene view overlays (new assembly)

  - **Mini Hierarchy** — a compact, searchable scene tree inside the Scene view, prefab-stage aware,
    with expand/collapse, dimmed inactive objects, click-to-select and double-click-to-frame.
  - **Inspector** — the selected object's inspector docked in the Scene view, built on
    `InspectorElement` so custom editors and property drawers render exactly as they do in the real
    Inspector.
  - Both live in `NMJ.NMJTools.SceneView.Editor` (namespace `NMJToolBox.SceneTools`) with no
    references to the rest of the package, so they can be lifted into their own package with a
    single move. See **Splitting them out** in the README for why that is likely the right call.

---
## v2.0.0

### Breaking

  - **PlayerPrefs Manager has been removed from this package** and now ships separately as
    `com.nmj_gd.playerprefsmanager`. If you used `Elinis.PlayerPrefsManager.*`, install that
    package instead — its namespaces are now `NMJ.Prefs` and `NMJ.Prefs.Editor`.
  - Minimum Unity version raised from 2021.1.17f1 to **2021.2**. The package already used C# 9
    syntax (target-typed `new()`), which 2021.1 does not compile.

### Fixed

  - **NMJ.NMJTools.Editor.asmdef listed `UltimateEditorEnhancer` and
    `UltimateEditorEnhancer-Editor` as named references.** Unity refuses to compile an assembly
    definition whose named references cannot be resolved, so in any project without Ultimate
    Editor Enhancer installed the *entire* NMJ editor assembly silently failed to compile — every
    menu item, inspector and window in it was unavailable. Both references are gone; the package
    now has no third-party assembly references.
  - `GameObjectName.cs` was dead code: its whole body sat behind `#if UEE`, a define nothing in
    the package ever set.

### Changed

  - `GameObjectName` rewritten without Ultimate Editor Enhancer. It now adds **Rename GameObject
    To This Component** to every component's context menu (the ⋮ button in its Inspector header),
    registers an Undo step, and hides itself on `Transform`/`RectTransform` and when the name
    already matches. Unity exposes no public API for a real component *header button*, which is
    what the UEE dependency provided.
  - `README.md` rewritten as full package documentation — API tables for every public type, usage
    examples, the generated state-machine shape, and an assembly map.
  - `package.json` description no longer advertises NaughtyAttributes, EButton and EditorToolbox;
    none have been in the repository since v1.0.2.
  - `Third Party Notices.md` corrected for the same reason — it listed two components that are not
    bundled.

---
## v1.0.5
  Changes:
  
    -StateMachineGenerator.cs: Added optional UnityEvents Generator for Enter and Exit methods.
    
---  
## v1.0.4
  Changes:
  
    -SceneChanger.cs: it know loads using scenepath instead of name.
    
---  
## v1.0.3
  Changes:
  
    -Added JsonParser from string to generic [Serializable] object
    -Added String Trim Extension for start and end
    -Added AssetDatabaseExtensions ande AssetDatabaseUtilities
    -Added CustomListView, Editor only use

---
## v1.0.2
  Changes:
  
    -Deleted Hotkey Scenes Switcher from https://github.com/vimpsoft/ScenesSwitcher by Andrey Tsarkov
    -Deleted Hierarchy 2 from https://github.com/truongnguyentungduy/hierarchy-2 by truongnguyentungduy

---

## v1.0.1
 Changes:
 
  - Added Hotkey Scenes Switcher from https://github.com/vimpsoft/ScenesSwitcher by Andrey Tsarkov

---

## v1.0.1
 Changes:
 
  - Added Hierarchy 2 from https://github.com/truongnguyentungduy/hierarchy-2 by truongnguyentungduy

---

## v1.0.0
  - First Release.
  - Third Party Used:
  - NaughtyAttributes by dbrizov : https://github.com/dbrizov/NaughtyAttributes
  - EButton by Script Boy: https://assetstore.unity.com/packages/tools/utilities/ebutton-196880
  - EditorToolbox by Kevin Castejon: https://assetstore.unity.com/packages/tools/utilities/editortoolbox-198306
