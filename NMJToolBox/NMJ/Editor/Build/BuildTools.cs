using System;
using System.Globalization;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace NMJTools
{
	/// <summary>
	/// Version bumping, timestamped builds, and the folder-opening commands that otherwise mean
	/// digging through Explorer every time.
	/// </summary>
	public static class BuildTools
	{
		private const string AutoIncrementKey = "NMJ.Build.AutoIncrement";
		private const string BuildRootKey = "NMJ.Build.Root";
		private const string DefaultBuildRoot = "Builds";

		/// <summary>Bumps <c>PlayerSettings.bundleVersion</c> on every successful build.</summary>
		public static bool AutoIncrementOnBuild
		{
			get => EditorPrefs.GetBool(AutoIncrementKey, false);
			private set => EditorPrefs.SetBool(AutoIncrementKey, value);
		}

		/// <summary>Folder builds are written into, relative to the project root.</summary>
		public static string BuildRoot
		{
			get => EditorPrefs.GetString(BuildRootKey, DefaultBuildRoot);
			set => EditorPrefs.SetString(BuildRootKey, value);
		}

		// ========== Version ==========

		[MenuItem("Tools/NMJ/Build/Increment Patch Version", false, 200)]
		private static void IncrementPatch() => IncrementVersion(2);

		[MenuItem("Tools/NMJ/Build/Increment Minor Version", false, 201)]
		private static void IncrementMinor() => IncrementVersion(1);

		[MenuItem("Tools/NMJ/Build/Increment Major Version", false, 202)]
		private static void IncrementMajor() => IncrementVersion(0);

		/// <summary>
		/// Bumps one part of a dotted version and zeroes everything after it. Missing components
		/// are treated as zero, so "1.2" bumped at the patch level becomes "1.2.1" rather than failing.
		/// </summary>
		public static void IncrementVersion(int componentIndex)
		{
			var parts = PlayerSettings.bundleVersion
									  .Split('.')
									  .Select(p => int.TryParse(p, NumberStyles.Integer, CultureInfo.InvariantCulture, out var v) ? v : 0)
									  .ToList();

			while (parts.Count <= componentIndex) parts.Add(0);

			parts[componentIndex]++;
			for (var i = componentIndex + 1; i < parts.Count; i++) parts[i] = 0;

			var previous = PlayerSettings.bundleVersion;
			PlayerSettings.bundleVersion = string.Join(".", parts);

			// Android and iOS keep their own monotonic build counters alongside the display version;
			// bumping only bundleVersion would make a store upload fail late instead of here.
			PlayerSettings.Android.bundleVersionCode++;
			if (int.TryParse(PlayerSettings.iOS.buildNumber, out var iosBuild))
				PlayerSettings.iOS.buildNumber = (iosBuild + 1).ToString(CultureInfo.InvariantCulture);

			Debug.Log($"[NMJ] Version {previous} → {PlayerSettings.bundleVersion} " +
					  $"(Android code {PlayerSettings.Android.bundleVersionCode}).");
		}

		[MenuItem("Tools/NMJ/Build/Auto-Increment Patch On Build", false, 210)]
		private static void ToggleAutoIncrement() => AutoIncrementOnBuild = !AutoIncrementOnBuild;

		[MenuItem("Tools/NMJ/Build/Auto-Increment Patch On Build", true)]
		private static bool ToggleAutoIncrementChecked()
		{
			Menu.SetChecked("Tools/NMJ/Build/Auto-Increment Patch On Build", AutoIncrementOnBuild);
			return true;
		}

		// ========== Build ==========

		[MenuItem("Tools/NMJ/Build/Build Timestamped %#b", false, 220)]
		private static void BuildTimestamped()
		{
			var scenes = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray();
			if (scenes.Length == 0)
			{
				EditorUtility.DisplayDialog("Nothing To Build",
											"No scenes are enabled in Build Settings.", "OK");
				return;
			}

			var target = EditorUserBuildSettings.activeBuildTarget;
			var stamp = DateTime.Now.ToString("yyyyMMdd-HHmm", CultureInfo.InvariantCulture);
			var folderName = $"{SanitiseFileName(PlayerSettings.productName)}_{PlayerSettings.bundleVersion}_{stamp}";

			var directory = Path.Combine(BuildRoot, target.ToString(), folderName);
			Directory.CreateDirectory(directory);

			var options = new BuildPlayerOptions
			{
				scenes = scenes,
				locationPathName = Path.Combine(directory, ExecutableName(target)),
				target = target,
				targetGroup = BuildPipeline.GetBuildTargetGroup(target),
				options = BuildOptions.None
			};

			var report = BuildPipeline.BuildPlayer(options);
			var summary = report.summary;

			if (summary.result == BuildResult.Succeeded)
			{
				var megabytes = summary.totalSize / (1024f * 1024f);
				Debug.Log($"[NMJ] Build succeeded in {summary.totalTime:mm\\:ss} — {megabytes:F1} MB\n{directory}");
				RevealInFinder(directory);
			}
			else
			{
				Debug.LogError($"[NMJ] Build {summary.result} with {summary.totalErrors} error(s).");
			}
		}

		/// <summary>
		/// Unity wants the output path to include the executable for some targets and to be a plain
		/// folder for others; getting this wrong produces a build nested one level too deep.
		/// </summary>
		private static string ExecutableName(BuildTarget target)
		{
			var product = SanitiseFileName(PlayerSettings.productName);

			return target switch
			{
				BuildTarget.StandaloneWindows or BuildTarget.StandaloneWindows64 => product + ".exe",
				BuildTarget.StandaloneOSX                                        => product + ".app",
				BuildTarget.StandaloneLinux64                                    => product + ".x86_64",
				BuildTarget.Android                                              => product + ".apk",
				_                                                                => product
			};
		}

		private static string SanitiseFileName(string name)
		{
			var invalid = Path.GetInvalidFileNameChars();
			var cleaned = new string(name.Where(c => !invalid.Contains(c)).ToArray()).Trim();
			return string.IsNullOrEmpty(cleaned) ? "Build" : cleaned;
		}

		// ========== Folders ==========

		[MenuItem("Tools/NMJ/Build/Open Build Folder", false, 230)]
		private static void OpenBuildFolder()
		{
			var path = Path.GetFullPath(BuildRoot);
			if (!Directory.Exists(path))
			{
				Debug.LogWarning($"[NMJ] Build folder does not exist yet: {path}");
				return;
			}

			RevealInFinder(path);
		}

		[MenuItem("Tools/NMJ/Open/Persistent Data Path", false, 240)]
		private static void OpenPersistentDataPath() => RevealInFinder(Application.persistentDataPath);

		[MenuItem("Tools/NMJ/Open/Project Folder", false, 241)]
		private static void OpenProjectFolder() =>
			RevealInFinder(Directory.GetParent(Application.dataPath)?.FullName ?? Application.dataPath);

		[MenuItem("Tools/NMJ/Open/Editor Log Folder", false, 242)]
		private static void OpenEditorLogFolder()
		{
			var path = Application.platform switch
			{
				RuntimePlatform.WindowsEditor => Path.Combine(
															  Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
															  "Unity", "Editor"),
				RuntimePlatform.OSXEditor => Path.Combine(
														  Environment.GetFolderPath(Environment.SpecialFolder.Personal),
														  "Library", "Logs", "Unity"),
				_ => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Personal), ".config", "unity3d")
			};

			RevealInFinder(path);
		}

		/// <summary>
		/// <see cref="EditorUtility.RevealInFinder"/> needs a trailing separator to open a folder
		/// rather than select it in its parent.
		/// </summary>
		private static void RevealInFinder(string path)
		{
			if (!Directory.Exists(path))
			{
				Debug.LogWarning($"[NMJ] Path does not exist: {path}");
				return;
			}

			EditorUtility.RevealInFinder(path + Path.DirectorySeparatorChar);
		}
	}

	/// <summary>
	/// Applies <see cref="BuildTools.AutoIncrementOnBuild"/>. Runs before the build rather than after,
	/// so the version baked into the player is the incremented one.
	/// </summary>
	public class BuildVersionIncrementer : IPreprocessBuildWithReport
	{
		public int callbackOrder => 0;

		public void OnPreprocessBuild(BuildReport report)
		{
			if (!BuildTools.AutoIncrementOnBuild) return;
			BuildTools.IncrementVersion(2);
		}
	}
}