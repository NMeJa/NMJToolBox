using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Callbacks;
using UnityEngine;

namespace NMJToolBox.Setup
{
	/// <summary>
	/// Detects which of the catalogued packages are in the project and keeps the matching scripting
	/// define symbols in sync.
	///
	/// Detection reads loaded assemblies rather than asmdef <c>versionDefines</c> because it has to
	/// cover both cases uniformly: UPM packages, which have a package name and version, and Asset
	/// Store content dropped into <c>Assets/</c>, which is not a package at all and has nothing for
	/// <c>versionDefines</c> to bind to.
	/// </summary>
	[InitializeOnLoad]
	public static class PackageDetector
	{
		private const string AutoManageKey = "NMJ.Setup.AutoManageDefines";
		private const string ForceAttributesKey = "NMJ.Setup.ForceAttributes";

		static PackageDetector()
		{
			// Defer past the constructor: touching PlayerSettings during the InitializeOnLoad pass can
			// run before the asset database is ready.
			EditorApplication.delayCall += () => Sync(false);
		}

		[DidReloadScripts]
		private static void OnScriptsReloaded() => EditorApplication.delayCall += () => Sync(false);

		/// <summary>Whether defines are updated automatically after every recompile.</summary>
		public static bool AutoManage
		{
			get => EditorPrefs.GetBool(AutoManageKey, true);
			set => EditorPrefs.SetBool(AutoManageKey, value);
		}

		/// <summary>
		/// Forces NMJ's inspector attributes on even when Odin is present. Off by default: both
		/// packages declare <c>[ReadOnly]</c>, <c>[Required]</c> and <c>[Button]</c>, so with both
		/// namespaces imported every use of those names becomes ambiguous, and both claim the default
		/// inspector for MonoBehaviour. Turn this on only if you intend to fully qualify them.
		/// </summary>
		public static bool ForceAttributesWithOdin
		{
			get => EditorPrefs.GetBool(ForceAttributesKey, false);
			set => EditorPrefs.SetBool(ForceAttributesKey, value);
		}

		/// <summary>True when the package's assemblies or marker types are loaded.</summary>
		public static bool IsPresent(PackageEntry entry)
		{
			if (entry == null) return false;

			var loaded = LoadedAssemblyNames();

			if (entry.Assemblies.Any(loaded.Contains)) return true;

			return entry.Types.Length > 0 &&
				   entry.Types.Any(typeName => FindType(typeName) != null);
		}

		private static HashSet<string> cachedAssemblyNames;

		private static HashSet<string> LoadedAssemblyNames()
		{
			// Rebuilt once per reload; the set cannot change without a domain reload, and the window
			// repaints many times a second.
			return cachedAssemblyNames ??= new HashSet<string>(
															   AppDomain.CurrentDomain.GetAssemblies()
																		.Select(a => a.GetName().Name),
															   StringComparer.Ordinal);
		}

		private static Type FindType(string fullName)
		{
			var direct = Type.GetType(fullName);
			if (direct != null) return direct;

			return AppDomain.CurrentDomain.GetAssemblies()
							.Select(assembly =>
								{
									try { return assembly.GetType(fullName); }
									catch { return null; }
								})
							.FirstOrDefault(type => type != null);
		}

		/// <summary>
		/// Recomputes every managed define and writes them back if anything changed.
		/// </summary>
		/// <param name="verbose">Log what changed. Off for the automatic pass, on when run by hand.</param>
		public static void Sync(bool verbose)
		{
			if (!AutoManage && !verbose) return;

			cachedAssemblyNames = null;

			var namedTarget = CurrentNamedBuildTarget();
			var defines = PlayerSettings.GetScriptingDefineSymbols(namedTarget)
										.Split(';')
										.Where(d => !string.IsNullOrWhiteSpace(d))
										.Select(d => d.Trim())
										.ToList();

			var before = new HashSet<string>(defines, StringComparer.Ordinal);
			var wanted = new HashSet<string>(StringComparer.Ordinal);
			var managed = new HashSet<string>(StringComparer.Ordinal);

			foreach (var entry in PackageCatalog.All)
			{
				if (string.IsNullOrEmpty(entry.Define)) continue;

				managed.Add(entry.Define);
				if (IsPresent(entry)) wanted.Add(entry.Define);
			}

			// NMJ's own attributes are the one define driven by an *absence*: they exist to fill the
			// gap Odin would otherwise fill, so they switch off the moment Odin appears.
			managed.Add(PackageCatalog.NmjAttributesDefine);
			var odinPresent = before.Contains(PackageCatalog.OdinDefine) ||
							  PackageCatalog.All.Any(e => e.DisplayName == "Odin Inspector" && IsPresent(e));

			if (!odinPresent || ForceAttributesWithOdin)
				wanted.Add(PackageCatalog.NmjAttributesDefine);

			var after = new HashSet<string>(before, StringComparer.Ordinal);
			after.ExceptWith(managed);
			after.UnionWith(wanted);

			if (after.SetEquals(before))
			{
				if (verbose) Debug.Log("[NMJ] Package defines already up to date.");
				return;
			}

			var added = after.Except(before).ToList();
			var removed = before.Except(after).ToList();

			// Writing defines triggers a recompile, which re-enters this method. Only ever writing on
			// an actual change is what stops that from looping forever.
			PlayerSettings.SetScriptingDefineSymbols(namedTarget, after.OrderBy(d => d).ToArray());

			var summary = new List<string>();
			if (added.Count > 0) summary.Add("+" + string.Join(" +", added));
			if (removed.Count > 0) summary.Add("-" + string.Join(" -", removed));
			Debug.Log($"[NMJ] Package defines updated: {string.Join("  ", summary)}");
		}

		internal static NamedBuildTarget CurrentNamedBuildTarget() =>
			NamedBuildTarget.FromBuildTargetGroup(EditorUserBuildSettings.selectedBuildTargetGroup);

		[MenuItem("Tools/NMJ/Setup/Re-scan Packages", false, 300)]
		private static void RescanMenu() => Sync(true);
	}
}
