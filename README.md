# NMJ ToolBox

Helper scripts, runtime extensions and Editor tooling for Unity.

Install via **Package Manager → Add package from git URL…**

```
https://github.com/NMeJa/NMJToolBox.git
```

* **Unity:** 2021.2 or newer (uses C# 9 syntax: target-typed `new()`, range operators, switch expressions)
* **Dependencies:** `com.unity.nuget.newtonsoft-json` 3.2.1 (used only by `ParsingExtensions.ParseJson<T>`)
* **License:** MIT

> The **PlayerPrefs Manager** that used to live in this package now ships separately —
> see [Related packages](#related-packages).

---

## Contents

| Area | Assembly | Path |
| --- | --- | --- |
| Runtime | `NMJ.NMJTools.Runtime` | `NMJToolBox/NMJ/Runtime/` |
| Editor | `NMJ.NMJTools.Editor` | `NMJToolBox/NMJ/Editor/` |
| Scene view overlays | `NMJ.NMJTools.SceneView.Editor` | `NMJToolBox/NMJ/Editor.SceneView/` |

Namespace: `NMJToolBox`, except the scene-view overlays (`NMJToolBox.SceneTools`) and
`ListContainer` (`CustomListView`).

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
| Component ⋮ ▸ `Rename GameObject To This Component` | | `GameObjectName` |
| Scene view ⋮ ▸ `Mini Hierarchy` / `Inspector` | `` ` `` | Scene view overlays |

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
using NMJToolBox;

var deck = new List<Card>(allCards);
deck.Shuffle();                       // in place
var one = deck.GetRandomElement();
var top = deck.Pop();

if (deck.IsEmpty()) Reshuffle();
```

## `StringBeautifier` (file: `Extensions/Beautify.cs`)

Turns identifiers into human-readable labels.

| Method | `"player_healthBar"` → |
| --- | --- |
| `ReplaceUnderscoresWithSpaces()` | `"player healthBar"` |
| `SplitCamelCase()` | `"player_health Bar"` |
| `CapitalizeFirstLetter()` | Title-cases **every** word (`en-US`) |
| `RemoveExcessWhitespace()` | Collapses runs of whitespace to one space |
| `Beautify()` | All four, in that order → `"Player Health Bar"` |

All methods return `string.Empty` for `null`/empty input.

## `StringExtensions`

| Method | Description |
| --- | --- |
| `TrimStart(this string, string wordToRemove)` | Removes `wordToRemove` from the start, if present. |
| `TrimEnd(this string, string wordToRemove)` | Removes `wordToRemove` from the end, if present. |

> These are *string* overloads of BCL names that normally take `char[]`. The compiler picks the
> extension only because no instance overload accepts a `string`.

## `ParsingExtensions`

| Method | Description |
| --- | --- |
| `ToBoolean(this float value, bool useAbsolute = false, bool useInverse = false, float differentialValue = 0.5f)` | Threshold a float into a bool. `useAbsolute` applies `Math.Abs` first; `useInverse` flips the comparison to `<`. |
| `ParseJson<T>(this string json)` | `JsonConvert.DeserializeObject<T>`, rethrowing with the offending JSON in the message. |

## `UIToolkitExtensions`

| Method | Description |
| --- | --- |
| `DisplayVisibility(this IStyle, DisplayStyle)` | Sets `style.display`. |
| `ChangeBackgroundColor(this IStyle, Color)` | Sets `style.backgroundColor`. |

## `SingletonPattern<T>`

`[DefaultExecutionOrder(-10)]` abstract `MonoBehaviour` singleton base.

```csharp
public class AudioManager : SingletonPattern<AudioManager>
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
using NMJToolBox;

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
| `{B}` | — | Beautified — `my_healthBar` → `My Health Bar` (uses `StringBeautifier`) |
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

They live in their own assembly (`NMJ.NMJTools.SceneView.Editor`, namespace `NMJToolBox.SceneTools`)
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
| `NMJ.NMJTools.Runtime` | All | Newtonsoft.Json |
| `NMJ.NMJTools.Editor` | Editor | `NMJ.NMJTools.Runtime` |
| `NMJ.NMJTools.SceneView.Editor` | Editor | — |

No third-party assembly references — the package compiles in a clean project.

## Related packages

**PlayerPrefs Manager** — an extended `PlayerPrefs` API (`bool`, `Color`, `Vector2/3/4`,
`Quaternion`, `Transform`) plus a cross-platform Editor window for browsing, editing, filtering,
categorising and live-monitoring every pref in a project. Formerly bundled here; now its own
package.

## License

MIT — see [LICENSE.md](LICENSE.md).

Third-party components are listed in [Third Party Notices.md](Third%20Party%20Notices.md).
