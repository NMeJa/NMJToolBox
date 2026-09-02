# NMJ ToolBox

Helper scripts, runtime extensions and Editor tooling for Unity.

Install via **Package Manager → Add package from git URL…**

```
https://github.com/NMeJa/NMJToolBox.git
```

* **Unity:** 2022.3 LTS or newer (uses C# 9 syntax: target-typed `new()`, range operators, switch
  expressions). The manifest said 2021.2 until v4.0.0, which was never accurate — the Scene view
  overlays use `ListView.selectionChanged` (2022.x; it replaced `onSelectionChange`) and
  `FindAnyObjectByType` (2021.3.18+).
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
| `Runtime/Patterns` | `NMJTools.Patterns` | | `Editor/Drawers` | `NMJTools.Drawers` |
| `Runtime/Attributes` | `NMJTools.Attributes` | | `Editor/Hierarchy` | `NMJTools.Hierarchy` |
| `Runtime/Debugging` | `NMJTools.Debugging` | | `Editor/Prefabs` | `NMJTools.Prefabs` |
| `Runtime/Scenes` | `NMJTools.Scenes` | | `Editor/Setup` | `NMJTools.Setup` |
| `Runtime/Tools` | `NMJTools.Tools` | | `Editor/Scaffolding` | `NMJTools.Scaffolding` |
| | | | `Editor/MaterialCreation` | `NMJTools.MaterialCreation` |
| | | | `Editor/Tools` | `NMJTools.Tools` |

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
| `Tools ▸ NMJ ▸ Reset Transform` | *(unbound)* | `GameplayTools` |
| `Tools ▸ NMJ ▸ Setup ▸ Create Folder Structure` | | `FolderStructureWindow` |
| `Tools ▸ NMJ ▸ State Machine Generator` | | `StateMachineGenerator` |
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
| `Tools ▸ NMJ ▸ Setup ▸ Open manifest.json` | | `PackageManifest` |
| `Assets ▸ NMJ ▸ Create Material From Textures` | | `MaterialFromTextures` |
| `Assets ▸ NMJ ▸ Open in External Editor` | | `OpenInExternalEditor` |
| `Assets ▸ NMJ ▸ Reveal in Explorer` | | `OpenInExternalEditor` |
| `Tools ▸ NMJ ▸ Build ▸ Build Timestamped` | `Ctrl+Alt+B` | `BuildTools` |
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
    public override void Enter() { }
    public override void Exit()  { }

    // Update and FixedUpdate are virtual — override only the one you need.
    public override void Update() { }
}

var fsm = new StateMachine(new IdleState());   // calls Enter() immediately
fsm.Change(new RunState());                    // Exit() old → Enter() new

private void Update()      => fsm.Tick();
private void FixedUpdate() => fsm.FixedTick();
```

`State` exposes a `MonoBehaviour User` property (and a `protected user` field) for the owning
behaviour, plus a constructor taking one.

`StateMachine` also has `Current`, `IsIn<T>()`, `Restart()` and a `StateChanged` event.
`Change` ignores a transition to the state already running — an accidental self-transition would
otherwise fire `Exit` then `Enter` for a state that never changed, resetting whatever they set up.
Use `Restart()` when a genuine re-entry is what you want.

> **Changed in v4.0.0.** `Update` and `FixedUpdate` were abstract on `State` but nothing ever
> called them, so every state was obliged to implement two methods that could not run. They are now
> virtual, and `Tick`/`FixedTick` drive them.

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

## `SerializableDictionary<TKey, TValue>` — removed in v4.0.0

Unity 6.6 serializes `Dictionary<,>` natively, so the wrapper and its drawer are gone. Migration is
dropping the type name:

```csharp
[SerializeField] private Dictionary<string, int> scores = new();
```

Three differences worth knowing:

* **It is opt-in.** `[SerializeField]` is required on every dictionary field — a *public* dictionary
  is not serialized automatically, unlike other public fields.
* **`[SerializeReference]` is not supported** on dictionary fields. If you relied on polymorphic
  values, `com.mackysoft.serializereference-extensions` covers that case and is listed in the
  Optional category of [Project setup](#project-setup).
* **No multi-object editing**, and a dictionary cannot nest directly inside another collection —
  `List<Dictionary<string, int>>` is invalid. Duplicate and null keys behave as the old drawer did:
  flagged in the inspector, first occurrence wins at runtime.

On Unity 2022.3–6.5 there is no built-in equivalent; stay on v3.1.0 or vendor the old file.

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

**One button, no prompts.** Everything selected is applied as a *single* `Client.AddAndRemove`
transaction — one resolve, one domain reload, and a dependency graph solved once with the whole set
visible to it, rather than one `Client.Add` and one reload per package. Cached Asset Store packages
are then imported back to back with `interactive: false`, chained on the import callbacks, so the
"Import Unity Package" dialog never appears. The queue lives in `SessionState` and survives the
domain reload the transaction causes.

**Per-item toggles, saved in the project.** The selection is a `SetupProfile` asset
(`Assets ▸ Create ▸ NMJ ▸ Setup Profile`) with presets — Standard, Mobile, Prototype, Everything,
Empty. It is a project asset rather than an editor preference for the same reason `FolderTemplate`
is: the set of packages a project depends on is a property of the project, not of the machine
building it. Committed, a clone reproduces the environment in one click. The window works with no
asset at all, against an in-memory profile, until you press **Save Profile…**.

**Removals are first-class.** Unity's default template ships the Visual Studio integration, the
VS Code integration and Visual Scripting; each costs compile and domain-reload time and none is
wanted in a Rider project. They are listed as toggles alongside the installs and go into the same
transaction.

**OpenUPM registry.** OpenUPM packages need a scoped registry in `Packages/manifest.json` before
`Client` can resolve the name, and Unity exposes no API for it — `Client` has Add, Remove and
Resolve, and nothing that touches `scopedRegistries`. The window parses the manifest properly and
merges: it unions scopes into an existing OpenUPM entry, leaves other registries alone, and writes
through a temp file so a half-written manifest can never land. (Until v4.0.0 it spliced a JSON
string in and gave up whenever *any* `scopedRegistries` block already existed — the common case.)

**What it cannot do — and why:** paid Asset Store packages cannot be installed by script. Unity
checks entitlement server-side against the signed-in account, so no API can make a package you don't
own appear. What *is* possible: if you already downloaded one on this machine, Unity keeps the
`.unitypackage` in a shared per-user cache, and the window imports it straight from there —
silently, as part of the same run. Only a package never downloaded on this machine needs a trip to
the store page. **This is the honest ceiling on "one-button install".**

### Catalogue

`PackageCatalog` is a plain data table — adding a package is one entry, not a code change. Entries
are grouped so a preset can switch a whole category:

| Category | Contents |
| --- | --- |
| **Core** | Newtonsoft Json, UniTask, PlayerPrefs Manager, Rider Editor, Test Framework, Editor Coroutines, Burst, Collections, Input System |
| **Debugging** | In-Game Debug Console, Runtime Inspector, Asset Usage Detector, Graphy, Profile Analyzer, Memory Profiler, Compilation Visualizer |
| **Authoring** | Cinemachine, Timeline, Recorder, ProBuilder, Addressables, LitMotion, BroAudio |
| **Optional** | R3, ZString, SerializeReference Extensions, NuGetForUnity, Code Coverage, Localization |
| **Mobile** | Mobile Notifications, Adaptive Performance |
| **Paid** | Odin Inspector, Init(args), vHierarchy/vInspector/vFolders/vTabs/vRuler, Editor Console Pro, Hot Reload |

Two notes on what is *not* there. **ProGrids** was dropped: it never left preview and was
discontinued — ProBuilder 5 has its own grid and snapping. **Input System** and **Addressables**
are off by default; the first forces an editor restart when the input backend switches, and the
second changes how the whole project loads content. Both are decisions, not defaults.

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

## Folder structure wizard

`Tools ▸ NMJ ▸ Setup ▸ Create Folder Structure` — pick a layout, edit it, see exactly what will be
created, then run it.

### Templates

Five ship with the package, as code rather than assets so a fresh install has something to offer
immediately:

| Template | For |
| --- | --- |
| **Standard** | Full 3D project — Runtime/Editor split with art, audio, data and UI broken out |
| **Minimal** | Jams and prototypes: Scripts, Scenes, Prefabs, Art, Audio |
| **Feature-based** | Grouped by feature rather than asset type; scales better on large projects |
| **2D Game** | Sprites, atlases, tilemaps, 2D animation |
| **Empty** | Root only — a starting point for your own |

Any of them can be edited in the window and saved with **Save as Template…**, producing a
`FolderTemplate` asset that appears in the dropdown under `Project/`. You can also create one
directly from `Assets ▸ Create ▸ NMJ ▸ Folder Template`.

> A template **asset** is deliberately the project-level setting rather than an editor preference.
> It lives in the repository, so a team shares one structure and changes to it show up in review;
> editor settings are per-user and per-machine, which is the wrong scope for a shared convention.

### Options

| Option | Description |
| --- | --- |
| **Root folder** | Created under the target path; everything nests inside it. Defaults to `__Game`. |
| **Create in** | Target parent. Defaults to the selected Project-window folder. |
| **Write .gitkeep files** | On by default — see below. |
| **Create assembly definitions** | Generates `<Root>.Runtime` and `<Root>.Editor` asmdefs, the editor one referencing the runtime one **by GUID** (read back after import, so it cannot be mistyped). |
| **Write README.md** | Documents the layout in the root folder, so the convention travels with the project. |

The preview lists every folder, ancestors included, marking new ones green and existing ones grey.
Creation is batched inside `StartAssetEditing`/`StopAssetEditing`, reports one summary line instead
of one log per folder, and checks `AssetDatabase.CreateFolder`'s return value so a failure is not
silent. Because folder creation is not undoable, it asks for confirmation first.

### Why `.gitkeep` matters

**Git cannot track an empty directory.** Unity writes a folder's `.meta` file *beside* the folder,
so a commit carries `Art/Textures.meta` but not `Art/Textures/`. On clone, Unity finds an orphaned
`.meta` with no folder, warns, and deletes it — and the whole scaffold quietly evaporates for
everyone except the person who generated it.

A `.gitkeep` fixes it: git tracks the file so the directory survives, and Unity ignores dot-files
entirely, so it never appears in the Project window and never gets a `.meta` of its own.

## `StateMachineGenerator`

`Tools ▸ NMJ ▸ State Machine Generator` — a code generator window that writes
`<chosen folder>/<Prefix>StateMachine.cs`.

This is deliberately still the enum-and-switch shape rather than the class-per-state
[`StateMachine`](#state--statemachine): they solve different problems, and a switch is the better
fit when the states are a fixed, small, closed set.

**Naming**
* **Prefix** — reduced to a valid PascalCase type-name fragment.
* **Namespace** — optional; empty generates into the global namespace. Remembered between runs.
* **Folder** — browse, or take the current Project-window selection. Remembered between runs.
* **States** — `+`/`-` buttons. Each name is upper-cased with non-alphanumerics turned into `_`.
  One state is flagged **Default** with a radio, and duplicates are reported before you can generate.

**Code styling parameters**

| Option | Effect |
| --- | --- |
| Generate UnityEvents | Emits `public UnityEvent on<State>Enter/Exit` and invokes them. |
| Use regions / Space regions | Wraps sections in `#region`, optionally with blank lines between them. |
| Group by state / Group by phase | Methods ordered per state (`OnEnterIdle`, `OnUpdateIdle`, `OnExitIdle`, …) or per phase (all `OnEnter*`, then all `OnUpdate*`, then all `OnExit*`). |

**Generated shape**

```csharp
public enum FooState { IDLE, RUN }

public class FooStateMachine : MonoBehaviour
{
    public FooState CurrentState { get; private set; }

    private void Start()  => OnStateEnter(CurrentState);
    private void Update() => OnStateUpdate(CurrentState);

    private void OnStateEnter(FooState state)  { /* switch → OnEnterIdle() … */ }
    private void OnStateUpdate(FooState state) { /* switch → OnUpdateIdle() … */ }
    private void OnStateExit(FooState state)   { /* switch → OnExitIdle() … */ }

    public void TransitionToState(FooState toState);   // ignores a self-transition

    private void OnEnterIdle()  { }
    private void OnUpdateIdle() { }
    private void OnExitIdle()   { }
}
```

**Generate** is disabled when the target file already exists — it never overwrites.

> **Fixed in v4.0.0.** With *Group by phase* on, the generator opened three `#region` directives and
> closed none, so the generated file did not compile. *Use Foldout on Events* emitted
> `[Foldout(...)]` from NaughtyAttributes, which this package stopped bundling — that option is
> gone. Output also always landed in the root of `Assets/` with no namespace.

## `GameplayTools`

`Tools ▸ NMJ ▸ Reset Transform` resets every selected transform: `localPosition = zero`,
`localRotation = identity`, `localScale = one`. For a `RectTransform` it also sets
pivot/anchorMin/anchorMax to `(0.5, 0.5)` and `sizeDelta` to `(100, 100)`. The whole selection is one
Undo step.

> **Changed in v4.0.0.** This used to be bound to `Ctrl+R`, which is Unity's own *Assets ▸ Refresh*.
> Unity resolves a shortcut collision by silently disabling **both** entries, so the binding cost the
> refresh shortcut and did not work itself. It is now registered through the shortcut system with no
> default binding — assign one in `Edit ▸ Shortcuts` (search "NMJ"), as with the batch-rename and
> selection-history commands.

## `InstantiateInGrid`

A `MonoBehaviour` that lays copies of a prefab out on a grid, built from a **Build Grid** button in
its inspector.

| Field | Description |
| --- | --- |
| `objectToInstantiate` | Prefab or scene object to clone. |
| `parent` | Parent for the copies. Falls back to this transform. |
| `columns` / `rows` | Grid dimensions (X and Z). |
| `spacing` | Spacing on each axis, as a `Vector2`. |
| `origin` | Local-space offset of the first cell — a full `Vector3`, so Y works. |
| `centred` | Centre the grid on the origin instead of growing towards +X/+Z. |

Copies are named `<prefab> [column,row]`. **Clear** removes only children carrying that suffix, so
anything you parented there by hand is left alone. Cell positions are drawn as gizmos when selected.

> **Fixed in v4.0.0.** This lived in `Editor/Tools/`, inside an assembly whose only platform is
> Editor. A `MonoBehaviour` in an editor-only assembly cannot exist in a build — Unity strips the
> assembly, and every scene or prefab referencing it loads with a missing script. It moved to the
> runtime assembly, keeping its `.meta` so existing scene references survive. The old
> `[ContextMenu]` also used plain `Instantiate`, producing prefab-disconnected clones that could not
> be undone; edit-mode building now goes through `PrefabUtility.InstantiatePrefab` and registers a
> single Undo step for the whole grid.

## Material from textures

`Assets ▸ NMJ ▸ Create Material From Textures` — right-click one or more texture folders. Creates
(or updates) one material per folder, wiring up the textures found there by filename suffix and
correcting each one's import settings.

Everything it needs to know comes from a `MaterialSetupProfile` asset
(`Assets ▸ Create ▸ NMJ ▸ Material Setup Profile`), pre-filled with the URP Lit conventions: the
shader name, the material prefix, the albedo filename pattern, and a table of map slots. Each slot
carries its filename suffixes (current convention first, so a folder holding two generators' output
resolves predictably), the shader property it feeds, and how it must be imported.

Two details that matter more than they look:

* **The albedo is the map with *no* suffix**, so any suffix the table does not know falls through
  and can be mistaken for it. That is why roughness, cavity, ORM and friends are listed with an
  empty shader property — recognised, import flags corrected, but assigned nowhere.
* **Import settings are silent when wrong.** A normal map not flagged as one lights incorrectly, and
  any mask left on sRGB has a gamma curve applied to what is data, not colour.

Re-running updates textures but never the tuning values — the `defaults` list is written only when
a material is first created, so nothing stamps over what you tuned in the inspector.

> **New in v4.0.0.** This replaces `StandardMaterialCreator`, which was not a reusable tool: it was
> one project's code copied into the package, with a hardcoded `StockAndSorcery` namespace, an
> `Elinis/TCP2` shader, a `Ramp_WarmCosy` asset and an `Assets ▸ Elinis` menu item. Installed
> anywhere else it added a menu entry that could only ever fail with "shader not found".

## `OpenInExternalEditor`

`Assets ▸ NMJ ▸ Open in External Editor` opens the selection in whatever editor the project is
configured to use, via `CodeEditor.CurrentEditor` — so Rider opens the solution it already has
loaded rather than a detached file. Anything no script editor will take (a `.png`, an `.fbx`) falls
back to the OS default application. `Assets ▸ NMJ ▸ Reveal in Explorer` selects the file itself
rather than its folder.

> **New in v4.0.0.** Replaces `OpenWithVSCode`, which hardcoded `code` as the executable, ignored
> the editor set in `Preferences ▸ External Tools`, sat in the global namespace inside a package,
> and silently did nothing where VS Code was not on `PATH`.

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
