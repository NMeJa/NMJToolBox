using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace NMJTools.Scaffolding
{
	/// <summary>What a scaffold run actually did, so the caller can report it in one line.</summary>
	public class ScaffoldResult
	{
		public readonly List<string> Created = new();
		public readonly List<string> AlreadyExisted = new();
		public readonly List<string> Failed = new();
		public int GitKeepsWritten;
		public int AssemblyDefinitionsCreated;
		public bool ReadmeWritten;

		public string Summary()
		{
			var parts = new List<string> { $"{Created.Count} folder(s) created" };
			if (AlreadyExisted.Count > 0) parts.Add($"{AlreadyExisted.Count} already existed");
			if (Failed.Count > 0) parts.Add($"{Failed.Count} FAILED");
			if (GitKeepsWritten > 0) parts.Add($"{GitKeepsWritten} .gitkeep");
			if (AssemblyDefinitionsCreated > 0) parts.Add($"{AssemblyDefinitionsCreated} asmdef");
			if (ReadmeWritten) parts.Add("README.md");
			return string.Join(", ", parts);
		}
	}

	/// <summary>
	/// Creates a folder structure from a <see cref="FolderLayout"/>.
	///
	/// Driven by <see cref="FolderStructureWindow"/>; exposed publicly so it can also be scripted.
	/// </summary>
	public static class FolderStructureCreator
	{
		/// <summary>
		/// Every folder the layout implies, ancestors included, ordered so a parent always precedes
		/// its children.
		///
		/// Expanding ancestors up front is what lets a template list only its leaves
		/// ("Runtime/Art/Textures") and removes the need for the recursive create-parent-first dance
		/// entirely — along with its habit of throwing when a path contains no separator.
		/// </summary>
		public static List<string> ResolvePaths(FolderLayout layout, string targetParent)
		{
			var root = FolderLayout.SanitiseFolderName(layout.rootFolderName);
			var parent = string.IsNullOrWhiteSpace(targetParent) ? "Assets" : targetParent.TrimEnd('/');

			var basePath = string.IsNullOrEmpty(root) ? parent : $"{parent}/{root}";

			var all = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { basePath };

			foreach (var relative in layout.NormalisedFolders)
			{
				var segments = relative.Split('/');
				var accumulated = basePath;

				foreach (var segment in segments)
				{
					var safe = FolderLayout.SanitiseFolderName(segment);
					if (safe == null) break;
					accumulated += "/" + safe;
					all.Add(accumulated);
				}
			}

			// Shallowest first: guarantees every parent exists by the time its child is created.
			return all.OrderBy(p => p.Count(c => c == '/')).ThenBy(p => p, StringComparer.Ordinal).ToList();
		}

		public static bool Exists(string assetPath) => AssetDatabase.IsValidFolder(assetPath);

		/// <summary>Runs the layout. Returns what happened rather than logging as it goes.</summary>
		public static ScaffoldResult Create(FolderLayout layout, string targetParent)
		{
			var result = new ScaffoldResult();
			var paths = ResolvePaths(layout, targetParent);

			// One asset-database transaction for the whole batch instead of a refresh per folder.
			AssetDatabase.StartAssetEditing();
			try
			{
				foreach (var path in paths)
				{
					if (AssetDatabase.IsValidFolder(path))
					{
						result.AlreadyExisted.Add(path);
						continue;
					}

					var lastSlash = path.LastIndexOf('/');
					if (lastSlash <= 0)
					{
						result.Failed.Add(path);
						continue;
					}

					var parent = path[..lastSlash];
					var folderName = path[(lastSlash + 1)..];

					// CreateFolder returns the new folder's GUID, or an empty string on failure —
					// discarding it is how a silent no-op looks like success.
					var guid = AssetDatabase.CreateFolder(parent, folderName);
					if (string.IsNullOrEmpty(guid)) result.Failed.Add(path);
					else result.Created.Add(path);
				}
			}
			finally
			{
				AssetDatabase.StopAssetEditing();
			}

			AssetDatabase.Refresh();

			// The rest needs the folders to exist on disk, so it runs after the transaction closes.
			if (layout.createGitKeep) result.GitKeepsWritten = WriteGitKeeps(paths);
			if (layout.createReadme) result.ReadmeWritten = WriteReadme(layout, paths.First());
			if (layout.createAssemblyDefinitions) result.AssemblyDefinitionsCreated = WriteAssemblyDefinitions(layout, paths);

			if (layout.createGitKeep || layout.createReadme || layout.createAssemblyDefinitions)
				AssetDatabase.Refresh();

			return result;
		}

		/// <summary>
		/// Drops a .gitkeep into every folder.
		///
		/// Git cannot track an empty directory. Unity writes the folder's .meta beside it, so a
		/// commit carries "Textures.meta" but not "Textures/" — on clone Unity finds an orphaned
		/// meta and deletes it, and the scaffold quietly evaporates for everyone but its author.
		/// A dot-file is invisible to the AssetDatabase, so it never gets a .meta of its own.
		/// </summary>
		private static int WriteGitKeeps(IEnumerable<string> assetPaths)
		{
			var written = 0;

			foreach (var assetPath in assetPaths)
			{
				try
				{
					var absolute = ToAbsolute(assetPath);
					if (!Directory.Exists(absolute)) continue;

					var keep = Path.Combine(absolute, ".gitkeep");
					if (File.Exists(keep)) continue;

					File.WriteAllText(keep, string.Empty);
					written++;
				}
				catch (Exception e)
				{
					Debug.LogWarning($"[NMJ] Could not write .gitkeep in '{assetPath}': {e.Message}");
				}
			}

			return written;
		}

		private static bool WriteReadme(FolderLayout layout, string rootAssetPath)
		{
			var path = Path.Combine(ToAbsolute(rootAssetPath), "README.md");
			if (File.Exists(path)) return false;

			var text = new StringBuilder();
			text.AppendLine($"# {Path.GetFileName(rootAssetPath)}");
			text.AppendLine();
			text.AppendLine($"Folder structure generated from the **{layout.layoutName}** template.");

			if (!string.IsNullOrWhiteSpace(layout.description))
			{
				text.AppendLine();
				text.AppendLine(layout.description);
			}

			text.AppendLine();
			text.AppendLine("```");
			foreach (var folder in layout.NormalisedFolders.OrderBy(f => f, StringComparer.Ordinal))
				text.AppendLine(folder);
			text.AppendLine("```");
			text.AppendLine();
			text.AppendLine("Each folder carries a `.gitkeep` so it survives a clone — git cannot track an");
			text.AppendLine("empty directory, and without one only the orphaned `.meta` would arrive.");

			try
			{
				File.WriteAllText(path, text.ToString());
				return true;
			}
			catch (Exception e)
			{
				Debug.LogWarning($"[NMJ] Could not write README.md: {e.Message}");
				return false;
			}
		}

		/// <summary>
		/// Writes a Runtime and an Editor assembly definition, with the editor one referencing the
		/// runtime one <em>by GUID</em>.
		///
		/// By name would be one typo away from the "references non-existent assemblies" error, which
		/// does not degrade — it stops the whole assembly compiling. The GUID is read back from the
		/// runtime asmdef after importing it, so it cannot be mistyped.
		/// </summary>
		private static int WriteAssemblyDefinitions(FolderLayout layout, IReadOnlyList<string> paths)
		{
			var rootPath = paths.First();
			var rootName = FolderLayout.SanitiseFolderName(Path.GetFileName(rootPath))?.TrimStart('_');
			if (string.IsNullOrEmpty(rootName)) rootName = "Game";

			var runtimeFolder = paths.FirstOrDefault(p => p.EndsWith("/Runtime", StringComparison.OrdinalIgnoreCase));
			var editorFolder = paths.FirstOrDefault(p => p.EndsWith("/Editor", StringComparison.OrdinalIgnoreCase));

			if (runtimeFolder == null && editorFolder == null)
			{
				Debug.LogWarning("[NMJ] Assembly definitions were requested but the layout has no " +
								 "Runtime or Editor folder, so none were created.");
				return 0;
			}

			var created = 0;
			string runtimeGuid = null;

			if (runtimeFolder != null)
			{
				var path = $"{runtimeFolder}/{rootName}.Runtime.asmdef";
				if (WriteJson(path, BuildAsmdef($"{rootName}.Runtime", rootName, false, null)))
				{
					created++;
					AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
					runtimeGuid = AssetDatabase.AssetPathToGUID(path);
				}
			}

			if (editorFolder != null)
			{
				var path = $"{editorFolder}/{rootName}.Editor.asmdef";
				if (WriteJson(path, BuildAsmdef($"{rootName}.Editor", $"{rootName}.Editor", true, runtimeGuid)))
				{
					created++;
					AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
				}
			}

			return created;
		}

		private static string BuildAsmdef(string name, string rootNamespace, bool editorOnly, string referenceGuid)
		{
			var references = string.IsNullOrEmpty(referenceGuid) ? "" : $"\"GUID:{referenceGuid}\"";
			var platforms = editorOnly ? "\"Editor\"" : "";

			return "{\n" +
				   $"    \"name\": \"{name}\",\n" +
				   $"    \"rootNamespace\": \"{rootNamespace}\",\n" +
				   $"    \"references\": [{references}],\n" +
				   $"    \"includePlatforms\": [{platforms}],\n" +
				   "    \"excludePlatforms\": [],\n" +
				   "    \"allowUnsafeCode\": false,\n" +
				   "    \"overrideReferences\": false,\n" +
				   "    \"precompiledReferences\": [],\n" +
				   "    \"autoReferenced\": true,\n" +
				   "    \"defineConstraints\": [],\n" +
				   "    \"versionDefines\": [],\n" +
				   "    \"noEngineReferences\": false\n" +
				   "}\n";
		}

		private static bool WriteJson(string assetPath, string contents)
		{
			var absolute = ToAbsolute(assetPath);
			if (File.Exists(absolute)) return false;

			try
			{
				File.WriteAllText(absolute, contents);
				return true;
			}
			catch (Exception e)
			{
				Debug.LogWarning($"[NMJ] Could not write '{assetPath}': {e.Message}");
				return false;
			}
		}

		/// <summary>"Assets/Foo" to a real filesystem path. Application.dataPath already ends in "Assets".</summary>
		private static string ToAbsolute(string assetPath)
		{
			var projectRoot = Directory.GetParent(Application.dataPath)?.FullName ?? Application.dataPath;
			return Path.Combine(projectRoot, assetPath.Replace('/', Path.DirectorySeparatorChar));
		}
	}
}
