using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace NMJTools.Setup
{
	/// <summary>
	/// Reads and edits <c>Packages/manifest.json</c>, for the one thing Unity's package API cannot do:
	/// scoped registries.
	///
	/// <see cref="UnityEditor.PackageManager.Client"/> can add, remove and resolve packages, but has no
	/// method that touches <c>scopedRegistries</c> — so an OpenUPM package cannot be installed by name
	/// until the registry exists in the file. The previous version of this code hand-spliced a JSON
	/// string in and gave up with a console message whenever the manifest already had a
	/// <c>scopedRegistries</c> block, which is the common case on any project that has ever installed
	/// one OpenUPM package. This parses the file properly and merges instead.
	/// </summary>
	public static class PackageManifest
	{
		public static string Path =>
			System.IO.Path.GetFullPath(System.IO.Path.Combine(Application.dataPath, "..", "Packages", "manifest.json"));

		public static bool Exists => File.Exists(Path);

		/// <summary>What a registry merge did, so the caller can report it without re-reading the file.</summary>
		public readonly struct RegistryResult
		{
			public RegistryResult(bool changed, string message, bool failed = false)
			{
				Changed = changed;
				Message = message;
				Failed = failed;
			}

			/// <summary>True when the file was rewritten.</summary>
			public bool Changed { get; }

			public string Message { get; }
			public bool Failed { get; }
		}

		/// <summary>
		/// Package ids listed under <c>dependencies</c>.
		///
		/// This is the answer to "is it installed" for a package whose assemblies have not compiled
		/// yet — immediately after an install, and for any package that ships no assembly definition
		/// at all. <see cref="PackageDetector"/> uses it alongside assembly probing.
		/// </summary>
		public static HashSet<string> ReadDependencies()
		{
			// Cached against the file's write time. The setup window asks this question from OnGUI,
			// which repaints many times a second - re-reading and re-parsing an eighty-entry manifest
			// each time would be a file read per frame for an answer that changes maybe twice a day.
			// Keying on the timestamp rather than a manual flag means an edit made outside the editor
			// is still picked up.
			var stamp = LastWriteTicks();

			if (cachedDependencies != null && stamp == cachedDependencyStamp) return cachedDependencies;

			var result = new HashSet<string>(StringComparer.Ordinal);

			if (TryRead(out var root, out _) && root.TryGet<JsonObject>("dependencies", out var dependencies))
			{
				foreach (var member in dependencies.Members) result.Add(member.Key);
			}

			cachedDependencies = result;
			cachedDependencyStamp = stamp;
			return result;
		}

		private static HashSet<string> cachedDependencies;
		private static long cachedDependencyStamp = -1;

		private static long LastWriteTicks()
		{
			try
			{
				var path = Path;
				return File.Exists(path) ? File.GetLastWriteTimeUtc(path).Ticks : -1;
			}
			catch (IOException)
			{
				return -1;
			}
		}

		/// <summary>
		/// Ensures the OpenUPM registry exists and covers <paramref name="scopes"/>.
		///
		/// Three cases, all of which the old string-splice version got wrong: no scopedRegistries block
		/// at all (create one), an OpenUPM entry that is missing some scopes (union them in, keeping the
		/// existing ones), and someone else's registry already present (leave it entirely alone and add
		/// OpenUPM beside it).
		/// </summary>
		public static RegistryResult EnsureOpenUpmScopes(IEnumerable<string> scopes)
		{
			var wanted = scopes?.Where(s => !string.IsNullOrWhiteSpace(s))
							   .Distinct(StringComparer.Ordinal)
							   .OrderBy(s => s, StringComparer.Ordinal)
							   .ToList()
						 ?? new List<string>();

			if (wanted.Count == 0) return new RegistryResult(false, "No OpenUPM packages selected.");

			if (!TryRead(out var root, out var error))
				return new RegistryResult(false, error, failed: true);

			if (!root.TryGet<JsonArray>("scopedRegistries", out var registries))
			{
				registries = new JsonArray();
				root.SetFirst("scopedRegistries", registries);
			}

			var openUpm = FindOpenUpmRegistry(registries);
			var added = new List<string>();

			if (openUpm == null)
			{
				var created = new JsonObject();
				created.Set("name", new JsonString(PackageCatalog.OpenUpmRegistryName));
				created.Set("url", new JsonString(PackageCatalog.OpenUpmRegistryUrl));

				var scopeArray = new JsonArray();
				foreach (var scope in wanted)
				{
					scopeArray.Add(new JsonString(scope));
					added.Add(scope);
				}

				created.Set("scopes", scopeArray);
				registries.Add(created);
			}
			else
			{
				if (!openUpm.TryGet<JsonArray>("scopes", out var existing))
				{
					existing = new JsonArray();
					openUpm.Set("scopes", existing);
				}

				var present = new HashSet<string>(
												  existing.Items.OfType<JsonString>().Select(s => s.Value),
												  StringComparer.Ordinal);

				foreach (var scope in wanted.Where(scope => !present.Contains(scope)))
				{
					existing.Add(new JsonString(scope));
					added.Add(scope);
				}
			}

			if (added.Count == 0)
				return new RegistryResult(false, "OpenUPM registry already covers every selected package.");

			if (!TryWrite(root, out var writeError))
				return new RegistryResult(false, writeError, failed: true);

			cachedDependencyStamp = -1;

			var verb = openUpm == null ? "Added the OpenUPM scoped registry" : "Extended the OpenUPM registry";
			return new RegistryResult(true, $"{verb} with {added.Count} scope(s): {string.Join(", ", added)}.");
		}

		/// <summary>
		/// The registry entry pointing at OpenUPM, matched on URL rather than name so a registry
		/// someone renamed is still recognised instead of being duplicated.
		/// </summary>
		private static JsonObject FindOpenUpmRegistry(JsonArray registries)
		{
			foreach (var entry in registries.Items.OfType<JsonObject>())
			{
				if (entry["url"] is not JsonString url) continue;

				var value = url.Value.TrimEnd('/');
				if (string.Equals(value, PackageCatalog.OpenUpmRegistryUrl, StringComparison.OrdinalIgnoreCase))
					return entry;
			}

			return null;
		}

		/// <summary>
		/// <c>com.yasirkula.runtimeinspector</c> → <c>com.yasirkula</c>.
		///
		/// OpenUPM scopes are per-publisher by convention, which keeps the manifest short and means a
		/// second package from the same author needs no manifest change at all.
		/// </summary>
		public static string ScopeOf(string packageId)
		{
			if (string.IsNullOrEmpty(packageId)) return string.Empty;
			var parts = packageId.Split('.');
			return parts.Length >= 2 ? $"{parts[0]}.{parts[1]}" : packageId;
		}

		private static bool TryRead(out JsonObject root, out string error)
		{
			root = null;
			error = null;

			var path = Path;

			if (!File.Exists(path))
			{
				error = $"Could not find manifest.json at '{path}'.";
				return false;
			}

			try
			{
				root = JsonValue.Parse(File.ReadAllText(path)) as JsonObject;
			}
			catch (JsonParseException e)
			{
				error = $"manifest.json could not be parsed and was left untouched: {e.Message}";
				return false;
			}
			catch (IOException e)
			{
				error = $"manifest.json could not be read: {e.Message}";
				return false;
			}

			if (root != null) return true;

			error = "manifest.json does not contain a JSON object.";
			return false;
		}

		/// <summary>
		/// Writes through a temporary file and swaps it in.
		///
		/// A half-written manifest is a project that will not open, and this runs while Unity may be
		/// watching the folder — so the file is only ever replaced by a complete one.
		/// </summary>
		private static bool TryWrite(JsonValue root, out string error)
		{
			error = null;
			var path = Path;
			var temporary = path + ".nmj.tmp";

			try
			{
				File.WriteAllText(temporary, root.ToJson());
				File.Copy(temporary, path, true);
				File.Delete(temporary);
				return true;
			}
			catch (IOException e)
			{
				error = $"manifest.json could not be written: {e.Message}";
				TryDelete(temporary);
				return false;
			}
			catch (UnauthorizedAccessException e)
			{
				error = $"manifest.json is read-only or locked: {e.Message}";
				TryDelete(temporary);
				return false;
			}
		}

		private static void TryDelete(string path)
		{
			try
			{
				if (File.Exists(path)) File.Delete(path);
			}
			catch (Exception)
			{
				// Nothing useful to do about a stray temp file, and it must not mask the real error.
			}
		}

		[MenuItem("Tools/NMJ/Setup/Open manifest.json", false, 320)]
		private static void OpenManifest()
		{
			if (!Exists)
			{
				Debug.LogError($"[NMJ] No manifest.json at '{Path}'.");
				return;
			}

			EditorUtility.OpenWithDefaultApp(Path);
		}
	}
}
