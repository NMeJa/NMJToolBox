using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.PackageManager;
using UnityEditor.PackageManager.Requests;
using UnityEngine;

namespace NMJTools.Setup
{
	/// <summary>
	/// One window to bring a fresh project up to the standard package set.
	///
	/// What it can and cannot do, plainly: UPM packages (git and OpenUPM) install without any further
	/// input. Paid Asset Store packages cannot — Unity has no API to buy or download store content
	/// because entitlement is checked server-side against the signed-in account. For those, the
	/// window looks for a copy already in the per-user Asset Store cache and imports that; failing
	/// that, it opens the store page. Nothing here can conjure a license.
	/// </summary>
	public class ProjectSetupWindow : EditorWindow
	{
		private Vector2 scroll;
		private AddRequest activeRequest;
		private readonly Queue<PackageEntry> installQueue = new();
		private string statusMessage = string.Empty;
		private bool includeOptional = true;

		[MenuItem("Tools/NMJ/Setup/Project Setup", false, 290)]
		public static void Open()
		{
			var window = GetWindow<ProjectSetupWindow>(true, "NMJ Project Setup");
			window.minSize = new Vector2(560, 520);
			AssetStoreLocator.Refresh();
			window.Show();
		}

		private void OnGUI()
		{
			DrawHeader();

			scroll = EditorGUILayout.BeginScrollView(scroll);

			DrawSection("Free / open source", InstallSource.Git, InstallSource.OpenUpm);
			DrawSection("Paid — Asset Store", InstallSource.AssetStore);

			EditorGUILayout.EndScrollView();

			DrawFooter();
		}

		private static void DrawHeader()
		{
			EditorGUILayout.Space(6);
			EditorGUILayout.LabelField("Standard Project Packages", EditorStyles.boldLabel);

			EditorGUILayout.HelpBox(
									"Free and OpenUPM packages install automatically.\n\n" +
									"Paid Asset Store packages cannot be installed by script — Unity checks entitlement " +
									"server-side. If you already downloaded one on this machine it is imported from the " +
									"Asset Store cache; otherwise the store page opens.",
									MessageType.Info);

			using (new EditorGUILayout.HorizontalScope())
			{
				PackageDetector.AutoManage = EditorGUILayout.ToggleLeft(
																		new GUIContent("Auto-manage define symbols",
																					   "Keep NMJ_* defines in sync with what is installed, after every recompile."),
																		PackageDetector.AutoManage);

				if (GUILayout.Button("Re-scan", GUILayout.Width(80))) PackageDetector.Sync(true);
			}

			var odin = PackageCatalog.All.FirstOrDefault(e => e.displayName == "Odin Inspector");
			if (odin != null && PackageDetector.IsPresent(odin))
			{
				EditorGUI.BeginChangeCheck();
				PackageDetector.ForceAttributesWithOdin = EditorGUILayout.ToggleLeft(
																					 new GUIContent("Force NMJ attributes alongside Odin",
																									"Both packages define [ReadOnly], [Required] and [Button]. Enabling this makes " +
																									"those names ambiguous wherever both namespaces are imported."),
																					 PackageDetector.ForceAttributesWithOdin);

				if (EditorGUI.EndChangeCheck()) PackageDetector.Sync(true);
			}

			EditorGUILayout.Space(4);
		}

		private void DrawSection(string titleLabel, params InstallSource[] sources)
		{
			var entries = PackageCatalog.All.Where(e => sources.Contains(e.source)).ToList();
			if (entries.Count == 0) return;

			EditorGUILayout.Space(6);
			EditorGUILayout.LabelField(titleLabel, EditorStyles.boldLabel);

			foreach (var entry in entries) DrawEntry(entry);
		}

		private void DrawEntry(PackageEntry entry)
		{
			var present = PackageDetector.IsPresent(entry);

			using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
			{
				using (new EditorGUILayout.HorizontalScope())
				{
					var icon = present ? "✓" : "•";
					var style = new GUIStyle(EditorStyles.label)
					{
						normal = { textColor = present ? new Color(0.45f, 0.85f, 0.45f) : EditorStyles.label.normal.textColor },
						fontStyle = FontStyle.Bold
					};

					EditorGUILayout.LabelField($"{icon}  {entry.displayName}", style);
					GUILayout.FlexibleSpace();

					if (!string.IsNullOrEmpty(entry.define))
						EditorGUILayout.LabelField(entry.define, EditorStyles.miniLabel, GUILayout.Width(190));

					DrawEntryButton(entry, present);
				}

				if (!string.IsNullOrEmpty(entry.notes))
					EditorGUILayout.LabelField(entry.notes, EditorStyles.wordWrappedMiniLabel);

				if (entry.source != InstallSource.AssetStore || present) return;

				var cachedPath = AssetStoreLocator.FindCachedPackage(entry);
				EditorGUILayout.LabelField(
										   cachedPath != null
											   ? "Found in Asset Store cache — can be imported."
											   : "Not in the Asset Store cache. Download it once via Package Manager > My Assets.",
										   EditorStyles.miniLabel);
			}
		}

		private void DrawEntryButton(PackageEntry entry, bool present)
		{
			if (present)
			{
				using (new EditorGUI.DisabledScope(true)) GUILayout.Button("Installed", GUILayout.Width(90));
				return;
			}

			using (new EditorGUI.DisabledScope(activeRequest != null))
			{
				switch (entry.source)
				{
					case InstallSource.Git:
					case InstallSource.OpenUpm:
						if (GUILayout.Button("Install", GUILayout.Width(90))) Enqueue(new[] { entry });
						break;

					case InstallSource.AssetStore:
						var cachedPath = AssetStoreLocator.FindCachedPackage(entry);
						if (cachedPath != null)
						{
							if (GUILayout.Button("Import", GUILayout.Width(90)))
								AssetDatabase.ImportPackage(cachedPath, true);
						}
						else if (GUILayout.Button("Store Page", GUILayout.Width(90)))
						{
							Application.OpenURL(entry.url);
						}

						break;
				}
			}
		}

		private void DrawFooter()
		{
			EditorGUILayout.Space(4);

			if (!string.IsNullOrEmpty(statusMessage))
				EditorGUILayout.HelpBox(statusMessage, MessageType.None);

			includeOptional = EditorGUILayout.ToggleLeft(
														 "Also open store pages for missing paid packages",
														 includeOptional);

			using (new EditorGUILayout.HorizontalScope())
			{
				GUILayout.FlexibleSpace();

				using (new EditorGUI.DisabledScope(activeRequest != null))
				{
					if (GUILayout.Button("Install All Free Packages", GUILayout.Height(28), GUILayout.Width(220)))
						InstallEverything();
				}
			}

			if (activeRequest == null) return;
			EditorGUILayout.LabelField("Installing…", EditorStyles.miniLabel);
		}

		private void InstallEverything()
		{
			var missing = PackageCatalog.All
										.Where(e => e.source != InstallSource.AssetStore)
										.Where(e => !PackageDetector.IsPresent(e))
										.ToList();

			if (missing.Count == 0)
			{
				statusMessage = "Every free package is already installed.";
			}
			else
			{
				// The OpenUPM scoped registry has to exist in manifest.json before Client.Add can
				// resolve a com.tayx.* or com.yasirkula.* name, so it is written first.
				if (missing.Any(e => e.source == InstallSource.OpenUpm)) EnsureOpenUpmRegistry(missing);
				Enqueue(missing);
			}

			if (!includeOptional) return;

			var paid = PackageCatalog.All
									 .Where(e => e.source == InstallSource.AssetStore)
									 .Where(e => !PackageDetector.IsPresent(e))
									 .ToList();

			foreach (var entry in paid)
			{
				var cachedPath = AssetStoreLocator.FindCachedPackage(entry);
				if (cachedPath != null) AssetDatabase.ImportPackage(cachedPath, true);
				else Application.OpenURL(entry.url);
			}
		}

		private void Enqueue(IEnumerable<PackageEntry> entries)
		{
			foreach (var entry in entries) installQueue.Enqueue(entry);
			ProcessQueue();
		}

		private void ProcessQueue()
		{
			if (activeRequest != null || installQueue.Count == 0) return;

			var entry = installQueue.Dequeue();
			statusMessage = $"Installing {entry.displayName}…";

			activeRequest = Client.Add(entry.identifier);
			EditorApplication.update += PollRequest;
		}

		private void PollRequest()
		{
			if (activeRequest is not { IsCompleted: true }) return;

			EditorApplication.update -= PollRequest;

			statusMessage = activeRequest.Status == StatusCode.Success
								? $"Installed {activeRequest.Result.displayName} {activeRequest.Result.version}."
								: $"Failed: {activeRequest.Error?.message}";

			if (activeRequest.Status != StatusCode.Success)
				Debug.LogError($"[NMJ] Package install failed: {activeRequest.Error?.message}");

			activeRequest = null;
			Repaint();

			ProcessQueue();
		}

		/// <summary>
		/// Adds the OpenUPM scoped registry to <c>Packages/manifest.json</c> if it is not already
		/// there, merging scopes rather than replacing them so an existing OpenUPM entry from another
		/// package survives.
		/// </summary>
		private void EnsureOpenUpmRegistry(IEnumerable<PackageEntry> entries)
		{
			var scopes = entries
						 .Where(e => e.source == InstallSource.OpenUpm)
						 .Select(e => ScopeOf(e.identifier))
						 .Where(s => !string.IsNullOrEmpty(s))
						 .Distinct()
						 .ToList();

			if (scopes.Count == 0) return;

			var manifestPath = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "Packages", "manifest.json"));
			if (!File.Exists(manifestPath))
			{
				Debug.LogError($"[NMJ] Could not find manifest.json at '{manifestPath}'.");
				return;
			}

			var text = File.ReadAllText(manifestPath);

			// Hand-editing JSON is unpleasant, but Unity ships no writable manifest API, and
			// JsonUtility cannot round-trip the arbitrary shape of a manifest without dropping keys.
			// Bailing out with a clear message beats silently corrupting the file.
			if (text.Contains(PackageCatalog.OpenUpmRegistryUrl))
			{
				statusMessage = "OpenUPM registry already present in manifest.json.";
				return;
			}

			var scopeList = string.Join(",\n        ", scopes.Select(s => $"\"{s}\""));
			var registryBlock =
				"  \"scopedRegistries\": [\n" +
				"    {\n" +
				$"      \"name\": \"{PackageCatalog.OpenUpmRegistryName}\",\n" +
				$"      \"url\": \"{PackageCatalog.OpenUpmRegistryUrl}\",\n" +
				"      \"scopes\": [\n" +
				$"        {scopeList}\n" +
				"      ]\n" +
				"    }\n" +
				"  ],\n";

			if (text.Contains("\"scopedRegistries\""))
			{
				Debug.LogWarning("[NMJ] manifest.json already has a scopedRegistries block, but not OpenUPM. " +
								 "Add the OpenUPM registry by hand to avoid clobbering the existing one:\n" +
								 registryBlock);
				statusMessage = "Add the OpenUPM registry manually — see the Console.";
				return;
			}

			var insertAt = text.IndexOf('{');
			if (insertAt < 0)
			{
				Debug.LogError("[NMJ] manifest.json is not valid JSON.");
				return;
			}

			text = text.Insert(insertAt + 1, "\n" + registryBlock);
			File.WriteAllText(manifestPath, text);
			Client.Resolve();

			Debug.Log("[NMJ] Added the OpenUPM scoped registry to manifest.json.");
		}

		/// <summary>com.yasirkula.runtimeinspector → com.yasirkula (OpenUPM scopes are per-publisher).</summary>
		private static string ScopeOf(string packageId)
		{
			if (string.IsNullOrEmpty(packageId)) return string.Empty;
			var parts = packageId.Split('.');
			return parts.Length >= 2 ? $"{parts[0]}.{parts[1]}" : packageId;
		}

		private void OnDisable() => EditorApplication.update -= PollRequest;
	}
}