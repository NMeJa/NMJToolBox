# NMJToolBox version history

## v4.0.0

### Breaking

  - **`SerializableDictionary<TKey, TValue>` and its drawer are removed.** Unity 6.6 serializes
    `Dictionary<,>` natively, so the wrapper is redundant there — drop the type name and keep the
    `[SerializeField]`. Note the built-in support is *opt-in* (a public dictionary is not serialized
    automatically, unlike other public fields), does not work with `[SerializeReference]`, and
    cannot nest a dictionary inside another collection. On 2022.3–6.5 there is no built-in
    equivalent: stay on v3.1.0 or vendor the old file.
  - **`ListContainer` is removed**, along with its `.uxml` and `.uss`. The entire class had been
    commented out, so it compiled to nothing while still shipping two assets and a namespace.
  - **`StandardMaterialCreator` is removed**, replaced by `MaterialSetupProfile` +
    `MaterialFromTextures` — see *Added*.
  - **`OpenWithVSCode` is removed**, replaced by `OpenInExternalEditor` — see *Added*.
  - **`InstantiateInGrid` moved from the Editor assembly to the Runtime assembly**, and its fields
    were renamed (`column`/`row` → `columns`/`rows`, `xSize`/`zSize` → `spacing`,
    `startXPos`/`startZPos` → `origin`). Its `.meta` was kept, so existing scene references survive
    the move, but serialized field values do not.
  - **`State.Update` and `State.FixedUpdate` are now virtual** rather than abstract. Overrides keep
    working; states that implemented them only to satisfy the compiler can delete them.
  - **Minimum Unity is now 2022.3.** The manifest claimed 2021.2, which was never accurate — see
    *Fixed*.

### Changed — Project Setup is now one button

  `Tools ▸ NMJ ▸ Setup ▸ Project Setup` installed one package per `Client.Add` call, which is one
  resolve and one domain reload each, and imported paid packages through Unity's interactive import
  dialog. A dozen packages meant a dozen reloads and a dozen dialogs to click through.

  - **One transaction.** Everything selected — installs *and* removals — goes through a single
    `Client.AddAndRemove`: one resolve, one domain reload, and a dependency graph solved once with
    the whole set visible to it.
  - **Silent Asset Store imports.** Cached `.unitypackage` files are imported with
    `interactive: false`, chained on the import callbacks so they run back to back. The queue lives
    in `SessionState` and survives the domain reload the transaction causes; a guard flag stops the
    reload and the deferred call from both draining it.
  - **Per-item toggles, stored in a `SetupProfile` asset** with Standard / Mobile / Prototype /
    Everything / Empty presets. A project asset rather than an editor preference, for the same
    reason `FolderTemplate` is one: the packages a project depends on are a property of the project,
    not of the machine building it. The window works against an in-memory profile until you save.
  - **Removals are first-class.** The three packages the old `.bat` stripped by hand — Visual Studio
    Editor, VS Code Editor, Visual Scripting — are toggles alongside the installs, with the reason
    stated, and go into the same transaction.
  - **The full package set from the install script is now the catalogue**, grouped into Core,
    Debugging, Authoring, Optional, Mobile and Paid, plus PlayerPrefs Manager and suggested extras
    (R3, ZString, SerializeReference Extensions, NuGetForUnity, Memory Profiler, Compilation
    Visualizer, Input System, Code Coverage, Localization, Mobile Notifications, Adaptive
    Performance) — all off by default. **ProGrids was dropped**: it never left preview and was
    discontinued; ProBuilder 5 has its own grid and snapping.

### Fixed — the OpenUPM registry write

  Adding the scoped registry was a hand-spliced JSON string that bailed out with a console message
  whenever `manifest.json` already contained *any* `scopedRegistries` block — the common case on any
  project that has ever installed one OpenUPM package.

  Unity ships no writable manifest API and no scoped-registry API (`Client` has Add, Remove and
  Resolve, and nothing that touches `scopedRegistries`), and `JsonUtility` cannot round-trip a
  manifest without dropping every key it was not told about — `lock`, `testables`, anything a future
  Unity adds. So `MiniJson.cs` is a small order-preserving JSON reader/writer for exactly this job.
  `PackageManifest` now unions scopes into an existing OpenUPM entry, leaves other registries
  untouched, and writes through a temporary file so a half-written manifest can never land.

### Fixed

  - **`SingletonBase` destroyed a duplicate and then carried on.** `Destroy` is deferred to the end
    of the frame, so nothing stopped the duplicate reaching `DontDestroyOnLoad` and `PersonalAwake`
    — a second instance ran its whole initialisation, and could mark itself `DontDestroyOnLoad`, on
    the frame it was being destroyed.
  - **`StateMachine` never called `Update` or `FixedUpdate`.** `State` declared both abstract, so
    every state was forced to implement two methods the machine had no way to invoke. Added `Tick`
    and `FixedTick`, plus `Current`, `IsIn<T>()`, `Restart()` and a `StateChanged` event. `Change`
    now ignores a self-transition, which previously replayed `Exit`/`Enter` for a state that had not
    changed.
  - **`StateMachineGenerator` emitted code that did not compile.** With *Group by phase* on it
    opened three `#region` directives and closed none; *Use Foldout on Events* emitted
    `[Foldout(...)]` from NaughtyAttributes, which the package stopped bundling — the `using` had
    been removed but the attribute usage left behind. Output also always landed in the root of
    `Assets/` with no namespace. It now takes a namespace and a folder, both remembered.
  - **`Ctrl+R` on Reset Transform collided with Unity's `Assets ▸ Refresh`.** Unity resolves a
    shortcut collision by silently disabling *both* entries, so the binding cost the refresh
    shortcut and did not work itself. Now an unbound `[Shortcut]`, like the batch-rename and
    selection-history commands. It also marks the scene dirty, which it never did.
  - **`InstantiateInGrid` was a `MonoBehaviour` in an editor-only assembly** and so could not exist
    in a build. Edit-mode building now uses `PrefabUtility.InstantiatePrefab` and one Undo step,
    rather than plain `Instantiate` producing prefab-disconnected, non-undoable clones.
  - **`SerializedProperty.objectReferenceEntityIdValue` is Unity 6 only** and was used against a
    declared 2021.2 minimum. Also, `DeleteArrayElementAtIndex` nulls an object-reference element on
    the first call and only removes it on the second, so removing a reference-typed row appeared to
    do nothing. *(Both in the drawer since removed, but the same bug is worth recording.)*
  - **`CollectionExtensions.Shuffle` constructed `new Random()` per call.** Mono seeds a
    parameterless `Random` from the system clock, so two shuffles in the same frame came out in
    lockstep. Now one shared generator and a Fisher-Yates swap, replacing repeated `List.RemoveAt`
    — which was O(n²) for no gain.
  - **`CollectionExtensions.Loop`** carried a meaningless `where T : IConvertible` constraint that
    excluded every reference type, and wrapped only one step: index 7 into a 3-element array
    returned element 0 rather than element 1, and a negative index threw.
  - **`AssetDatabaseExtensions.CreateAsset`** used `folderPath[..7]`, throwing
    `ArgumentOutOfRangeException` on any path shorter than `"Assets/"` instead of the intended
    message.
  - **`CreateCollection` created the group in the wrong scene.** `new GameObject` always lands in
    the *active* scene, so with two scenes open and the selection in the other one, a collection
    with no shared parent dragged its children across.
  - **Android App Bundle builds were named `.apk`.** They still build, then fail at upload — the
    slowest possible place to find out.
  - **`manifest.json` was re-read and re-parsed on every repaint** of the setup window. Now cached
    against the file's write time, so an external edit is still picked up.
  - **`package.json` declared `"unity": "2021.2"`**, which never compiled: the Scene view overlays
    use `ListView.selectionChanged` (2022.x, replacing `onSelectionChange`) and
    `FindAnyObjectByType` (2021.3.18+).
  - `NMJ.NMJTools.Attributes.Editor` referenced its runtime assembly **by name** while every other
    asmdef in the package used a GUID. By name, one rename produces the "references non-existent
    assemblies" error, which stops the whole assembly compiling rather than degrading.
  - Two `<see cref>` targets in `MathExtensions` pointed at `ParsingExtensions`, a type that no
    longer exists.

### Added

  - **`MaterialSetupProfile` + `MaterialFromTextures`** — `Assets ▸ NMJ ▸ Create Material From
    Textures` builds a material per selected folder from texture filename suffixes, and corrects
    each map's import type and colour space. Shader, material prefix, albedo pattern, the
    suffix → shader-property table and per-map import kinds all come from a profile asset, pre-filled
    with the URP Lit conventions.

    This replaces `StandardMaterialCreator`, which was one project's code copied into the shared
    package: a `StockAndSorcery` namespace, a hardcoded `Elinis/TCP2` shader, a `Ramp_WarmCosy`
    asset and an `Assets ▸ Elinis` menu item. Installed anywhere else it added a menu entry that
    could only ever fail with "shader not found".
  - **`OpenInExternalEditor`** — `Assets ▸ NMJ ▸ Open in External Editor` uses
    `CodeEditor.CurrentEditor`, so the file opens in the editor configured in
    `Preferences ▸ External Tools` rather than a hardcoded one, and non-code assets fall back to the
    OS default application. Plus `Assets ▸ NMJ ▸ Reveal in Explorer`, which selects the file itself
    rather than its folder.
  - **`Tools ▸ NMJ ▸ Setup ▸ Open manifest.json`**.
  - `UniTaskReturnToPool` gained the `whenParticlesFinished` option that `ReturnToPool` already had.
    Without it the UniTask version — the one the docs tell you to prefer — was missing the option
    that makes one-shot effects work, so a variable-length particle burst forced you back onto the
    `Update`-driven component this class exists to avoid.
  - `InstantiateInGrid` gained a `centred` option, `Vector3` origin (Y now works), gizmo preview of
    the cell positions, and a **Clear** button that removes only the children it created.

---

## v3.1.0

### Changed — folder scaffolding is now a wizard

  `FolderStructureCreator` was a menu item with a hardcoded `const projectName = "__Game"`, one
  `Debug.Log` per folder, and no way to vary the structure without editing the package source. It is
  now `Tools ▸ NMJ ▸ Setup ▸ Create Folder Structure` — a window with templates, live preview and a
  custom editor, in `Editor/Scaffolding/` (`NMJTools.Scaffolding`).

  - **Empty folders now survive a clone.** Git cannot track an empty directory, and Unity writes a
    folder's `.meta` beside it — so a commit carried `Art/Textures.meta` but not `Art/Textures/`,
    and on clone Unity deleted the orphaned meta. The entire scaffold evaporated for everyone but
    its author. Every folder now gets a `.gitkeep`, which git tracks and Unity ignores.
  - **Five built-in templates** (Standard, Minimal, Feature-based, 2D Game, Empty), shipped as code
    so a fresh install needs no assets. Any can be edited and saved as a `FolderTemplate` asset,
    which is the project-level setting — it lives in the repo and shows up in review, unlike an
    editor preference, which is per-user and per-machine.
  - **Preview before running**, marking new folders green and existing ones grey, with a
    confirmation dialog since folder creation cannot be undone.
  - **Optional assembly definitions**: `<Root>.Runtime` and `<Root>.Editor`, the editor one
    referencing the runtime one **by GUID**, read back after import so it cannot be mistyped. By
    name, one typo produces the "references non-existent assemblies" error, which stops the whole
    assembly compiling rather than degrading.
  - **Optional README.md** in the root, documenting the layout.
  - Target folder defaults to the current Project-window selection instead of always `Assets/`.

### Fixed

  - Folder creation is batched in `StartAssetEditing`/`StopAssetEditing` and reports one summary
    line, rather than 21 individual creations and 21 log lines (including "already exists" on every
    re-run).
  - `AssetDatabase.CreateFolder`'s return value is checked — an empty string means failure, and
    discarding it is how a silent no-op passes for success.
  - Ancestor paths are expanded and sorted shallowest-first, so a template can list only its leaves.
    This replaces the recursive create-parent-first walk, which threw on any path containing no `/`.
  - Paths are normalised: backslashes, trailing slashes, `.`/`..` segments and invalid filename
    characters no longer fail silently.

---
## v3.0.0

### Breaking — namespaces

  - Root namespace is now **`NMJTools`** (was `NMJToolBox`), and every file's namespace mirrors its
    folder under the assembly root, per the usual C# convention. Consumers must update their
    `using` directives; the package and repository names are unchanged.
  - Full map is documented under **Namespaces** in the README.

### Fixed — namespace collisions

  These are why several folders were renamed. A namespace segment under `NMJTools` shadows any Unity
  namespace or class of the same name for *every* namespace nested beneath it, because C# resolves
  enclosing namespaces before `using` directives — so one badly-named folder poisons the whole
  package, not just its own files.

  - **`Runtime/Gizmos/` → `Runtime/Debugging/`.** `NMJTools.Gizmos` made `UnityEngine.Gizmos`
    unreachable package-wide. `GizmoUtils.cs` had been forced to write `UnityEngine.Gizmos.DrawLine`
    on 25 lines, and `PoolSpawner.cs` on 4 more — despite living in a different namespace and never
    referencing `GizmoUtils`. Both are back to plain `Gizmos.`.
  - **`Runtime/Serialization/` → `Runtime/Serializables/`.** Same shadowing of
    `UnityEngine.Serialization`; `SceneChanger` no longer needs
    `[UnityEngine.Serialization.FormerlySerializedAs]`.
  - **`Runtime/Patterns/StateMachine/` flattened into `Runtime/Patterns/`.** A folder named after a
    class inside it made `StateMachine` resolve to the namespace from anywhere else — the same
    defect as the `Elinis.PlayerPrefsManager` collision fixed in the PlayerPrefs split.
  - **`Editor/Build/` → `Editor/BuildTools/`**, pre-empting a shadow of `UnityEditor.Build`, which
    `BuildTools.cs` and `PackageDetector.cs` both import for `NamedBuildTarget`.
  - Straggler namespaces cleaned up: `NMJToolBox` in `CreateCollection.cs`, `CustomListView` in
    `ListContainer.cs`, and the placeholder `NMJTools.WhyEvenUsing` on the singleton.
  - `StateMachineGenerator` no longer emits `using NMJToolBox;` into generated files — a using for a
    namespace that no longer exists, for a `Foldout` attribute this package has not shipped since
    NaughtyAttributes was removed in v1.0.2.
  - Broken `<see cref="GeneratorInstance.Setup.PackageDetector"/>` crefs in both integration
    assemblies replaced — `GeneratorInstance` never existed, and the target lives in an editor
    assembly those runtime assemblies do not reference.

### Changed — file organisation

  - `Beautify.cs` merged into `StringExtensions.cs`. Two static classes of string extensions in one
    namespace were indistinguishable at every call site — extension resolution ignores the declaring
    type — so the split only ever cost a file to open.
  - `ParsingExtensions.cs` dispersed: `ToBoolean(float)` moved to `MathExtensions` (it is scalar
    maths, not parsing), and `ParseJson<T>` became **`JsonExtensions.cs`**. Kept separate rather than
    folded into `StringExtensions` because it is the only code touching Newtonsoft, and therefore the
    only reason the package has a dependency at all — isolated, it can later move into a
    `defineConstraints`-gated assembly like the UniTask and LitMotion integrations.
  - `SingletonPattern<T>` renamed to **`Singleton<T>`** (`SingletonPattern.cs` → `Singleton.cs`).
  - `Editor.SceneView/` → `Editor.SceneTools/`, and its assembly `NMJ.NMJTools.SceneView.Editor` →
    `NMJ.NMJTools.SceneTools.Editor`, so folder, assembly and namespace agree.
  - `Editor/EditorOnlyListView/` → `Editor/Controls/`; `Runtime/Tools/` → `Runtime/Scenes/`;
    integration assemblies moved to `Runtime.Integrations.{UniTask,LitMotion}/`.
  - Every asmdef's `rootNamespace` now matches what its folder produces, so Rider generates correct
    namespaces for new files instead of reproducing the old ones.

---
## v2.2.0

### Added — optional package integrations

  - `Tools ▸ NMJ ▸ Setup ▸ Project Setup`: installs the standard package set. Free and OpenUPM
    packages install in one click (the OpenUPM scoped registry is written into `manifest.json`
    first, merging scopes and refusing rather than clobbering an existing `scopedRegistries` block).
  - **Paid Asset Store packages cannot be installed by script** — Unity checks entitlement
    server-side against the signed-in account. The window instead looks for a copy already in the
    per-user Asset Store download cache and imports that, falling back to opening the store page.
    That is the honest ceiling on "one-button install".
  - `PackageDetector` scans loaded assemblies after every recompile and keeps the `NMJ_*` define
    symbols in sync. Assembly scanning rather than asmdef `versionDefines`, because it covers UPM
    packages and Asset Store content dropped into `Assets/` identically — the latter is not a
    package and has no version to bind to. Defines are only written when the set actually changes,
    which is what stops the write→recompile→detect cycle from looping.
  - **UniTask** (`NMJ_UNITASK`): `UniTaskReturnToPool` — a drop-in for `ReturnToPool` with no
    per-instance `Update` at all — plus `DespawnAfterAsync`, `SpawnForAsync`, and awaitable
    `SceneChanger.LoadAsync` / `LoadAdditiveAsync` / `UnloadAsync`.
  - **LitMotion** (`NMJ_LITMOTION`): `MotionReturnToPool` for tweened pooled spawn/despawn, and
    `PoolMotionExtensions.SpawnWithPop`.
  - **Asset Usage Detector** (`NMJ_ASSETUSAGEDETECTOR`): `Assets ▸ NMJ ▸ Find Usages`. Reached via
    `ExecuteMenuItem` rather than an assembly reference, so a renamed assembly degrades to a
    warning instead of taking the whole editor assembly down.
  - **Odin Inspector**: NMJ's own `[ReadOnly]`/`[Required]`/`[Button]` now switch **off**
    automatically while Odin is present. Odin declares all three names and also claims the default
    MonoBehaviour inspector, so the two cannot coexist without ambiguity. A *Force NMJ attributes
    alongside Odin* override exists for anyone willing to fully qualify them.
  - Every integration lives in its own `defineConstraints`-gated assembly. The core assemblies
    reference none of these packages, so **NMJ ToolBox still has no hard third-party dependency** —
    an uninstalled package means the integration assembly is skipped, not a broken build.

### Added — runtime

  - `TransformExtensions`, `GameObjectExtensions`, `VectorExtensions`, `MathExtensions` —
    `GetOrAddComponent`, `DestroyChildren`, `SetLayerRecursively`, `With(x:,y:,z:)`, `Remap`,
    `Wrap`, `SnapToGrid` and the rest.
  - `SerializableDictionary<TKey,TValue>` with a paired-row property drawer. Unity's serializer has
    never supported `Dictionary<,>`; marking one `[SerializeField]` does nothing, silently. The
    drawer keeps duplicate and empty keys visible while editing instead of discarding them.
  - `GizmoUtils` — arrows, arcs, view cones, and a wire capsule matching `CapsuleCollider`, none of
    which `Gizmos` provides. `DrawArrow` clamps its head to half the shaft so a short arrow cannot
    render inverted, clamps `headAngle` below the angle where `tan` diverges, and closes the head
    into a pyramid so it stays readable edge-on. The direction-based form is named `DrawArrowRay`
    rather than being an overload: both would start (Vector3, Vector3, float), so a three-argument
    call could not be distinguished by the compiler or by a reader.

### Added — editor

  - `[SceneReference]` turns any `string` field into a scene picker that stores the asset path,
    warns when the scene is missing from Build Settings, and offers a one-click **Add to Build**.
  - Prefab tools: **Replace With Prefab** (keeps transform, parent, sibling index and active state),
    **Select Instances In Open Scenes**, **Find Missing Scripts** with bulk removal.
  - Build tools: version increment (including `Android.bundleVersionCode` and the iOS build number,
    which stores require and a display-version bump alone misses), optional auto-increment as a
    pre-build step, timestamped builds, and quick-open folder commands.
  - Scene view **Selection History** overlay — back/forward through selections, with a static
    recorder that survives domain reloads. Its shortcuts ship unbound: a colliding shortcut leaves
    *both* disabled, and no key here is free on every platform.
  - Scene view **Isolate** overlay, driving Unity's `SceneVisibilityManager` rather than toggling
    `SetActive` — so it cannot dirty a scene, break a prefab override, or leak into a build.

### Changed

  - `SceneChanger` now uses `[SceneReference]` and **`SceneChangerEditor` has been removed**; the
    attribute provides the same picker and Build Settings button for any string field. The stored
    value is unchanged, so existing prefabs and scenes keep working.

---
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
