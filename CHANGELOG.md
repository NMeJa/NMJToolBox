# NMJToolBox version history

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
