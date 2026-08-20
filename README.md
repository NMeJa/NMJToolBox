# NMJ ToolBox

Helper scripts, runtime extensions and Editor tooling for Unity.

Install via **Package Manager → Add package from git URL…**

```
https://github.com/NMeJa/NMJToolBox.git
```

* **Unity:** 2021.2 or newer (uses C# 9 syntax: target-typed `new()`, range operators, switch expressions)
* **Dependencies:** none. `JsonExtensions.ParseJson<T>` needs `com.unity.nuget.newtonsoft-json`, but it lives in an opt-in assembly — see [Optional package integrations](#optional-package-integrations)
* **License:** MIT

> The **PlayerPrefs Manager** that used to live in this package now ships separately —
> see [Related packages](#related-packages).

---

## Contents

| Area | Assembly | Path |
| --- | --- | --- |
| Runtime | `NMJ.NMJTools.Runtime` | `NMJToolBox/NMJ/Runtime/` |
| Editor | `NMJ.NMJTools.Editor` | `NMJToolBox/NMJ/Editor/` |
| Scene view overlays | `NMJ.NMJTools.SceneTools.Editor` | `NMJToolBox/NMJ/Editor.SceneTools/` |
| Inspector attributes *(opt-in)* | `NMJ.NMJTools.Attributes` (+ `.Editor`) | `NMJToolBox/NMJ/Runtime.Attributes/`, `Editor.Attributes/` |
| UniTask integration *(opt-in)* | `NMJ.NMJTools.UniTask` | `NMJToolBox/NMJ/Runtime.Integrations.UniTask/` |
| LitMotion integration *(opt-in)* | `NMJ.NMJTools.LitMotion` | `NMJToolBox/NMJ/Runtime.Integrations.LitMotion/` |
| Newtonsoft integration *(opt-in)* | `NMJ.NMJTools.Newtonsoft` | `NMJToolBox/NMJ/Runtime.Integrations.Newtonsoft/` |

The *opt-in* assemblies are gated by `defineConstraints` and compile only when the matching package
is present — see [Optional package integrations](#optional-package-integrations).

### Namespaces

Namespaces mirror folders under each assembly root, per the usual C# convention:

| Folder | Namespace | | Folder | Namespace |
| --- | --- | --- | --- | --- |
| `Runtime/Extensions` | `NMJTools.Extensions` | | `Editor/BatchRename` | `NMJTools.BatchRename` |
| `Runtime/Pooling` | `NMJTools.Pooling` | | `Editor/BuildTools` | `NMJTools.BuildTools` |
| `Runtime/Patterns` | `NMJTools.Patterns` | | `Editor/Controls` | `NMJTools.Controls` |
| `Runtime/Attributes` | `NMJTools.Attributes` | | `Editor/Drawers` | `NMJTools.Drawers` |
| `Runtime/Debugging` | `NMJTools.Debugging` | | `Editor/Hierarchy` | `NMJTools.Hierarchy` |
| `Runtime/Serializables` | `NMJTools.Serializables` | | `Editor/Prefabs` | `NMJTools.Prefabs` |
| `Runtime/Scenes` | `NMJTools.Scenes` | | `Editor/Setup` | `NMJTools.Setup` |

> **Folder names are constrained by that convention.** A namespace segment under `NMJTools`
> shadows any Unity namespace or class of the same name, for *every* namespace nested beneath it —
> C# resolves enclosing namespaces before `using` directives. `Runtime/Gizmos/` made
> `UnityEngine.Gizmos` unreachable package-wide, forcing `UnityEngine.Gizmos.DrawLine(...)` even in
> files that had nothing to do with it. It is now `Runtime/Debugging/`.
>
> Segments to avoid for the same reason: **Gizmos · Editor · Serialization · Build · Rendering ·
> Events · Audio · Video · UI · Animations · Playables · Profiling · Assertions · Scripting ·
> SceneManagement · Search · Experimental · Compilation · Tilemaps · Pool**. `Editor` is the worst
> of them — it would break every `: Editor` and `[CustomEditor]` class in the package.
>
> The same applies to your own types: a folder must not be named after a class inside it.
> `Runtime/Patterns/StateMachine/` containing `class StateMachine` made the class unreachable by
> its bare name from anywhere else.

### Menu items

| Menu path | Shortcut | Script |
| --- | --- | --- |
| `Tools ▸ NMJ ▸ Reset Transform` | `Ctrl+R` | `GameplayTools` |
| `Tools ▸ NMJ ▸ Create Custom Folder Structure` | | `FolderStructureCreator` |
| `Tools ▸ NMJ ▸ StateMachineGenerator` | | `StateMachineGenerator` |
| `Tools ▸ NMJ ▸ Batch Rename` | `Ctrl+Shift+R` | `BatchRenameWindow` |
| `Tools ▸ NMJ ▸ Shortcuts ▸ Batch Rename — Take Over F2` | | `BatchRenameShortcut` |
| `Assets ▸ NMJ ▸ Batch Rename` | | `BatchRenameWindow` |
| `GameObject ▸ NMJ ▸ Create Collection` | `Ctrl+G` | `CreateCollection` |
| `GameObject ▸ NMJ ▸ Dissolve Collection` | `Ctrl+Shift+G` | `CreateCollection` |
| `GameObject ▸ NMJ ▸ Pool Manager` | | `PoolManagerEditor` |
| `GameObject ▸ NMJ ▸ Replace With Prefab…` | | `PrefabTools` |
| `Assets ▸ NMJ ▸ Select Instances In Open Scenes` | | `PrefabTools` |
| `Assets ▸ NMJ ▸ Find Usages` | | `AssetUsageBridge` |
| `Tools ▸ NMJ ▸ Find Missing Scripts In Open Scenes` | | `PrefabTools` |
| `Tools ▸ NMJ ▸ Setup ▸ Project Setup` | | `ProjectSetupWindow` |
| `Tools ▸ NMJ ▸ Setup ▸ Re-scan Packages` | | `PackageDetector` |
| `Tools ▸ NMJ ▸ Build ▸ Build Timestamped` | `Ctrl+Shift+B` | `BuildTools` |
| `Tools ▸ NMJ ▸ Build ▸ Increment … Version` | | `BuildTools` |
| `Tools ▸ NMJ ▸ Open ▸ …` | | `BuildTools` |
| Component ⋮ ▸ `Rename GameObject To This Component` | | `GameObjectName` |
| Scene view ⋮ ▸ `Mini Hierarchy` / `Inspector` / `Selection History` / `Isolate` | `` ` `` | Scene view overlays |

---

# Runtime

## `CollectionExtensions`

Extension methods for `IEnumerable<T>`, `IList<T>`, `T[]`, `List<T>`, `Queue<T>` and `IDictionary<,>`.

| Method | Description |
| --- | --- |
| `IsEmpty<T>(this IEnumerable<T>)` | `true` when the source is `null` **or** has no elements. |
| `DistinctBy<T,TKey>(this IEnumerable<T>, Func<T,TKey>)` | Distinct by a projected key, keeping the first of each group. |
| `Shuffle<T>(this IEnumerable<T>, bool canBeNull = false)` | Returns a shuffled copy (uses `System.Random`). Throws on an empty source unless `canBeNull` is set. |
| `Shuffle<T>(this IEnumerable<T>, int seed, bool canBeNull = false)` | Same, deterministic for a given seed. |
| `Shuffle<T>(this IList<T>)` | Fisher–Yates **in place**, using `UnityEngine.Random`. |
| `GetRandomElement<T>(this IList<T>)` / `(this T[])` | Random element. |
| `RandomElement<T>(this IEnumerable<T>)` | Random element; throws on an empty source (Editor only). |
| `GetRandomValue<TKey,TVal>(this IDictionary<TKey,TVal>)` | Random value. |
| `GetRandomKey<TKey,TVal>(this IDictionary<TKey,TVal>)` | Random key. |
| `GetRandomKeyValuePair<TKey,TVal>(this IDictionary<TKey,TVal>)` | Random pair (key and value are drawn from the same index). |
| `Pop<T>(this List<T>)` | Removes and returns the **last** element. |
| `PopAt<T>(this List<T>, int index)` | Removes and returns the element at `index`. |
| `Dequeue<T>(this List<T>)` | Removes and returns the **first** element. |
| `Loop<T>(this T[], int index)` | Returns `element[index]`, wrapping to `0` when `index >= Length`. |
| `DequeueChunk<T>(this Queue<T>, int chunkSize)` | Lazily dequeues up to `chunkSize` items. |

```csharp
using NMJTools.Extensions;

var deck = new List<Card>(allCards);
deck.Shuffle();                       // in place
var one = deck.GetRandomElement();
var top = deck.Pop();

if (deck.IsEmpty()) Reshuffle();
```

## `StringExtensions`

| Method | Description |
| --- | --- |
| `TrimStart(this string, string wordToRemove)` | Removes `wordToRemove` from the start, if present. |
| `TrimEnd(this string, string wordToRemove)` | Removes `wordToRemove` from the end, if present. |
| `ReplaceUnderscoresWithSpaces()` | `"player_healthBar"` → `"player healthBar"` |
| `SplitCamelCase()` | `"player_healthBar"` → `"player_health Bar"` |
| `CapitalizeFirstLetter()` | Title-cases **every** word (invariant culture) |
| `RemoveExcessWhitespace()` | Collapses runs of whitespace to one space |
| `Beautify()` | All four of the above, in order → `"Player Health Bar"` |

All methods return `string.Empty` for `null`/empty input.

> `TrimStart`/`TrimEnd` are *string* overloads of BCL names that normally take `char[]`. The
> compiler picks the extension only because no instance overload accepts a `string`.

## `JsonExtensions` *(opt-in)*

| Method | Description |
| --- | --- |
| `ParseJson<T>(this string json)` | `JsonConvert.DeserializeObject<T>`, rethrowing with the offending JSON in the message. |

Lives in its own `defineConstraints`-gated assembly (`NMJ.NMJTools.Newtonsoft`, define
`NMJ_NEWTONSOFT`) — the same pattern as the UniTask and LitMotion integrations.

That is what lets **NMJ ToolBox declare no package dependencies at all**. Without
`com.unity.nuget.newtonsoft-json` installed the define is never set, the assembly is skipped, and
`ParseJson<T>` is simply absent; nothing else in the package notices. Install it from
`Tools ▸ NMJ ▸ Setup ▸ Project Setup` to switch it on.

## `UIToolkitExtensions`

| Method | Description |
| --- | --- |
| `DisplayVisibility(this IStyle, DisplayStyle)` | Sets `style.display`. |
| `ChangeBackgroundColor(this IStyle, Color)` | Sets `style.backgroundColor`. |

## `SingletonBase<T>`

`[DefaultExecutionOrder(-10)]` abstract `MonoBehaviour` singleton base.

```csharp
public class AudioManager : SingletonBase<AudioManager>
{
    protected override void PersonalAwake() { /* your Awake code */ }
}

AudioManager.Singleton.Play(clip);
```

* `Singleton` — static accessor. If no instance exists it runs `FindObjectOfType<T>()`, and failing
  that creates a new `GameObject` named after the type and adds the component.
* `dontDestroy` — serialized bool; when set, calls `DontDestroyOnLoad` in `Awake`.
* `PersonalAwake()` — override this instead of `Awake` (the base `Awake` is private).

## `State` / `StateMachine`

A minimal, allocation-free state machine.

```csharp
[Serializable]
public class IdleState : State
{
    public override void Enter()       { }
    public override void Exit()        { }
    public override void Update()      { }
    public override void FixedUpdate() { }
}

var fsm = new StateMachine(new IdleState());   // calls Enter() immediately
fsm.Change(new RunState());                    // Exit() old → Enter() new
```

`State` exposes a `protected MonoBehaviour user` field for the owning behaviour.
`StateMachine` only drives `Enter`/`Exit` — pump `Update`/`FixedUpdate` yourself if you need them.

## Extensions

`TransformExtensions` · `Children()`, `Descendants()`, `DestroyChildren()` (edit-mode safe),
`ResetLocal()`, `SetX/Y/Z`, `Translate`, `DirectionTo`, `SqrDistanceTo`, `GetPath()`,
`GetComponentInAncestors<T>()`.

`GameObjectExtensions` · `GetOrAddComponent<T>()`, `HasComponent<T>()`, `SetLayerRecursively`
(by index or name), `IsInLayerMask`, `SafeDestroy()`, `WithActive()`.

`VectorExtensions` · `With(x:, y:, z:)` and `Add(...)` for partial copies, `Flatten()`,
`FlatDirection()`, `ToXZ()`, `Multiply`/`Divide`, `Rotate(degrees)`, `IsCloserThan` (squared, no
square root), `Abs()`, `SnapToGrid()`, `Bounds.RandomPointInside()`.

`MathExtensions` · `Remap` / `RemapClamped` / `Normalize`, `Approximately(other, tolerance)`,
`Wrap` (int and float, negatives handled — unlike `%`), `IsBetween`, `PercentBetween`, `SnapTo`,
`ToBoolean(useAbsolute, useInverse, threshold)`.

```csharp
transform.position = transform.position.With(y: 0f);
var t = health.Remap(0f, maxHealth, 0f, 1f);
var rb = gameObject.GetOrAddComponent<Rigidbody>();
```

## `SerializableDictionary<TKey, TValue>`

A real `Dictionary` Unity can serialize. Unity's serializer has never supported `Dictionary<,>` —
marking one `[SerializeField]` does nothing, silently, and the field resets on every domain reload.

```csharp
[SerializeField] private SerializableDictionary<string, int> scores = new();
```

Unity 2020.1+ serializes generic fields directly, so no subclass is needed. The custom drawer shows
paired key/value rows with add/remove, and tints duplicate keys red — mid-edit the backing lists
legitimately hold duplicates that a dictionary cannot, so those rows stay visible and fixable rather
than silently discarded. Only the first occurrence of a duplicate key survives into the dictionary.

## `[SceneReference]`

Turns any `string` field into a scene picker.

```csharp
[SerializeField, SceneReference] private string mainMenu;
...
SceneManager.LoadScene(mainMenu);
```

Stores the scene's asset path — which is what `SceneManager` wants — while editing it as a
`SceneAsset`. The drawer rewrites the path when the asset moves, warns when the scene is missing
from Build Settings, and offers a one-click **Add to Build**.

> This replaced `SceneChangerEditor`, which hand-rolled the same logic for one component.
> `SceneChanger` is now a plain field with no custom inspector, and the stored value is unchanged.

## Pooling

An inspector-driven object pool. It does **not** reimplement pooling — `UnityEngine.Pool.ObjectPool<T>`
has shipped with Unity since 2021.1 and does the hard part. What this adds is everything that pool
leaves to you: instantiation, activation ordering, parenting, lifecycle callbacks, and a setup that
a designer can drive without opening a script.

### Setting one up (no code)

1. `GameObject ▸ NMJ ▸ Pool Manager`
2. Add entries to its **Pools** list — a prefab, a prewarm count, a max size.
3. Add a **Pool Spawner** to whatever should spawn, point it at the pool id.
4. Add a **Return To Pool** to the prefab so instances come back on their own.

Nothing else is required. In Play mode the Pool Manager inspector shows live active/pooled counts
per pool with a usage bar, and warns when a pool has grown past its max size — so prewarm and max
values can be set from evidence rather than guesswork.

### `PoolManager`

| Member | Description |
| --- | --- |
| `Pools` | The configured `PrefabPool` list. |
| `GetPool(string id)` / `GetPool(GameObject prefab)` | Look up a pool. The prefab overload creates one on demand when **Auto Create Missing Pools** is on. |
| `PoolManager.Spawn(id \| prefab, position, rotation, parent)` | Static. Drop-in replacement for `Instantiate`. |
| `PoolManager.Despawn(GameObject)` | Static. Drop-in replacement for `Destroy` — falls back to `Destroy` for objects that were never pooled, so it is safe to use everywhere. |
| `Don't Destroy On Load` | Keeps the manager and its pools across scene loads. |

```csharp
using NMJTools.Pooling;

var bullet = PoolManager.Spawn("Bullet", muzzle.position, muzzle.rotation);
// ... later
PoolManager.Despawn(bullet);
```

### `PrefabPool`

Serializable, so it is configured entirely in the inspector: **Id** (defaults to the prefab name),
**Prefab**, **Prewarm**, **Max Size**, **Collection Check**. Exposes `CountActive`, `CountInactive`
and `CountAll`.

Instances are positioned and parented *before* being activated, so `OnEnable`, particle systems and
physics all see the correct transform on their first frame — which is not what you get if you
activate inside `ObjectPool<T>`'s own get callback.

### `PoolSpawner`

Spawning with no code. Every entry point is UnityEvent-compatible.

| Field | Description |
| --- | --- |
| Pool Id / Prefab | What to spawn. Prefab is used when the id is blank. |
| Spawn At | `ThisTransform`, `SpawnPoint` or `WorldOrigin`. |
| Offset / Random Spread | Local-space offset, plus optional per-axis randomisation. |
| Parent To Spawn Point | Off for projectiles and impact effects, on for attached objects. |
| Spawn On Enable / Repeat Interval | Fire-and-forget emitters. |
| `On Spawned` | UnityEvent receiving the new instance. |

`Spawn()` and `SpawnMany(int)` are public — wire them to a Button, an Animation Event or a
Timeline signal. The spawn point and spread box draw as gizmos when selected.

### `ReturnToPool`

The despawn half. Put it on the prefab: return after a **Delay**, optionally on **Unscaled Time**,
and/or **When Particles Finished** for one-shot effects of varying length. The timer restarts on
every spawn, which is why `Destroy(go, t)` in `Start` stops working the moment an object is pooled.

### `IPoolable`

Optional. Implement `OnSpawned()` / `OnDespawned()` on any component of the prefab (children
included) when `OnEnable` is the wrong moment because you need to run *after* the pool has
positioned and parented the instance. Resolved once at creation and cached.

## `GizmoUtils`

The shapes `Gizmos` leaves out. Call from `OnDrawGizmos`; they respect `Gizmos.color` and
`Gizmos.matrix` like the built-ins.

`DrawArrow` · `DrawCircle` · `DrawWireSphere` · `DrawWireCapsule` (matches a `CapsuleCollider`,
which `Gizmos` cannot draw) · `DrawArc` · `DrawViewCone` (FOV / detection ranges) · `DrawCross` ·
`DrawBounds` · `DrawPath` · `DrawLabel` (editor-only, compiles away in a build).

## Optional package integrations

Some classes gain extra behaviour when a package you already use is in the project. Detection and
the matching define symbols are automatic — see [Project setup](#project-setup).

| Package | Define | What lights up |
| --- | --- | --- |
| **UniTask** | `NMJ_UNITASK` | `UniTaskReturnToPool`, `GameObject.DespawnAfterAsync()`, `SpawnForAsync()`, awaitable `SceneChanger.LoadAsync()` / `LoadAdditiveAsync()` / `UnloadAsync()` |
| **LitMotion** | `NMJ_LITMOTION` | `MotionReturnToPool` (tweened spawn/despawn for pooled objects), `PoolMotionExtensions.SpawnWithPop()` |
| **Odin Inspector** | `ODIN_INSPECTOR` | Switches NMJ's own `[ReadOnly]` / `[Required]` / `[Button]` **off**, so the two cannot collide |
| **Newtonsoft Json** | `NMJ_NEWTONSOFT` | `JsonExtensions.ParseJson<T>()` |
| **Asset Usage Detector** | `NMJ_ASSETUSAGEDETECTOR` | `Assets ▸ NMJ ▸ Find Usages` |

Each integration lives in its own assembly gated by `defineConstraints`. The core assemblies never
reference these packages, so **NMJ ToolBox has no hard dependency on any of them** — an uninstalled
package means the integration assembly is skipped, not that the build breaks.

`UniTaskReturnToPool` is worth calling out: the built-in `ReturnToPool` polls a timer in `Update`,
which is fine for a handful of objects and wasteful for hundreds. The UniTask version runs off one
shared PlayerLoop hook, so the per-instance cost is zero.

## `SceneChanger`

`MonoBehaviour` that stores a scene by **path** and exposes UnityEvent-friendly methods.
Pair it with the custom inspector (below) to pick the scene as an asset.

| Member | Description |
| --- | --- |
| `string ScenePath { get; set; }` | The serialized scene path (`Assets/…/Foo.unity`). |
| `bool IsActiveScene` | `true` when the active scene's path matches. |
| `_LoadScene()` | `SceneManager.LoadScene(scenePath)` |
| `_LoadScene(string scene)` | Load an arbitrary scene by name or path. |
| `_LoadParallelScene()` | Async additive load. |
| `_LoadParallelScene(LoadSceneMode mode)` | Async load with an explicit mode. |
| `_UnLoadParallelScene()` | Async unload (`UnloadAllEmbeddedSceneObjects`). |

The serialized `sceneName` field was renamed to `scenePath` and carries a `[FormerlySerializedAs]`,
so existing prefabs/scenes keep their value.

---

# Editor

## `SceneChangerEditor`

Custom inspector for `SceneChanger`:

* Drag a `SceneAsset` in — the read-only `scenePath` field is filled from `AssetDatabase.GetAssetPath`.
* **Add Scene To BuildSettings** — appends the scene to `EditorBuildSettings.scenes` if not already there.

## `GameObjectName`

Adds **Rename GameObject To This Component** to every component's context menu (the ⋮ / hamburger
button in its Inspector header). It renames the host GameObject to the beautified type name —
a `PlayerHealthBar` component renames its object to `"Player Health Bar"`.

* Registers an Undo step and flags the object dirty.
* Hidden on `Transform`/`RectTransform`, and on components whose name already matches.
* `GameObjectName.Rename(Component)` is public if you want to call it from your own tooling.

> Unity exposes no public API for adding a button to an arbitrary component's *header row*; the
> context menu is the supported equivalent. (An earlier version of this script used Ultimate Editor
> Enhancer for a real header button — that dependency has been removed.)

## Batch Rename

Renames many objects at once, in both the **Hierarchy** and the **Project** window.

Press **F2 with more than one object selected** to open it. With a single object selected, F2 still
starts Unity's own inline rename — the batch window only appears when batching is what you actually
asked for. `Ctrl+Shift+R`, `Tools ▸ NMJ ▸ Batch Rename` and the `GameObject`/`Assets` context menus
open the same window.

> **Enabling F2 is one click.** Unity already binds F2 to `Main Menu/Edit/Rename`, and its shortcut
> system disables *both* entries when two shortcuts claim one key — a conflict an attribute cannot
> resolve. So the binding ships unassigned; run `Tools ▸ NMJ ▸ Shortcuts ▸ Batch Rename — Take Over F2`
> once. The takeover is lossless: for a single selection the handler simply invokes `Edit/Rename`, so
> ordinary F2 behaves exactly as before. `Batch Rename — Restore Unity F2` reverses it.

### Pattern tokens

Anything in `{}` is a token; everything else is literal text. An unknown token is echoed back into
the preview verbatim rather than vanishing, so typos are visible instead of silent.

| Token | Arguments | Result |
| --- | --- | --- |
| `{N}` | — | Original name |
| `{N:i}` `{N:i,c}` | index, count | Substring. Negative `i` counts from the end |
| `{S:i,c}` | index, count | Same as `{N:i,c}` |
| `{C}` | start, step, pad | Counter, starts at 1 |
| `{I}` | start, step, pad | Index, starts at 0 |
| `{A}` `{AU}` | start | Letter counter — `a, b, … z, aa`; `AU` uppercases |
| `{T}` | — | Total items in the batch |
| `{P}` | — | Parent GameObject / containing folder |
| `{TY}` | — | Type name — asset type, or first non-Transform component |
| `{E}` | — | File extension (assets only) |
| `{U}` `{L}` `{TC}` | — | Upper / lower / Title Case |
| `{B}` | — | Beautified — `my_healthBar` → `My Health Bar` (uses `StringExtensions.Beautify`) |
| `{D}` | format | Date, default `yyyy-MM-dd` |
| `{{` `}}` | — | A literal `{` or `}` |

```
{N}_{C:1,1,2}        Crate      →  Crate_01, Crate_02, Crate_03
Enemy_{TY}_{I}       (mixed)    →  Enemy_Rigidbody_0, Enemy_Collider_1
{P}_{A}              Level/Cube →  Level_a, Level_b
{N:0,4}_{C:10,10}    Barrel     →  Barr_10, Barr_20, Barr_30
```

### Other controls

**Find & Replace** with optional regex and case sensitivity · **Prefix** / **Suffix** ·
**Trim start** / **Trim end** by character count · **Order** (Natural, Alphabetical,
Alphabetical descending, Selection order) which decides how counters are assigned.

Operations compose in a fixed order — **trim → find/replace → pattern → prefix/suffix** — and `{N}`
sees the result of the steps before it, so a pattern can build on a replace rather than fight it.

Natural order reproduces exactly the top-to-bottom order the Hierarchy draws, by comparing the chain
of sibling indices from root to leaf.

The live preview marks changed names green and colliding names orange. Renaming is blocked outright
only for empty names or characters a filename cannot contain.

> Scene-object renames go through `Undo` as a single collapsed step. **Asset renames do not** —
> `AssetDatabase.RenameAsset` is outside Unity's undo system entirely.

## Create Collection

The grouping command Unity has never shipped.

| Command | Shortcut | Behaviour |
| --- | --- | --- |
| `GameObject ▸ NMJ ▸ Create Collection` | `Ctrl+G` | Wraps the selection in a new empty parent |
| `GameObject ▸ NMJ ▸ Dissolve Collection` | `Ctrl+Shift+G` | The inverse — re-parents children out, deletes the empty |

It works at any depth, which is the point: the new parent is created as a **sibling of the
selection**, not at the scene root, so grouping three objects six levels deep leaves them exactly
where they were.

* Inserted at the sibling index of the topmost selected object, so the hierarchy does not reshuffle.
* Children keep their relative order.
* Pivot is placed at the centre of the selection's renderer bounds — which is what makes the
  collection usable as a rotate/scale handle afterwards — falling back to the average pivot when
  nothing selected has a renderer.
* Selecting a parent **and** its child groups only the parent; descendants of a selected object are
  filtered out, so nothing gets re-parented twice.
* A selection of `RectTransform`s produces a `RectTransform` group stretched to its parent, so UI
  layouts survive grouping.
* Drops straight into inline rename, so naming the collection is part of the same gesture.
* Fully undoable.

## Project setup

`Tools ▸ NMJ ▸ Setup ▸ Project Setup` — brings a fresh project up to a standard package set, and
keeps the integration define symbols in sync.

**What it can do:** free and OpenUPM packages install with one button. OpenUPM needs a scoped
registry, which the window writes into `Packages/manifest.json` first (merging scopes; it refuses
and prints instructions rather than clobbering an existing `scopedRegistries` block).

**What it cannot do — and why:** paid Asset Store packages cannot be installed by script. Unity
checks entitlement server-side against the signed-in account, so no API can make a package you don't
own appear. What *is* possible: if you already downloaded one on this machine, Unity keeps the
`.unitypackage` in a shared per-user cache, and the window imports it straight from there. Failing
that it opens the store page. **This is the honest ceiling on "one-button install".**

### Define symbols

`PackageDetector` scans loaded assemblies after every recompile and adds or removes the `NMJ_*`
defines to match. Assembly scanning is used rather than asmdef `versionDefines` because it covers
both cases uniformly: UPM packages, and Asset Store content dropped into `Assets/`, which is not a
package at all and has no version to bind to.

Defines are only written when the set actually changes — writing them triggers a recompile, which
re-enters the detector, so that check is what stops it looping.

### Odin and the NMJ attribute set

NMJ's `[ReadOnly]`, `[Required]` and `[Button]` live behind `NMJ_ATTRIBUTES`, which the detector sets
**only while Odin is absent**. Odin declares all three names too, so with both imported every use
becomes ambiguous — and both claim the default MonoBehaviour inspector. The window offers a
*Force NMJ attributes alongside Odin* override if you intend to fully qualify them.

| Attribute | Behaviour |
| --- | --- |
| `[ReadOnly]` | Shown but not editable |
| `[Required]` | Red error box under the field while a reference is unassigned or a string is blank |
| `[Button("Label", ButtonMode.PlayModeOnly)]` | Draws a button that invokes the method; works on `ScriptableObject`s and multi-selections |

## Prefab tools

| Command | Description |
| --- | --- |
| `GameObject ▸ NMJ ▸ Replace With Prefab…` | Swaps the selection for prefab instances. Position, rotation, parent, sibling index and active state are always kept; name, scale and layer/tag are optional. Undoable as one step. |
| `Assets ▸ NMJ ▸ Select Instances In Open Scenes` | Selects every instance of the selected prefab asset. |
| `Assets ▸ NMJ ▸ Find Usages` | Opens Asset Usage Detector when installed. |
| `Tools ▸ NMJ ▸ Find Missing Scripts In Open Scenes` | Lists and selects objects with missing scripts, then offers to strip the empty components. |

## Build tools

| Command | Description |
| --- | --- |
| `Tools ▸ NMJ ▸ Build ▸ Increment Patch/Minor/Major Version` | Bumps `bundleVersion` and zeroes the components after it. Also bumps `Android.bundleVersionCode` and the iOS build number, which stores require and a display-version bump alone does not cover. |
| `Tools ▸ NMJ ▸ Build ▸ Auto-Increment Patch On Build` | Toggle; runs as a pre-build step so the baked version is the incremented one. |
| `Tools ▸ NMJ ▸ Build ▸ Build Timestamped` (`Ctrl+Shift+B`) | Builds into `Builds/<Target>/<Product>_<version>_<timestamp>/` and reveals the folder. |
| `Tools ▸ NMJ ▸ Open ▸ …` | Persistent data path, project folder, editor log folder. |

## `AssetDatabaseExtensions`

| Method | Description |
| --- | --- |
| `CreateAsset(this ScriptableObject, string folderPath, string objectName)` | Creates the folder chain if needed, then writes the asset at a unique path. `folderPath` must start with `Assets/`; `objectName` must **not** include `.asset`. |

```csharp
var config = ScriptableObject.CreateInstance<GameConfig>();
config.CreateAsset($"{AssetDatabaseUtilities.AssetPath}__Game/Runtime/ScriptableObjects", "GameConfig");
```

## `AssetDatabaseUtilities`

| Member | Description |
| --- | --- |
| `const string AssetPath` | `"Assets/"` |
| `GetAllInstances<T>()` | Every `ScriptableObject` of type `T` in the project (via `FindAssets("t:T")`). |
| `LoadUss(string path)` | Loads a `StyleSheet`. `Assets/` prefix and `.uss` suffix are optional. |
| `LoadUssWithName(string name)` | Finds a `StyleSheet` by file name anywhere in the project. Throws if the name is ambiguous. |
| `LoadUxml(string path)` | Same as `LoadUss`, for `VisualTreeAsset`. |
| `LoadUxmlWithName(string name)` | Same as `LoadUssWithName`, for `VisualTreeAsset`. |
| `GetAllScenes(SerializedProperty)` | Resolves an array property of scene paths into `SceneAsset`s. |
| `GetScene(SerializedProperty)` | Resolves a single scene-path property into a `SceneAsset`. |

## `FolderStructureCreator`

`Tools ▸ NMJ ▸ Create Custom Folder Structure` scaffolds an opinionated layout under `Assets/__Game`:

```
Assets/__Game/
├── Editor/
│   ├── Scripts/
│   └── EditorResources/
└── Runtime/
    ├── Scenes/       Scripts/       Materials/   Textures/
    ├── Prefabs/      Audio/         Animations/  Fonts/
    ├── Resources/    ScriptableObjects/
    └── Scripts/Managers/
```

Existing folders are skipped (and logged). The root name `__Game` is a `const` in the script.

## `StateMachineGenerator`

`Tools ▸ NMJ ▸ StateMachineGenerator` — a code generator window that writes
`Assets/<Prefix>StateMachine.cs`.

**Naming**
* **Prefix name** — spaces become `_`, first letter is capitalised.
* **Number of states** — `+`/`-` buttons or a delayed int field. Each state name is upper-cased and
  `-`/space become `_`. One state is flagged **Default**.

**Code styling parameters**

| Option | Effect |
| --- | --- |
| Use UnityEvents | Emits `public UnityEvent On<State>Enter/Exit` and invokes them. |
| Use Foldout on Events | Emits `[Foldout("<State>")]` above each UnityEvent (requires an attribute package that provides `Foldout`). |
| Use regions / Space regions | Wraps sections in `#region`, optionally with blank lines between them. |
| Group by state / Group by phase | Methods ordered per state (`OnEnterIdle`, `OnUpdateIdle`, `OnExitIdle`, …) or per phase (all `OnEnter*`, then all `OnUpdate*`, then all `OnExit*`). |

**Generated shape**

```csharp
public enum FooState { IDLE, RUN }

public class FooStateMachine : MonoBehaviour
{
    public FooState CurrentState { get; private set; }

    private void Start()  => TransitionToState(CurrentState, FooState.IDLE);
    private void Update() => OnStateUpdate(CurrentState);

    private void OnStateEnter(FooState state)  { /* switch → OnEnterIdle() … */ }
    private void OnStateUpdate(FooState state) { /* switch → OnUpdateIdle() … */ }
    private void OnStateExit(FooState state)   { /* switch → OnExitIdle() … */ }

    private void TransitionToState(FooState from, FooState to);
    private void TransitionToState(FooState to);

    private void OnEnterIdle()  { }
    private void OnUpdateIdle() { }
    private void OnExitIdle()   { }
}
```

The **Generate** button is hidden when the target file already exists — it never overwrites.

## `GameplayTools`

`Tools ▸ NMJ ▸ Reset Transform` (`Ctrl+R`) resets every selected transform:
`localPosition = zero`, `localRotation = identity`, `localScale = one`. For a `RectTransform` it also
sets pivot/anchorMin/anchorMax to `(0.5, 0.5)` and `sizeDelta` to `(100, 100)`. Registers an Undo per object.

## `InstantiateInGrid`

`[ExecuteAlways] MonoBehaviour` with an **Instantiate In Grid** context-menu command.

| Field | Description |
| --- | --- |
| `column` / `row` | Grid dimensions (X and Z). |
| `parent` | Parent for the clones. |
| `objectToInstantiate` | Prefab or scene object to clone. |
| `xSize` / `zSize` | Spacing on each axis. |
| `startXPos` / `startZPos` | Local origin of the grid. |

Clones are named `<original> <x> <z>` and positioned in local space.

## `ListContainer` (`CustomListView`)

A UI Toolkit `VisualElement` that wraps any `IList` in a titled, foldable, add/remove list —
intended for Editor windows and custom inspectors. Backed by `ListContainer.uxml` + `ListContainer.uss`.

```csharp
var list = new ListContainer("Waves", useFoldout: true, showScroller: true)
{
    ItemsSource = myWaveList,
    makeItem    = () => new WaveRow(),
    itemAdded   = (element, index) => ((WaveRow)element).Bind(myWaveList, index),
    itemRemoved = index => Debug.Log($"removed {index}")
};
root.Add(list);
```

| Member | Description |
| --- | --- |
| `IList ItemsSource` | Backing collection. Assigning it for the first time populates the view. |
| `Func<VisualElement> makeItem` | Creates one row's visual. |
| `Action<VisualElement,int> itemAdded` | Called after a row is created — bind here. |
| `Action<int> itemRemoved` / `Action<int> destroyItem` | Called when a row is removed. |

The header carries the title and a delayed `IntegerField` that resizes the list; the footer has
`+` / `-` toolbar buttons. `UxmlFactory` is exposed, so it can also be used from UXML.

---

---

# Scene view overlays

For working with the Scene view maximised. Both are standard Unity **Overlays** — enable them from
the Scene view's ⋮ menu, or press `` ` `` to open the overlay picker. They can be docked to any
edge, floated, or collapsed to icons like any built-in overlay.

They live in their own assembly (`NMJ.NMJTools.SceneTools.Editor`, namespace `NMJTools.SceneTools`)
with no references to the rest of the package — see [Splitting them out](#splitting-them-out).

## Mini Hierarchy

A compact scene tree inside the Scene view, so the full Hierarchy window can stay closed.

* Search box; while filtering, results are flat, and the walk descends through collapsed branches
  so a deep match is still found.
* Expand/collapse arrows, with clicks on the arrow not stealing selection.
* Inactive objects are dimmed; icons match the Hierarchy's.
* Click selects, double-click frames the object in the view.
* Follows the current **prefab stage** — in prefab isolation mode it shows the prefab contents
  rather than the scene roots.
* Tracks `hierarchyChanged` and scrolls to keep the current selection visible.
* Caps at 5000 rows and says so, rather than stalling on a huge scene.

## Inspector

The selected object's inspector, docked in the Scene view.

Built on `InspectorElement`, so every custom editor, property drawer and attribute in the project
renders exactly as it does in the real Inspector — this is a second view onto the same machinery,
not a reimplementation. A GameObject shows one foldout per component (Transform open by default);
assets show a single inspector. With several objects selected it shows the active one and says how
many others there are.

## Selection History

Back/forward through everything you have selected — the browser history Unity has never had.
Buttons plus the full list; clicking an entry jumps straight to it.

The recorder is static and `[InitializeOnLoad]`, not owned by the overlay, so history survives the
overlay being closed, the Scene view re-docked, and a domain reload. Selecting something new after
stepping back discards the forward branch, exactly like a browser. Destroyed objects are pruned.

Two shortcuts are registered **without default bindings** (`NMJ/Selection History/Back` and
`/Forward`) — a shortcut that collides with an existing one leaves *both* disabled, and there is no
key here that is free on every platform. Bind them yourself in `Edit ▸ Shortcuts`; Alt+Left /
Alt+Right match the browser convention.

## Isolate

Solo and hide controls: **Isolate Selection**, **Exit Isolation**, **Hide**, **Show All**.

Drives Unity's own `SceneVisibilityManager` rather than toggling `SetActive`. That distinction
matters — scene visibility is a pure editor concern that never touches the serialized scene, so
nothing here can dirty a scene, break a prefab override, or leak into a build. Hiding things by
deactivating them does all three.

## Splitting them out

These two are the strongest candidate in this package to become **their own package**, and the
assembly boundary is already drawn so it is a single `git mv` away:

* They change the Scene view's UX persistently, which is a different kind of opt-in than a library
  of helper methods. Someone who wants `GetOrAddComponent` has not asked for their viewport rearranged.
* They are the piece most likely to overlap with an editor-enhancer package already in a project.
* They have the most room to grow — pinning, filters, favourites, multi-object inspectors — and that
  growth does not belong in a general toolbox.

Everything else here (pooling, batch rename, collections) is general-purpose and stays.

---

## Assembly definitions

| Assembly | Platforms | References |
| --- | --- | --- |
| `NMJ.NMJTools.Runtime` | All | — |
| `NMJ.NMJTools.Editor` | Editor | `NMJ.NMJTools.Runtime` |
| `NMJ.NMJTools.SceneTools.Editor` | Editor | — |
| `NMJ.NMJTools.Attributes` | All | — · gated by `NMJ_ATTRIBUTES` |
| `NMJ.NMJTools.Attributes.Editor` | Editor | `NMJ.NMJTools.Attributes` · gated by `NMJ_ATTRIBUTES` |
| `NMJ.NMJTools.UniTask` | All | `NMJ.NMJTools.Runtime`, UniTask · gated by `NMJ_UNITASK` |
| `NMJ.NMJTools.LitMotion` | All | `NMJ.NMJTools.Runtime`, LitMotion, LitMotion.Extensions · gated by `NMJ_LITMOTION` |
| `NMJ.NMJTools.Newtonsoft` | All | `NMJ.NMJTools.Runtime`, Newtonsoft.Json · gated by `NMJ_NEWTONSOFT` |

**No core assembly has a third-party reference**, so the package compiles in a completely clean
project and `package.json` declares no dependencies. Every optional integration lives in its own
assembly behind `defineConstraints`, so an uninstalled package means that assembly is skipped rather
than the build breaking.

> Third-party assemblies are referenced **by GUID, not by name**, wherever the GUID is known. A
> by-name reference that does not resolve takes the whole assembly down with the "references
> non-existent assemblies" error — the failure that used to affect the entire editor assembly via
> Ultimate Editor Enhancer.

## Related packages

**PlayerPrefs Manager** — an extended `PlayerPrefs` API (`bool`, `Color`, `Vector2/3/4`,
`Quaternion`, `Transform`) plus a cross-platform Editor window for browsing, editing, filtering,
categorising and live-monitoring every pref in a project. Formerly bundled here; now its own
package.

## License

MIT — see [LICENSE.md](LICENSE.md).

Third-party components are listed in [Third Party Notices.md](Third%20Party%20Notices.md).
