using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace NMJTools.Scaffolding
{
	/// <summary>
	/// A named folder structure, plus the options that decide what else gets written alongside it.
	///
	/// Plain serializable data rather than a ScriptableObject, so the same type describes both the
	/// built-in layouts (which ship as code and need no assets) and the ones a project saves to disk
	/// as a <see cref="FolderTemplate"/>.
	/// </summary>
	[Serializable]
	public class FolderLayout
	{
		public string layoutName = "Custom";

		[TextArea(1, 3)]
		public string description = string.Empty;

		[Tooltip("Folder created under the target path. Everything else nests inside it.")]
		public string rootFolderName = "__Game";

		[Tooltip("Paths relative to the root folder. Parent folders are created automatically, so " +
				 "listing 'Runtime/Art/Textures' alone is enough.")]
		public List<string> folders = new();

		[Tooltip("Write a .gitkeep into every folder. Without it, empty folders do not survive a " +
				 "git clone - git cannot track an empty directory, so only the orphaned .meta " +
				 "arrives and Unity deletes it.")]
		public bool createGitKeep = true;

		[Tooltip("Generate <Root>.Runtime and <Root>.Editor assembly definitions, with the editor " +
				 "one referencing the runtime one by GUID.")]
		public bool createAssemblyDefinitions;

		[Tooltip("Write a README.md in the root folder describing the layout.")]
		public bool createReadme;

		public FolderLayout Clone() => new()
		{
			layoutName = layoutName,
			description = description,
			rootFolderName = rootFolderName,
			folders = new List<string>(folders),
			createGitKeep = createGitKeep,
			createAssemblyDefinitions = createAssemblyDefinitions,
			createReadme = createReadme
		};

		/// <summary>
		/// Folder paths cleaned up and de-duplicated: separators normalised, blanks and stray
		/// slashes dropped. Windows habits produce backslashes and trailing slashes, and both make
		/// <see cref="UnityEditor.AssetDatabase"/> calls fail silently rather than loudly.
		/// </summary>
		public IEnumerable<string> NormalisedFolders =>
			folders.Select(Normalise)
				   .Where(f => !string.IsNullOrEmpty(f))
				   .Distinct(StringComparer.OrdinalIgnoreCase);

		public static string Normalise(string path)
		{
			if (string.IsNullOrWhiteSpace(path)) return string.Empty;

			var parts = path.Replace('\\', '/')
							.Split('/')
							.Select(p => p.Trim())
							.Where(p => p.Length > 0 && p != "." && p != "..");

			return string.Join("/", parts);
		}

		/// <summary>
		/// Strips characters a folder name cannot contain. Returns null when nothing usable is left,
		/// which the window treats as "cannot create yet" rather than guessing a name.
		/// </summary>
		public static string SanitiseFolderName(string name)
		{
			if (string.IsNullOrWhiteSpace(name)) return null;

			var invalid = System.IO.Path.GetInvalidFileNameChars();
			var cleaned = new string(name.Trim().Where(c => !invalid.Contains(c) && c != '/' && c != '\\').ToArray());

			return string.IsNullOrWhiteSpace(cleaned) ? null : cleaned;
		}
	}
}
