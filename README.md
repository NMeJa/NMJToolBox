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

Namespace: `NMJToolBox` (except `ListContainer`, which is in `CustomListView`).

### Menu items

| Menu path | Script |
| --- | --- |
| `Tools ▸ NMJ ▸ Reset Transform` (`Ctrl+R`) | `GameplayTools` |
| `Tools ▸ NMJ ▸ Create Custom Folder Structure` | `FolderStructureCreator` |
| `Tools ▸ NMJ ▸ StateMachineGenerator` | `StateMachineGenerator` |
| Component ⋮ ▸ `Rename GameObject To This Component` | `GameObjectName` |

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

## Assembly definitions

| Assembly | Platforms | References |
| --- | --- | --- |
| `NMJ.NMJTools.Runtime` | All | Newtonsoft.Json |
| `NMJ.NMJTools.Editor` | Editor | `NMJ.NMJTools.Runtime` |

No third-party assembly references — the package compiles in a clean project.

## Related packages

**PlayerPrefs Manager** — an extended `PlayerPrefs` API (`bool`, `Color`, `Vector2/3/4`,
`Quaternion`, `Transform`) plus a cross-platform Editor window for browsing, editing, filtering,
categorising and live-monitoring every pref in a project. Formerly bundled here; now its own
package.

## License

MIT — see [LICENSE.md](LICENSE.md).

Third-party components are listed in [Third Party Notices.md](Third%20Party%20Notices.md).
