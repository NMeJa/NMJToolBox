using System.Collections.Generic;

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
		/// Package on Unity's own registry. Installs by name through <c>Client.Add</c> exactly like
		/// <see cref="Git"/>, but must not trigger the OpenUPM scoped-registry write — Unity's
		/// registry is already configured in every project.
		/// </summary>
		Registry,

		/// <summary>
		/// Paid Asset Store package. Cannot be purchased or downloaded programmatically. If the user
		/// already owns it and Unity has cached the .unitypackage, it can be imported; otherwise all
		/// we can do is open the store page.
		/// </summary>
		AssetStore
	}

	/// <summary>One package NMJ ToolBox knows how to detect and optionally install.</summary>
	public class PackageEntry
	{
		/// <summary>UPM package name, e.g. <c>com.cysharp.unitask</c>. Empty for Asset Store drops.</summary>
		public string packageId = string.Empty;

		public string displayName = string.Empty;

		public InstallSource source;

		/// <summary>Git URL or registry version, depending on <see cref="source"/>.</summary>
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

		public string notes = string.Empty;
	}

	/// <summary>
	/// The default package set, as a plain data table, so adding one is a single entry rather than a
	/// code change.
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

		private static readonly List<PackageEntry> entries = new()
		{
			// ---------- Free / open source ----------
			new PackageEntry
			{
				packageId = "com.unity.nuget.newtonsoft-json",
				displayName = "Newtonsoft Json",
				source = InstallSource.Registry,
				identifier = "com.unity.nuget.newtonsoft-json",
				url = "https://docs.unity3d.com/Packages/com.unity.nuget.newtonsoft-json@latest",
				define = "NMJ_NEWTONSOFT",
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
				identifier = "https://github.com/Cysharp/UniTask.git?path=src/UniTask/Assets/Plugins/UniTask",
				url = "https://github.com/Cysharp/UniTask",
				define = "NMJ_UNITASK",
				assemblies = new[] { "UniTask" },
				notes = "Enables the allocation-free async pooling and scene-loading helpers."
			},
			new PackageEntry
			{
				packageId = "com.annulusgames.lit-motion",
				displayName = "LitMotion",
				source = InstallSource.Git,
				identifier = "https://github.com/annulusgames/LitMotion.git?path=src/LitMotion/Assets/LitMotion",
				url = "https://github.com/annulusgames/LitMotion",
				define = "NMJ_LITMOTION",
				assemblies = new[] { "LitMotion" },
				notes = "Enables tweened spawn/despawn transitions for pooled objects."
			},
			new PackageEntry
			{
				packageId = "com.man572142.bro-audio",
				displayName = "BroAudio",
				source = InstallSource.Git,
				identifier = "https://github.com/man572142/Bro_Audio.git?path=/Assets/BroAudio",
				url = "https://github.com/man572142/Bro_Audio",
				define = "NMJ_BROAUDIO",
				assemblies = new[] { "Ami.BroAudio", "BroAudio" },
				notes = "No code integration yet — detected so it can be installed in one pass."
			},
			new PackageEntry
			{
				packageId = "com.tayx.graphy",
				displayName = "Graphy",
				source = InstallSource.OpenUpm,
				identifier = "com.tayx.graphy",
				url = "https://openupm.com/packages/com.tayx.graphy/",
				assemblies = new[] { "Tayx.Graphy" }
			},
			new PackageEntry
			{
				packageId = "com.yasirkula.assetusagedetector",
				displayName = "Asset Usage Detector",
				source = InstallSource.OpenUpm,
				identifier = "com.yasirkula.assetusagedetector",
				url = "https://openupm.com/packages/com.yasirkula.assetusagedetector/",
				define = "NMJ_ASSETUSAGEDETECTOR",
				assemblies = new[] { "AssetUsageDetector.Editor", "AssetUsageDetector.Runtime", "AssetUsageDetector" },
				notes = "Adds a Find Usages command to the NMJ prefab tools."
			},
			new PackageEntry
			{
				packageId = "com.yasirkula.runtimeinspector",
				displayName = "Runtime Inspector",
				source = InstallSource.OpenUpm,
				identifier = "com.yasirkula.runtimeinspector",
				url = "https://openupm.com/packages/com.yasirkula.runtimeinspector/",
				assemblies = new[] { "RuntimeInspector.Runtime", "RuntimeInspector" }
			},
			new PackageEntry
			{
				packageId = "com.yasirkula.ingamedebugconsole",
				displayName = "In-Game Debug Console",
				source = InstallSource.OpenUpm,
				identifier = "com.yasirkula.ingamedebugconsole",
				url = "https://openupm.com/packages/com.yasirkula.ingamedebugconsole/",
				assemblies = new[] { "IngameDebugConsole.Runtime", "IngameDebugConsole" }
			},

			// ---------- Paid (Asset Store) ----------
			// These are never installed automatically. Unity exposes no API to purchase or download
			// store content; the most that is possible is importing a .unitypackage the user already
			// owns, and Unity has cached locally. See AssetStoreLocator.
			new PackageEntry
			{
				displayName = "Odin Inspector",
				source = InstallSource.AssetStore,
				url = "https://assetstore.unity.com/packages/tools/utilities/odin-inspector-and-serializer-89041",
				assemblies = new[] { "Sirenix.OdinInspector.Attributes", "Sirenix.OdinInspector.Editor" },
				cacheHints = new[] { "Odin Inspector" },
				notes = "Defines ODIN_INSPECTOR itself. While present, NMJ's own [ReadOnly]/[Required]/[Button] " +
						"stay switched off so the two cannot collide."
			},
			new PackageEntry
			{
				displayName = "Init(args)",
				source = InstallSource.AssetStore,
				url = "https://assetstore.unity.com/packages/tools/utilities/init-args-a-next-gen-di-framework-200530",
				define = "NMJ_INITARGS",
				assemblies = new[] { "Sisus.Init", "Sisus.Init.Editor" },
				types = new[] { "Sisus.Init.InitArgs" },
				cacheHints = new[] { "Init(args)", "InitArgs" }
			},
			new PackageEntry
			{
				displayName = "vHierarchy 2",
				source = InstallSource.AssetStore,
				url = "https://assetstore.unity.com/packages/tools/utilities/vhierarchy-2-253397",
				assemblies = new[] { "VHierarchy", "VHierarchy.Editor" },
				types = new[] { "VHierarchy2.VHierarchy", "VHierarchy.VHierarchy" },
				cacheHints = new[] { "vHierarchy" }
			},
			new PackageEntry
			{
				displayName = "vInspector 2",
				source = InstallSource.AssetStore,
				url = "https://assetstore.unity.com/packages/tools/utilities/vinspector-2-252297",
				assemblies = new[] { "VInspector", "VInspector.Editor" },
				types = new[] { "VInspector2.VInspector", "VInspector.VInspector" },
				cacheHints = new[] { "vInspector" }
			},
			new PackageEntry
			{
				displayName = "vFolders 2",
				source = InstallSource.AssetStore,
				url = "https://assetstore.unity.com/packages/tools/utilities/vfolders-2-255470",
				assemblies = new[] { "VFolders", "VFolders.Editor" },
				cacheHints = new[] { "vFolders" }
			},
			new PackageEntry
			{
				displayName = "vTabs 2",
				source = InstallSource.AssetStore,
				url = "https://assetstore.unity.com/packages/tools/utilities/vtabs-2-253396",
				assemblies = new[] { "VTabs", "VTabs.Editor" },
				cacheHints = new[] { "vTabs" }
			},
			new PackageEntry
			{
				displayName = "vRuler",
				source = InstallSource.AssetStore,
				url = "https://assetstore.unity.com/packages/tools/utilities/vruler-249760",
				assemblies = new[] { "VRuler", "VRuler.Editor" },
				cacheHints = new[] { "vRuler" }
			},
			new PackageEntry
			{
				displayName = "Editor Console Pro",
				source = InstallSource.AssetStore,
				url = "https://assetstore.unity.com/packages/tools/utilities/editor-console-pro-11889",
				types = new[] { "FlyingWormConsole3.ConsolePro3Window" },
				cacheHints = new[] { "Editor Console Pro" }
			},
			new PackageEntry
			{
				displayName = "Hot Reload",
				source = InstallSource.AssetStore,
				url = "https://assetstore.unity.com/packages/p/hot-reload-edit-code-without-compiling-254358",
				assemblies = new[] { "SingularityGroup.HotReload", "SingularityGroup.HotReload.Editor" },
				cacheHints = new[] { "Hot Reload" }
			}
		};
	}
}