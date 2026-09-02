using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Callbacks;
using UnityEngine;

namespace NMJTools.Setup
{
	/// <summary>
	/// Detects which of the catalogued packages are in the project and keeps the matching scripting
	/// define symbols in sync.
	///
	/// Detection reads loaded assemblies rather than asmdef <c>versionDefines</c> because it has to
	/// cover both cases uniformly: UPM packages, which have a package name and version, and Asset
	/// Store content dropped into <c>Assets/</c>, which is not a package at all and has nothing for
	/// <c>versionDefines</c> to bind to. The manifest's dependency list is consulted as well, so a
	/// package that is installed but has not finished compiling still reads as present — otherwise
	/// the setup window offers to install it a second time in the window between the two.
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

		/// <summary>True when the package's assemblies, marker types, or manifest entry are found.</summary>
		public static bool IsPresent(PackageEntry entry)
		{
			if (entry == null) return false;

			if (entry.assemblies.Any(LoadedAssemblyNames().Contains)) return true;

			if (entry.types.Length > 0 && entry.types.Any(typeName => FindType(typeName) != null)) return true;

			// Manifest fallback, for the gap between "resolved" and "compiled". Only meaningful for
			// UPM entries — an Asset Store drop never appears in the manifest.
			return entry.IsUpm &&
				   !string.IsNullOrEmpty(entry.packageId) &&
				   ManifestDependencies().Contains(entry.packageId);
		}

		private static HashSet<string> cachedAssemblyNames;
		private static HashSet<string> cachedDependencies;

		private static HashSet<string> LoadedAssemblyNames()
		{
			// Rebuilt once per reload; the set cannot change without a domain reload, and the window
			// repaints many times a second.
			return cachedAssemblyNames ??= new HashSet<string>(
															   AppDomain.CurrentDomain.GetAssemblies()
																		.Select(a => a.GetName().Name),
															   StringComparer.Ordinal);
		}

		private static HashSet<string> ManifestDependencies() =>
			cachedDependencies ??= PackageManifest.ReadDependencies();

		/// <summary>Drops the per-reload caches. Call after anything that changes the manifest.</summary>
		public static void InvalidateCaches()
		{
			cachedAssemblyNames = null;
			cachedDependencies = null;
		}

		private static Type FindType(string fullName)
		{
			var direct = Type.GetType(fullName);
			if (direct != null) return direct;

			return AppDomain.CurrentDomain.GetAssemblies()
							.Select(assembly =>
								{
									try
									{
										return assembly.GetType(fullName);
									}
									catch
									{
										return null;
									}
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

			InvalidateCaches();

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
				if (string.IsNullOrEmpty(entry.define)) continue;

				managed.Add(entry.define);

				// Deliberately assembly-only here, not IsPresent: a define drives conditional
				// compilation, so it must not be set while the package's assemblies are still
				// resolving. The manifest fallback answers "should the window offer to install this",
				// which is a different question from "will this code compile".
				if (HasCompiledAssemblies(entry)) wanted.Add(entry.define);
			}

			// NMJ's own attributes are the one define driven by an *absence*: they exist to fill the
			// gap Odin would otherwise fill, so they switch off the moment Odin appears.
			managed.Add(PackageCatalog.NmjAttributesDefine);
			var odinPresent = before.Contains(PackageCatalog.OdinDefine) ||
							  PackageCatalog.All.Any(e => e.displayName == "Odin Inspector" && HasCompiledAssemblies(e));

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

		/// <summary>Assembly/type detection only — the manifest does not count. See <see cref="Sync"/>.</summary>
		private static bool HasCompiledAssemblies(PackageEntry entry)
		{
			if (entry == null) return false;
			if (entry.assemblies.Any(LoadedAssemblyNames().Contains)) return true;

			return entry.types.Length > 0 && entry.types.Any(typeName => FindType(typeName) != null);
		}

		internal static NamedBuildTarget CurrentNamedBuildTarget() =>
			NamedBuildTarget.FromBuildTargetGroup(EditorUserBuildSettings.selectedBuildTargetGroup);

		[MenuItem("Tools/NMJ/Setup/Re-scan Packages", false, 300)]
		private static void RescanMenu() => Sync(true);
	}
}
