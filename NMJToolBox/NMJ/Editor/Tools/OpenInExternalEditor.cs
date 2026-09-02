using System.IO;
using System.Linq;
using Unity.CodeEditor;
using UnityEditor;
using UnityEngine;

namespace NMJTools.Tools
{
	/// <summary>
	/// Opens the selected asset in whatever external editor the project is configured to use.
	/// </summary>
	/// <remarks>
	/// this changed: replaces <c>OpenWithVSCode.cs</c>, which hardcoded <c>code</c> as the executable
	/// and shelled out to it directly. Three problems with that: it ignored the editor actually
	/// configured in Preferences ▸ External Tools (Rider, here), it sat in the global namespace inside
	/// a package, and it silently did nothing on any machine where VS Code was not on PATH.
	///
	/// <see cref="CodeEditor.CurrentEditor"/> is the supported way to ask "what is this project's
	/// editor" and hands the file to it with the right arguments per editor, so Rider opens the
	/// solution it already has loaded rather than a detached file. Non-code assets that no script
	/// editor will accept fall back to the OS default application, which is the behaviour that makes
	/// this worth having on a .json or .md.
	/// </remarks>
	public static class OpenInExternalEditor
	{
		private const string MenuPath = "Assets/NMJ/Open in External Editor";
		private const string RevealPath = "Assets/NMJ/Reveal in Explorer";

		/// <summary>Extensions the configured script editor is expected to handle.</summary>
		private static readonly string[] CodeExtensions =
		{
			".cs", ".js", ".shader", ".cginc", ".hlsl", ".compute", ".asmdef", ".asmref",
			".json", ".xml", ".txt", ".md", ".uss", ".uxml", ".yaml", ".yml"
		};

		[MenuItem(MenuPath, false, 2000)]
		private static void Open()
		{
			foreach (var path in SelectedAssetPaths())
			{
				var absolute = Path.GetFullPath(path);
				var extension = Path.GetExtension(path);

				if (CodeExtensions.Contains(extension, System.StringComparer.OrdinalIgnoreCase))
				{
					// line 1, column 0 is "just open the file" for every editor integration.
					if (CodeEditor.CurrentEditor.OpenProject(absolute, 1, 0)) continue;
				}

				// Either not a code file, or the editor refused it. The OS knows what to do with a
				// .png or a .fbx and we do not.
				EditorUtility.OpenWithDefaultApp(absolute);
			}
		}

		[MenuItem(MenuPath, true)]
		private static bool ValidateOpen() => SelectedAssetPaths().Any();

		/// <summary>
		/// Shows the asset in the OS file browser. Unity's own "Show in Explorer" acts on the folder;
		/// this selects the file itself, which is what you want when hunting a specific .meta.
		/// </summary>
		[MenuItem(RevealPath, false, 2001)]
		private static void Reveal()
		{
			var first = SelectedAssetPaths().FirstOrDefault();
			if (first == null) return;

			EditorUtility.RevealInFinder(Path.GetFullPath(first));
		}

		[MenuItem(RevealPath, true)]
		private static bool ValidateReveal() => SelectedAssetPaths().Any();

		private static System.Collections.Generic.IEnumerable<string> SelectedAssetPaths() =>
			Selection.objects
					 .Select(AssetDatabase.GetAssetPath)
					 .Where(path => !string.IsNullOrEmpty(path) && File.Exists(Path.GetFullPath(path)));
	}
}
