using System.Collections.Generic;

namespace NMJToolBox.Setup
{
	/// <summary>How a package reaches the project.</summary>
	public enum InstallSource
	{
		/// <summary>UPM git URL — installable straight from <c>Client.Add</c>.</summary>
		Git,

		/// <summary>OpenUPM registry package — needs the scoped registry in manifest.json first.</summary>
		OpenUpm,

		/// <summary>
		/// Paid Asset Store package. Cannot be purchased or downloaded programmatically. If the user
		/// already owns it and Unity has cached the .unitypackage, it can be imported; otherwise all
		/// we can do is open the store page.
		/// </summary>
		AssetStore
	}

	/// <summary>One package NMJ ToolBox knows how to detect, and optionally install.</summary>
	public class PackageEntry
	{
		/// <summary>UPM package name, e.g. <c>com.cysharp.unitask</c>. Empty for Asset Store drops.</summary>
		public string PackageId = string.Empty;

		public string DisplayName = string.Empty;

		public InstallSource Source;

		/// <summary>Git URL or registry version, depending on <see cref="Source"/>.</summary>
		public string Identifier = string.Empty;

		/// <summary>Store or repository page, opened when automatic install is not possible.</summary>
		public string Url = string.Empty;

		/// <summary>
		/// Scripting define symbol added while this package is present. Empty means "detect but do
		/// not define" — used for packages that need no code integration.
		/// </summary>
		public string Define = string.Empty;

		/// <summary>
		/// Assembly names that prove this package is present. Matched against loaded assembly simple
		/// names. Assembly detection is used rather than asmdef <c>versionDefines</c> because it works
		/// identically for UPM packages and for Asset Store content dropped into <c>Assets/</c>,
		/// which is not a package at all and has no version to bind to.
		/// </summary>
		public string[] Assemblies = System.Array.Empty<string>();

		/// <summary>
		/// Fallback detection for assets with no assembly definition — their code lands in
		/// <c>Assembly-CSharp-Editor</c>, so the assembly name proves nothing and a known type must
		/// be looked for instead.
		/// </summary>
		public string[] Types = System.Array.Empty<string>();

		/// <summary>Substrings matched against cached .unitypackage paths, for Asset Store entries.</summary>
		public string[] CacheHints = System.Array.Empty<string>();

		public string Notes = string.Empty;
	}

	/// <summary>
	/// The default package set, as a plain data table so adding one is a single entry rather than a
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

		public static IReadOnlyList<PackageEntry> All => Entries;

		private static readonly List<PackageEntry> Entries = new()
		{
			// ---------- Free / open source ----------
			new PackageEntry
			{
				PackageId = "com.cysharp.unitask",
				DisplayName = "UniTask",
				Source = InstallSource.Git,
				Identifier = "https://github.com/Cysharp/UniTask.git?path=src/UniTask/Assets/Plugins/UniTask",
				Url = "https://github.com/Cysharp/UniTask",
				Define = "NMJ_UNITASK",
				Assemblies = new[] { "UniTask" },
				Notes = "Enables the allocation-free async pooling and scene-loading helpers."
			},
			new PackageEntry
			{
				PackageId = "com.annulusgames.lit-motion",
				DisplayName = "LitMotion",
				Source = InstallSource.Git,
				Identifier = "https://github.com/annulusgames/LitMotion.git?path=src/LitMotion/Assets/LitMotion",
				Url = "https://github.com/annulusgames/LitMotion",
				Define = "NMJ_LITMOTION",
				Assemblies = new[] { "LitMotion" },
				Notes = "Enables tweened spawn/despawn transitions for pooled objects."
			},
			new PackageEntry
			{
				PackageId = "com.man572142.bro-audio",
				DisplayName = "BroAudio",
				Source = InstallSource.Git,
				Identifier = "https://github.com/man572142/Bro_Audio.git?path=/Assets/BroAudio",
				Url = "https://github.com/man572142/Bro_Audio",
				Define = "NMJ_BROAUDIO",
				Assemblies = new[] { "Ami.BroAudio", "BroAudio" },
				Notes = "No code integration yet — detected so it can be installed in one pass."
			},
			new PackageEntry
			{
				PackageId = "com.tayx.graphy",
				DisplayName = "Graphy",
				Source = InstallSource.OpenUpm,
				Identifier = "com.tayx.graphy",
				Url = "https://openupm.com/packages/com.tayx.graphy/",
				Assemblies = new[] { "Tayx.Graphy" }
			},
			new PackageEntry
			{
				PackageId = "com.yasirkula.assetusagedetector",
				DisplayName = "Asset Usage Detector",
				Source = InstallSource.OpenUpm,
				Identifier = "com.yasirkula.assetusagedetector",
				Url = "https://openupm.com/packages/com.yasirkula.assetusagedetector/",
				Define = "NMJ_ASSETUSAGEDETECTOR",
				Assemblies = new[] { "AssetUsageDetector.Editor", "AssetUsageDetector.Runtime", "AssetUsageDetector" },
				Notes = "Adds a Find Usages command to the NMJ prefab tools."
			},
			new PackageEntry
			{
				PackageId = "com.yasirkula.runtimeinspector",
				DisplayName = "Runtime Inspector",
				Source = InstallSource.OpenUpm,
				Identifier = "com.yasirkula.runtimeinspector",
				Url = "https://openupm.com/packages/com.yasirkula.runtimeinspector/",
				Assemblies = new[] { "RuntimeInspector.Runtime", "RuntimeInspector" }
			},
			new PackageEntry
			{
				PackageId = "com.yasirkula.ingamedebugconsole",
				DisplayName = "In-Game Debug Console",
				Source = InstallSource.OpenUpm,
				Identifier = "com.yasirkula.ingamedebugconsole",
				Url = "https://openupm.com/packages/com.yasirkula.ingamedebugconsole/",
				Assemblies = new[] { "IngameDebugConsole.Runtime", "IngameDebugConsole" }
			},

			// ---------- Paid (Asset Store) ----------
			// These are never installed automatically. Unity exposes no API to purchase or download
			// store content; the most that is possible is importing a .unitypackage the user already
			// owns and Unity has cached locally. See AssetStoreLocator.
			new PackageEntry
			{
				DisplayName = "Odin Inspector",
				Source = InstallSource.AssetStore,
				Url = "https://assetstore.unity.com/packages/tools/utilities/odin-inspector-and-serializer-89041",
				Assemblies = new[] { "Sirenix.OdinInspector.Attributes", "Sirenix.OdinInspector.Editor" },
				CacheHints = new[] { "Odin Inspector" },
				Notes = "Defines ODIN_INSPECTOR itself. While present, NMJ's own [ReadOnly]/[Required]/[Button] " +
						"stay switched off so the two cannot collide."
			},
			new PackageEntry
			{
				DisplayName = "Init(args)",
				Source = InstallSource.AssetStore,
				Url = "https://assetstore.unity.com/packages/tools/utilities/init-args-a-next-gen-di-framework-200530",
				Define = "NMJ_INITARGS",
				Assemblies = new[] { "Sisus.Init", "Sisus.Init.Editor" },
				Types = new[] { "Sisus.Init.InitArgs" },
				CacheHints = new[] { "Init(args)", "InitArgs" }
			},
			new PackageEntry
			{
				DisplayName = "vHierarchy 2",
				Source = InstallSource.AssetStore,
				Url = "https://assetstore.unity.com/packages/tools/utilities/vhierarchy-2-253397",
				Assemblies = new[] { "VHierarchy", "VHierarchy.Editor" },
				Types = new[] { "VHierarchy2.VHierarchy", "VHierarchy.VHierarchy" },
				CacheHints = new[] { "vHierarchy" }
			},
			new PackageEntry
			{
				DisplayName = "vInspector 2",
				Source = InstallSource.AssetStore,
				Url = "https://assetstore.unity.com/packages/tools/utilities/vinspector-2-252297",
				Assemblies = new[] { "VInspector", "VInspector.Editor" },
				Types = new[] { "VInspector2.VInspector", "VInspector.VInspector" },
				CacheHints = new[] { "vInspector" }
			},
			new PackageEntry
			{
				DisplayName = "vFolders 2",
				Source = InstallSource.AssetStore,
				Url = "https://assetstore.unity.com/packages/tools/utilities/vfolders-2-255470",
				Assemblies = new[] { "VFolders", "VFolders.Editor" },
				CacheHints = new[] { "vFolders" }
			},
			new PackageEntry
			{
				DisplayName = "vTabs 2",
				Source = InstallSource.AssetStore,
				Url = "https://assetstore.unity.com/packages/tools/utilities/vtabs-2-253396",
				Assemblies = new[] { "VTabs", "VTabs.Editor" },
				CacheHints = new[] { "vTabs" }
			},
			new PackageEntry
			{
				DisplayName = "vRuler",
				Source = InstallSource.AssetStore,
				Url = "https://assetstore.unity.com/packages/tools/utilities/vruler-249760",
				Assemblies = new[] { "VRuler", "VRuler.Editor" },
				CacheHints = new[] { "vRuler" }
			},
			new PackageEntry
			{
				DisplayName = "Editor Console Pro",
				Source = InstallSource.AssetStore,
				Url = "https://assetstore.unity.com/packages/tools/utilities/editor-console-pro-11889",
				Types = new[] { "FlyingWormConsole3.ConsolePro3Window" },
				CacheHints = new[] { "Editor Console Pro" }
			},
			new PackageEntry
			{
				DisplayName = "Hot Reload",
				Source = InstallSource.AssetStore,
				Url = "https://assetstore.unity.com/packages/p/hot-reload-edit-code-without-compiling-254358",
				Assemblies = new[] { "SingularityGroup.HotReload", "SingularityGroup.HotReload.Editor" },
				CacheHints = new[] { "Hot Reload" }
			}
		};
	}
}
