using System.Linq;
using UnityEditor;
using UnityEngine;

namespace NMJTools.Setup
{
	/// <summary>
	/// Routes NMJ's "find usages" command to Asset Usage Detector when it is installed.
	///
	/// Deliberately reached through <see cref="EditorApplication.ExecuteMenuItem"/> rather than an
	/// assembly reference. A reference would need AUD's exact assembly name baked into an asmdef, and
	/// a wrong or renamed one takes the whole assembly down with the "references non-existent
	/// assemblies" error. A menu path that does not resolve simply returns false.
	/// </summary>
	public static class AssetUsageBridge
	{
		// AUD registers under both of these depending on version; the first that resolves wins.
		private static readonly string[] candidateMenuPaths =
		{
			"Window/Asset Usage Detector",
			"Window/General/Asset Usage Detector"
		};

		// ReSharper disable once MemberCanBePrivate.Global
		public static bool IsAvailable
		{
			get
			{
				var entry = PackageCatalog.All.FirstOrDefault(e => e.displayName == "Asset Usage Detector");
				return entry != null && PackageDetector.IsPresent(entry);
			}
		}

		[MenuItem("Assets/NMJ/Find Usages", false, 31)]
		[MenuItem("GameObject/NMJ/Find Usages", false, 13)]
		private static void FindUsages()
		{
			if (!Selection.activeObject) return;

			if (!IsAvailable)
			{
				if (EditorUtility.DisplayDialog(
												"Asset Usage Detector Not Installed",
												"Finding every reference to an asset needs Asset Usage Detector.\n\n" +
												"Install it from Tools > NMJ > Setup > Project Setup, or open the page now.",
												"Open Page", "Cancel"))
				{
					Application.OpenURL("https://openupm.com/packages/com.yasirkula.assetusagedetector/");
				}

				return;
			}

			if (candidateMenuPaths.Any(EditorApplication.ExecuteMenuItem)) return;

			Debug.LogWarning("[NMJ] Asset Usage Detector is installed but its window could not be opened " +
							 "from a known menu path. Open it from the Window menu instead.");
		}

		[MenuItem("Assets/NMJ/Find Usages", true)]
		[MenuItem("GameObject/NMJ/Find Usages", true)]
		private static bool ValidateFindUsages() => Selection.activeObject;
	}
}