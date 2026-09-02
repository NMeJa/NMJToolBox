using System.Collections.Generic;
using System.Linq;

namespace NMJTools.Setup
{
	/// <summary>How a package reaches the project.</summary>
	public enum InstallSource
	{
		/// <summary>UPM git URL — installable straight from <c>Client.Add</c>.</summary>
		Git,

		/// <summary>OpenUPM registry package — needs the scoped registry in manifest.json first.</summary>
		OpenUpm,

		/// <summary>
		/// Package on Unity's own registry. Installs by name exactly like <see cref="Git"/>, but must
		/// not trigger the OpenUPM scoped-registry write — Unity's registry is already configured in
		/// every project.
		/// </summary>
		Registry,

		/// <summary>
		/// Paid Asset Store package. Cannot be purchased or downloaded programmatically. If the user
		/// already owns it and Unity has cached the .unitypackage, it can be imported; otherwise all
		/// we can do is open the store page.
		/// </summary>
		AssetStore
	}

	/// <summary>
	/// Grouping for the setup window, and the unit a profile preset turns on or off in bulk.
	/// </summary>
	public enum PackageCategory
	{
		/// <summary>Things nearly every project here wants: serialisation, async, editor integration.</summary>
		Core,

		/// <summary>Console, inspector, profiling, usage detection.</summary>
		Debugging,

		/// <summary>Content authoring: cinematics, level building, recording.</summary>
		Authoring,

		/// <summary>Useful but not automatic — opt in per project.</summary>
		Optional,

		/// <summary>Mobile-target extras.</summary>
		Mobile,

		/// <summary>Paid Asset Store content.</summary>
		Paid
	}

	/// <summary>One package NMJ ToolBox knows how to detect and optionally install.</summary>
	public class PackageEntry
	{
		/// <summary>
		/// UPM package name, e.g. <c>com.cysharp.unitask</c>. Empty for Asset Store drops.
		/// Doubles as the stable key a <see cref="SetupProfile"/> stores its toggle against, which is
		/// why Asset Store entries fall back to <see cref="displayName"/> — see <see cref="Key"/>.
		/// </summary>
		public string packageId = string.Empty;

		public string displayName = string.Empty;

		public InstallSource source;

		public PackageCategory category = PackageCategory.Optional;

		/// <summary>Git URL, or the registry package name.</summary>
		public string identifier = string.Empty;

		/// <summary>Store or repository page, opened when automatic installation is not possible.</summary>
		public string url = string.Empty;

		/// <summary>
		/// Scripting define symbol added while this package is present. Empty means "detect but do
		/// not define" — used for packages that need no code integration.
		/// </summary>
		public string define = string.Empty;

		/// <summary>
		/// Assembly names that prove this package is present. Matched against loaded assembly simple
		/// names. Assembly detection is used rather than asmdef <c>versionDefines</c> because it works
		/// identically for UPM packages and for Asset Store content dropped into <c>Assets/</c>,
		/// which is not a package at all and has no version to bind to.
		/// </summary>
		public string[] assemblies = System.Array.Empty<string>();

		/// <summary>
		/// Fallback detection for assets with no assembly definition — their code lands in
		/// <c>Assembly-CSharp-Editor</c>, so the assembly name proves nothing and a known type must
		/// be looked for instead.
		/// </summary>
		public string[] types = System.Array.Empty<string>();

		/// <summary>Substrings matched against cached .unitypackage paths, for Asset Store entries.</summary>
		public string[] cacheHints = System.Array.Empty<string>();

		/// <summary>
		/// Ticked when a profile is reset to defaults. False for anything a project should have to ask
		/// for — heavy packages, platform-specific ones, and everything paid.
		/// </summary>
		public bool defaultEnabled;

		public string notes = string.Empty;

		/// <summary>Stable identity for profile storage. Never change it for an existing entry.</summary>
		public string Key => string.IsNullOrEmpty(packageId) ? "asset:" + displayName : packageId;

		/// <summary>True for the sources <see cref="UnityEditor.PackageManager.Client"/> can install unattended.</summary>
		public bool IsUpm => source is InstallSource.Git or InstallSource.OpenUpm or InstallSource.Registry;
	}

	/// <summary>
	/// A package the setup removes rather than installs.
	///
	/// The .bat this replaces removed three by hand on every new project, and the reasoning is worth
	/// keeping: each one is installed by Unity's default template, each one costs compile time and
	/// domain-reload time, and none of them is wanted in a Rider project.
	/// </summary>
	public class PackageRemoval
	{
		public string packageId = string.Empty;
		public string displayName = string.Empty;
		public string reason = string.Empty;
		public bool defaultEnabled = true;

		public string Key => "remove:" + packageId;
	}

	/// <summary>
	/// The package set, as a plain data table, so adding one is a single entry rather than a code change.
	/// </summary>
	public static class PackageCatalog
	{
		public const string OpenUpmRegistryName = "package.openupm.com";
		public const string OpenUpmRegistryUrl = "https://package.openupm.com";

		/// <summary>Set while Odin is absent, so NMJ's own inspector attributes can take over.</summary>
		public const string NmjAttributesDefine = "NMJ_ATTRIBUTES";

		/// <summary>Defined by Odin itself — listed here so the toggle can read it, never written.</summary>
		public const string OdinDefine = "ODIN_INSPECTOR";

		public static IReadOnlyList<PackageEntry> All => entries;
		public static IReadOnlyList<PackageRemoval> Removals => removals;

		public static PackageEntry Find(string key) => entries.FirstOrDefault(e => e.Key == key);

		public static IEnumerable<PackageEntry> InCategory(PackageCategory category) =>
			entries.Where(e => e.category == category);

		// ---------------------------------------------------------------------------------------
		// Removals — the `openupm remove` lines from the old .bat.
		// ---------------------------------------------------------------------------------------

		private static readonly List<PackageRemoval> removals = new()
		{
			new PackageRemoval
			{
				packageId = "com.unity.ide.visualstudio",
				displayName = "Visual Studio Editor",
				reason = "Rider is the editor here. The VS integration still runs its project-file " +
						 "generation on every script change, for nothing."
			},
			new PackageRemoval
			{
				packageId = "com.unity.ide.vscode",
				displayName = "VS Code Editor",
				reason = "Deprecated by Unity and superseded by the official VS Code extension. Same " +
						 "wasted project-file generation as above."
			},
			new PackageRemoval
			{
				packageId = "com.unity.visualscripting",
				displayName = "Visual Scripting",
				reason = "Never used in a C# codebase, and its assemblies are a measurable slice of " +
						 "every domain reload."
			}
		};

		// ---------------------------------------------------------------------------------------
		// Packages
		// ---------------------------------------------------------------------------------------

		private static readonly List<PackageEntry> entries = new()
		{
			// ========== Core ==========

			new PackageEntry
			{
				packageId = "com.unity.nuget.newtonsoft-json",
				displayName = "Newtonsoft Json",
				source = InstallSource.Registry,
				category = PackageCategory.Core,
				identifier = "com.unity.nuget.newtonsoft-json",
				url = "https://docs.unity3d.com/Packages/com.unity.nuget.newtonsoft-json@latest",
				define = "NMJ_NEWTONSOFT",
				defaultEnabled = true,
				// The Unity package ships the assembly as Newtonsoft.Json, not Newtonsoft.
				assemblies = new[] { "Newtonsoft.Json" },
				notes = "Enables JsonExtensions.ParseJson<T>. Without it that one helper is simply absent; " +
						"nothing else in the package needs it."
			},
			new PackageEntry
			{
				packageId = "com.cysharp.unitask",
				displayName = "UniTask",
				source = InstallSource.Git,
				category = PackageCategory.Core,
				identifier = "https://github.com/Cysharp/UniTask.git?path=src/UniTask/Assets/Plugins/UniTask",
				url = "https://github.com/Cysharp/UniTask",
				define = "NMJ_UNITASK",
				defaultEnabled = true,
				assemblies = new[] { "UniTask" },
				notes = "Enables the allocation-free async pooling and scene-loading helpers."
			},
			new PackageEntry
			{
				packageId = "com.nmj_gd.playerprefsmanager",
				displayName = "PlayerPrefs Manager",
				source = InstallSource.Git,
				category = PackageCategory.Core,
				identifier = "https://github.com/NMeJa/PlayerPrefsManager.git",
				url = "https://github.com/NMeJa/PlayerPrefsManager",
				defaultEnabled = true,
				assemblies = new[] { "PlayerPrefsManager", "NMJ.PlayerPrefsManager" },
				types = new[] { "PlayerPrefsManager" },
				notes = "Typed, editor-inspectable PlayerPrefs. If the repository is private, UPM needs " +
						"git credentials on the machine — a public HTTPS clone is the simplest fix."
			},
			new PackageEntry
			{
				packageId = "com.unity.ide.rider",
				displayName = "Rider Editor",
				source = InstallSource.Registry,
				category = PackageCategory.Core,
				identifier = "com.unity.ide.rider",
				url = "https://docs.unity3d.com/Packages/com.unity.ide.rider@latest",
				defaultEnabled = true,
				assemblies = new[] { "Unity.Rider.Editor" },
				notes = "The counterpart to removing the Visual Studio and VS Code integrations."
			},
			new PackageEntry
			{
				packageId = "com.unity.test-framework",
				displayName = "Test Framework",
				source = InstallSource.Registry,
				category = PackageCategory.Core,
				identifier = "com.unity.test-framework",
				url = "https://docs.unity3d.com/Packages/com.unity.test-framework@latest",
				defaultEnabled = true,
				assemblies = new[] { "UnityEngine.TestRunner", "UnityEditor.TestRunner" }
			},
			new PackageEntry
			{
				packageId = "com.unity.editorcoroutines",
				displayName = "Editor Coroutines",
				source = InstallSource.Registry,
				category = PackageCategory.Core,
				identifier = "com.unity.editorcoroutines",
				url = "https://docs.unity3d.com/Packages/com.unity.editorcoroutines@latest",
				defaultEnabled = true,
				assemblies = new[] { "Unity.EditorCoroutines.Editor" },
				notes = "Long-running editor tooling without blocking the main thread."
			},
			new PackageEntry
			{
				packageId = "com.unity.burst",
				displayName = "Burst",
				source = InstallSource.Registry,
				category = PackageCategory.Core,
				identifier = "com.unity.burst",
				url = "https://docs.unity3d.com/Packages/com.unity.burst@latest",
				defaultEnabled = true,
				assemblies = new[] { "Unity.Burst" }
			},
			new PackageEntry
			{
				packageId = "com.unity.collections",
				displayName = "Collections",
				source = InstallSource.Registry,
				category = PackageCategory.Core,
				identifier = "com.unity.collections",
				url = "https://docs.unity3d.com/Packages/com.unity.collections@latest",
				defaultEnabled = true,
				assemblies = new[] { "Unity.Collections" },
				notes = "Native containers. Pulls Burst in as a dependency anyway."
			},
			new PackageEntry
			{
				packageId = "com.unity.inputsystem",
				displayName = "Input System",
				source = InstallSource.Registry,
				category = PackageCategory.Core,
				identifier = "com.unity.inputsystem",
				url = "https://docs.unity3d.com/Packages/com.unity.inputsystem@latest",
				defaultEnabled = false,
				assemblies = new[] { "Unity.InputSystem" },
				notes = "Off by default because switching the active input backend forces an editor " +
						"restart — install it deliberately, at the start of a project."
			},

			// ========== Debugging ==========

			new PackageEntry
			{
				packageId = "com.yasirkula.ingamedebugconsole",
				displayName = "In-Game Debug Console",
				source = InstallSource.OpenUpm,
				category = PackageCategory.Debugging,
				identifier = "com.yasirkula.ingamedebugconsole",
				url = "https://openupm.com/packages/com.yasirkula.ingamedebugconsole/",
				defaultEnabled = true,
				assemblies = new[] { "IngameDebugConsole.Runtime", "IngameDebugConsole" }
			},
			new PackageEntry
			{
				packageId = "com.yasirkula.runtimeinspector",
				displayName = "Runtime Inspector",
				source = InstallSource.OpenUpm,
				category = PackageCategory.Debugging,
				identifier = "com.yasirkula.runtimeinspector",
				url = "https://openupm.com/packages/com.yasirkula.runtimeinspector/",
				defaultEnabled = true,
				assemblies = new[] { "RuntimeInspector.Runtime", "RuntimeInspector" }
			},
			new PackageEntry
			{
				packageId = "com.yasirkula.assetusagedetector",
				displayName = "Asset Usage Detector",
				source = InstallSource.OpenUpm,
				category = PackageCategory.Debugging,
				identifier = "com.yasirkula.assetusagedetector",
				url = "https://openupm.com/packages/com.yasirkula.assetusagedetector/",
				define = "NMJ_ASSETUSAGEDETECTOR",
				defaultEnabled = true,
				assemblies = new[] { "AssetUsageDetector.Editor", "AssetUsageDetector.Runtime", "AssetUsageDetector" },
				notes = "Adds a Find Usages command to the NMJ prefab tools."
			},
			new PackageEntry
			{
				packageId = "com.tayx.graphy",
				displayName = "Graphy",
				source = InstallSource.OpenUpm,
				category = PackageCategory.Debugging,
				identifier = "com.tayx.graphy",
				url = "https://openupm.com/packages/com.tayx.graphy/",
				defaultEnabled = true,
				assemblies = new[] { "Tayx.Graphy" }
			},
			new PackageEntry
			{
				packageId = "com.unity.performance.profile-analyzer",
				displayName = "Profile Analyzer",
				source = InstallSource.Registry,
				category = PackageCategory.Debugging,
				identifier = "com.unity.performance.profile-analyzer",
				url = "https://docs.unity3d.com/Packages/com.unity.performance.profile-analyzer@latest",
				defaultEnabled = true,
				assemblies = new[] { "Unity.PerformanceProfileAnalyzer.Editor", "Unity.PerformanceProfileAnalyzer" }
			},
			new PackageEntry
			{
				packageId = "com.unity.memoryprofiler",
				displayName = "Memory Profiler",
				source = InstallSource.Registry,
				category = PackageCategory.Debugging,
				identifier = "com.unity.memoryprofiler",
				url = "https://docs.unity3d.com/Packages/com.unity.memoryprofiler@latest",
				defaultEnabled = true,
				assemblies = new[] { "Unity.MemoryProfiler.Editor", "Unity.MemoryProfiler" },
				notes = "The other half of Profile Analyzer. On a mobile target this is the tool that " +
						"finds what is actually killing you."
			},
			new PackageEntry
			{
				packageId = "com.needle.compilation-visualizer",
				displayName = "Compilation Visualizer",
				source = InstallSource.OpenUpm,
				category = PackageCategory.Debugging,
				identifier = "com.needle.compilation-visualizer",
				url = "https://openupm.com/packages/com.needle.compilation-visualizer/",
				defaultEnabled = false,
				assemblies = new[] { "Needle.CompilationVisualizer.Editor", "Needle.CompilationVisualizer" },
				notes = "Shows which assembly is serialising your compile. The tool that tells you " +
						"whether an asmdef split actually paid off."
			},

			// ========== Authoring ==========

			new PackageEntry
			{
				packageId = "com.unity.cinemachine",
				displayName = "Cinemachine",
				source = InstallSource.Registry,
				category = PackageCategory.Authoring,
				identifier = "com.unity.cinemachine",
				url = "https://docs.unity3d.com/Packages/com.unity.cinemachine@latest",
				defaultEnabled = true,
				assemblies = new[] { "Unity.Cinemachine", "Cinemachine" },
				notes = "Resolves to Cinemachine 3.x on Unity 6, which is a breaking rename from 2.x " +
						"(Cinemachine.* becomes Unity.Cinemachine.*). Fine on a new project."
			},
			new PackageEntry
			{
				packageId = "com.unity.timeline",
				displayName = "Timeline",
				source = InstallSource.Registry,
				category = PackageCategory.Authoring,
				identifier = "com.unity.timeline",
				url = "https://docs.unity3d.com/Packages/com.unity.timeline@latest",
				defaultEnabled = true,
				assemblies = new[] { "Unity.Timeline" }
			},
			new PackageEntry
			{
				packageId = "com.unity.recorder",
				displayName = "Recorder",
				source = InstallSource.Registry,
				category = PackageCategory.Authoring,
				identifier = "com.unity.recorder",
				url = "https://docs.unity3d.com/Packages/com.unity.recorder@latest",
				defaultEnabled = true,
				assemblies = new[] { "Unity.Recorder.Editor", "Unity.Recorder" },
				notes = "Capture for store pages and bug reports. Depends on Timeline."
			},
			new PackageEntry
			{
				packageId = "com.unity.probuilder",
				displayName = "ProBuilder",
				source = InstallSource.Registry,
				category = PackageCategory.Authoring,
				identifier = "com.unity.probuilder",
				url = "https://docs.unity3d.com/Packages/com.unity.probuilder@latest",
				defaultEnabled = false,
				assemblies = new[] { "Unity.ProBuilder", "Unity.ProBuilder.Editor" },
				notes = "Greyboxing. ProGrids is deliberately not listed alongside it any more — it " +
						"never left preview and was discontinued; ProBuilder 5 has its own grid and snapping."
			},
			new PackageEntry
			{
				packageId = "com.unity.addressables",
				displayName = "Addressables",
				source = InstallSource.Registry,
				category = PackageCategory.Authoring,
				identifier = "com.unity.addressables",
				url = "https://docs.unity3d.com/Packages/com.unity.addressables@latest",
				defaultEnabled = false,
				assemblies = new[] { "Unity.Addressables", "Unity.Addressables.Editor" },
				notes = "Off by default for the same reason the old .bat asked: it changes how the whole " +
						"project loads content, so it is a decision, not a default."
			},
			new PackageEntry
			{
				packageId = "com.annulusgames.lit-motion",
				displayName = "LitMotion",
				source = InstallSource.Git,
				category = PackageCategory.Authoring,
				identifier = "https://github.com/annulusgames/LitMotion.git?path=src/LitMotion/Assets/LitMotion",
				url = "https://github.com/annulusgames/LitMotion",
				define = "NMJ_LITMOTION",
				defaultEnabled = true,
				assemblies = new[] { "LitMotion" },
				notes = "Enables tweened spawn/despawn transitions for pooled objects."
			},
			new PackageEntry
			{
				packageId = "com.man572142.bro-audio",
				displayName = "BroAudio",
				source = InstallSource.Git,
				category = PackageCategory.Authoring,
				identifier = "https://github.com/man572142/Bro_Audio.git?path=/Assets/BroAudio",
				url = "https://github.com/man572142/Bro_Audio",
				define = "NMJ_BROAUDIO",
				defaultEnabled = false,
				assemblies = new[] { "Ami.BroAudio", "BroAudio" },
				notes = "No code integration yet — detected so it can be installed in one pass."
			},

			// ========== Optional ==========
			// Suggested additions rather than ports of the .bat. All free, all OpenUPM or Unity
			// registry, all off by default so a Run Setup never installs something unasked.

			new PackageEntry
			{
				packageId = "com.cysharp.r3",
				displayName = "R3 (Reactive Extensions)",
				source = InstallSource.OpenUpm,
				category = PackageCategory.Optional,
				identifier = "com.cysharp.r3",
				url = "https://openupm.com/packages/com.cysharp.r3/",
				defaultEnabled = false,
				assemblies = new[] { "R3", "R3.Unity" },
				notes = "UniRx's successor, same authors as UniTask and designed to sit beside it. " +
						"Observable event channels without the SO boilerplate."
			},
			new PackageEntry
			{
				packageId = "com.cysharp.zstring",
				displayName = "ZString",
				source = InstallSource.OpenUpm,
				category = PackageCategory.Optional,
				identifier = "com.cysharp.zstring",
				url = "https://openupm.com/packages/com.cysharp.zstring/",
				defaultEnabled = false,
				assemblies = new[] { "ZString" },
				notes = "Zero-allocation string formatting. Worth it the moment a per-frame HUD label " +
						"shows up in the GC profile."
			},
			new PackageEntry
			{
				packageId = "com.mackysoft.serializereference-extensions",
				displayName = "SerializeReference Extensions",
				source = InstallSource.OpenUpm,
				category = PackageCategory.Optional,
				identifier = "com.mackysoft.serializereference-extensions",
				url = "https://openupm.com/packages/com.mackysoft.serializereference-extensions/",
				defaultEnabled = false,
				assemblies = new[] { "MackySoft.SerializeReferenceExtensions", "MackySoft.SerializeReferenceExtensions.Editor" },
				notes = "A type picker for [SerializeReference] fields — polymorphic data in a " +
						"ScriptableObject without reaching for Odin serialisation."
			},
			new PackageEntry
			{
				packageId = "com.github-glitchenzo.nugetforunity",
				displayName = "NuGetForUnity",
				source = InstallSource.OpenUpm,
				category = PackageCategory.Optional,
				identifier = "com.github-glitchenzo.nugetforunity",
				url = "https://openupm.com/packages/com.github-glitchenzo.nugetforunity/",
				defaultEnabled = false,
				assemblies = new[] { "NugetForUnity", "NugetForUnity.Editor" },
				notes = "Pulls plain .NET libraries in without hand-copying DLLs and guessing at " +
						"target frameworks."
			},
			new PackageEntry
			{
				packageId = "com.unity.testtools.codecoverage",
				displayName = "Code Coverage",
				source = InstallSource.Registry,
				category = PackageCategory.Optional,
				identifier = "com.unity.testtools.codecoverage",
				url = "https://docs.unity3d.com/Packages/com.unity.testtools.codecoverage@latest",
				defaultEnabled = false,
				assemblies = new[] { "Unity.TestTools.CodeCoverage.Editor" }
			},
			new PackageEntry
			{
				packageId = "com.unity.localization",
				displayName = "Localization",
				source = InstallSource.Registry,
				category = PackageCategory.Optional,
				identifier = "com.unity.localization",
				url = "https://docs.unity3d.com/Packages/com.unity.localization@latest",
				defaultEnabled = false,
				assemblies = new[] { "Unity.Localization" },
				notes = "Depends on Addressables — installing this installs that too."
			},

			// ========== Mobile ==========

			new PackageEntry
			{
				packageId = "com.unity.mobile.notifications",
				displayName = "Mobile Notifications",
				source = InstallSource.Registry,
				category = PackageCategory.Mobile,
				identifier = "com.unity.mobile.notifications",
				url = "https://docs.unity3d.com/Packages/com.unity.mobile.notifications@latest",
				defaultEnabled = false,
				assemblies = new[] { "Unity.Notifications.Android", "Unity.Notifications.iOS", "Unity.Notifications" }
			},
			new PackageEntry
			{
				packageId = "com.unity.adaptiveperformance",
				displayName = "Adaptive Performance",
				source = InstallSource.Registry,
				category = PackageCategory.Mobile,
				identifier = "com.unity.adaptiveperformance",
				url = "https://docs.unity3d.com/Packages/com.unity.adaptiveperformance@latest",
				defaultEnabled = false,
				assemblies = new[] { "Unity.AdaptivePerformance" },
				notes = "Thermal and power feedback on Android. Lets a game scale itself down before " +
						"the device throttles it down for you."
			},

			// ========== Paid (Asset Store) ==========
			// Never installed automatically. Unity exposes no API to purchase or download store
			// content; the most that is possible is importing a .unitypackage the user already owns
			// and Unity has cached locally. See AssetStoreLocator.

			new PackageEntry
			{
				displayName = "Odin Inspector",
				source = InstallSource.AssetStore,
				category = PackageCategory.Paid,
				url = "https://assetstore.unity.com/packages/tools/utilities/odin-inspector-and-serializer-89041",
				defaultEnabled = true,
				assemblies = new[] { "Sirenix.OdinInspector.Attributes", "Sirenix.OdinInspector.Editor" },
				cacheHints = new[] { "Odin Inspector" },
				notes = "Defines ODIN_INSPECTOR itself. While present, NMJ's own [ReadOnly]/[Required]/[Button] " +
						"stay switched off so the two cannot collide."
			},
			new PackageEntry
			{
				displayName = "Init(args)",
				source = InstallSource.AssetStore,
				category = PackageCategory.Paid,
				url = "https://assetstore.unity.com/packages/tools/utilities/init-args-a-next-gen-di-framework-200530",
				define = "NMJ_INITARGS",
				defaultEnabled = true,
				assemblies = new[] { "Sisus.Init", "Sisus.Init.Editor" },
				types = new[] { "Sisus.Init.InitArgs" },
				cacheHints = new[] { "Init(args)", "InitArgs" }
			},
			new PackageEntry
			{
				displayName = "vHierarchy 2",
				source = InstallSource.AssetStore,
				category = PackageCategory.Paid,
				url = "https://assetstore.unity.com/packages/tools/utilities/vhierarchy-2-253397",
				defaultEnabled = true,
				assemblies = new[] { "VHierarchy", "VHierarchy.Editor" },
				types = new[] { "VHierarchy2.VHierarchy", "VHierarchy.VHierarchy" },
				cacheHints = new[] { "vHierarchy" }
			},
			new PackageEntry
			{
				displayName = "vInspector 2",
				source = InstallSource.AssetStore,
				category = PackageCategory.Paid,
				url = "https://assetstore.unity.com/packages/tools/utilities/vinspector-2-252297",
				defaultEnabled = true,
				assemblies = new[] { "VInspector", "VInspector.Editor" },
				types = new[] { "VInspector2.VInspector", "VInspector.VInspector" },
				cacheHints = new[] { "vInspector" }
			},
			new PackageEntry
			{
				displayName = "vFolders 2",
				source = InstallSource.AssetStore,
				category = PackageCategory.Paid,
				url = "https://assetstore.unity.com/packages/tools/utilities/vfolders-2-255470",
				defaultEnabled = true,
				assemblies = new[] { "VFolders", "VFolders.Editor" },
				cacheHints = new[] { "vFolders" }
			},
			new PackageEntry
			{
				displayName = "vTabs 2",
				source = InstallSource.AssetStore,
				category = PackageCategory.Paid,
				url = "https://assetstore.unity.com/packages/tools/utilities/vtabs-2-253396",
				defaultEnabled = true,
				assemblies = new[] { "VTabs", "VTabs.Editor" },
				cacheHints = new[] { "vTabs" }
			},
			new PackageEntry
			{
				displayName = "vRuler",
				source = InstallSource.AssetStore,
				category = PackageCategory.Paid,
				url = "https://assetstore.unity.com/packages/tools/utilities/vruler-249760",
				defaultEnabled = false,
				assemblies = new[] { "VRuler", "VRuler.Editor" },
				cacheHints = new[] { "vRuler" }
			},
			new PackageEntry
			{
				displayName = "Editor Console Pro",
				source = InstallSource.AssetStore,
				category = PackageCategory.Paid,
				url = "https://assetstore.unity.com/packages/tools/utilities/editor-console-pro-11889",
				defaultEnabled = true,
				types = new[] { "FlyingWormConsole3.ConsolePro3Window" },
				cacheHints = new[] { "Editor Console Pro" }
			},
			new PackageEntry
			{
				displayName = "Hot Reload",
				source = InstallSource.AssetStore,
				category = PackageCategory.Paid,
				url = "https://assetstore.unity.com/packages/p/hot-reload-edit-code-without-compiling-254358",
				defaultEnabled = true,
				assemblies = new[] { "SingularityGroup.HotReload", "SingularityGroup.HotReload.Editor" },
				cacheHints = new[] { "Hot Reload" }
			}
		};
	}
}
