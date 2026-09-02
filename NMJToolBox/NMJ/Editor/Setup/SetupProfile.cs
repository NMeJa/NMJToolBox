using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace NMJTools.Setup
{
	/// <summary>
	/// Which catalogue entries a setup run should install, saved as a project asset.
	///
	/// A project asset rather than <see cref="EditorPrefs"/> for the same reason
	/// <c>FolderTemplate</c> is one: the set of packages a project depends on is a property of the
	/// project, not of the machine that happens to be building it. Committed, it means a clone can
	/// reproduce the environment in one click and a change to it shows up in review. EditorPrefs are
	/// per-user and per-machine, which is the wrong scope for a shared decision.
	///
	/// Create one from <c>Assets ▸ Create ▸ NMJ ▸ Setup Profile</c>, or let the setup window make one
	/// the first time you deviate from the defaults.
	/// </summary>
	[CreateAssetMenu(fileName = "SetupProfile", menuName = "NMJ/Setup Profile", order = 201)]
	public class SetupProfile : ScriptableObject
	{
		/// <summary>A starting point for a fresh profile.</summary>
		public enum Preset
		{
			/// <summary>Everything marked as a default in the catalogue.</summary>
			Standard,

			/// <summary>Standard, plus the mobile extras, minus the heavy authoring packages.</summary>
			Mobile,

			/// <summary>The smallest set that still gets you a debuggable project.</summary>
			Prototype,

			/// <summary>Every free package in the catalogue.</summary>
			Everything,

			/// <summary>Nothing ticked — build the list yourself.</summary>
			Empty
		}

		[SerializeField, TextArea(1, 3)]
		private string description = "Packages installed by Tools ▸ NMJ ▸ Setup ▸ Project Setup.";

		[Tooltip("Catalogue keys — package ids, or 'asset:<Display Name>' for Asset Store entries.")]
		[SerializeField] private List<string> enabledPackages = new();

		[Tooltip("Removal keys ('remove:<package id>') this profile should apply.")]
		[SerializeField] private List<string> enabledRemovals = new();

		[Tooltip("Import a paid package straight from the Asset Store cache when one is found, " +
				 "instead of only reporting that it could be imported.")]
		[SerializeField] private bool importCachedAssetStorePackages = true;

		[Tooltip("Open the store page for any paid package that is missing and not in the cache. " +
				 "Off by default — a setup run that opens eight browser tabs is not a setup run.")]
		[SerializeField] private bool openStorePagesForMissing;

		public string Description => description;
		public bool ImportCachedAssetStorePackages => importCachedAssetStorePackages;
		public bool OpenStorePagesForMissing => openStorePagesForMissing;

		public bool IsEnabled(PackageEntry entry) => entry != null && enabledPackages.Contains(entry.Key);
		public bool IsEnabled(PackageRemoval removal) => removal != null && enabledRemovals.Contains(removal.Key);

		public void SetEnabled(PackageEntry entry, bool enabled) => Toggle(enabledPackages, entry?.Key, enabled);
		public void SetEnabled(PackageRemoval removal, bool enabled) => Toggle(enabledRemovals, removal?.Key, enabled);

		private static void Toggle(ICollection<string> list, string key, bool enabled)
		{
			if (string.IsNullOrEmpty(key)) return;

			if (enabled)
			{
				if (!list.Contains(key)) list.Add(key);
			}
			else
			{
				list.Remove(key);
			}
		}

		public IEnumerable<PackageEntry> EnabledEntries =>
			PackageCatalog.All.Where(IsEnabled);

		public IEnumerable<PackageRemoval> EnabledRemovals =>
			PackageCatalog.Removals.Where(IsEnabled);

		/// <summary>
		/// Rewrites the toggle lists from a preset. Destructive by design — the setup window asks
		/// first.
		/// </summary>
		public void ApplyPreset(Preset preset)
		{
			enabledPackages.Clear();
			enabledRemovals.Clear();

			// Removals travel with every preset except Empty: they exist to undo Unity's own template,
			// and no preset here wants the Visual Studio integration back.
			if (preset != Preset.Empty)
			{
				foreach (var removal in PackageCatalog.Removals.Where(r => r.defaultEnabled))
					enabledRemovals.Add(removal.Key);
			}

			foreach (var entry in PackageCatalog.All)
			{
				if (ShouldEnable(entry, preset)) enabledPackages.Add(entry.Key);
			}

			importCachedAssetStorePackages = preset != Preset.Empty;
			openStorePagesForMissing = false;

			EditorUtility.SetDirty(this);
		}

		private static bool ShouldEnable(PackageEntry entry, Preset preset) => preset switch
		{
			Preset.Empty => false,

			Preset.Everything => entry.source != InstallSource.AssetStore || entry.defaultEnabled,

			Preset.Standard => entry.defaultEnabled,

			// Mobile keeps the defaults, adds the mobile category, and drops the two authoring
			// packages that only earn their compile time on a project that ships cinematics.
			Preset.Mobile => entry.category switch
			{
				PackageCategory.Mobile    => true,
				PackageCategory.Authoring => entry.defaultEnabled && entry.packageId is not
											 ("com.unity.recorder" or "com.unity.timeline" or "com.unity.cinemachine"),
				_ => entry.defaultEnabled
			},

			// A jam project wants a console, a profiler overlay and async. Not a content pipeline.
			Preset.Prototype => entry.defaultEnabled && entry.category is
								PackageCategory.Core or PackageCategory.Debugging or PackageCategory.Paid,

			_ => entry.defaultEnabled
		};

		// ===================================================================================
		// Resolution
		// ===================================================================================

		private const string LastProfileKey = "NMJ.Setup.LastProfile";

		/// <summary>Every profile asset in the project, name-ordered.</summary>
		public static List<SetupProfile> FindAll() =>
			AssetDatabase.FindAssets($"t:{nameof(SetupProfile)}")
						 .Select(AssetDatabase.GUIDToAssetPath)
						 .Select(AssetDatabase.LoadAssetAtPath<SetupProfile>)
						 .Where(p => p)
						 .OrderBy(p => p.name, StringComparer.Ordinal)
						 .ToList();

		/// <summary>
		/// The profile the window last used, the only one in the project, or null when there are none.
		/// Never creates an asset as a side effect of being asked — the window does that, on request.
		/// </summary>
		public static SetupProfile Resolve()
		{
			var all = FindAll();
			if (all.Count == 0) return null;

			var remembered = EditorPrefs.GetString(LastProfileKey, string.Empty);
			if (string.IsNullOrEmpty(remembered)) return all[0];

			var byPath = AssetDatabase.LoadAssetAtPath<SetupProfile>(remembered);
			return byPath ? byPath : all[0];
		}

		public static void Remember(SetupProfile profile)
		{
			EditorPrefs.SetString(LastProfileKey,
								  profile ? AssetDatabase.GetAssetPath(profile) : string.Empty);
		}

		/// <summary>
		/// Creates a profile asset seeded from a preset. Returns null if the user cancels the save
		/// dialog.
		/// </summary>
		public static SetupProfile CreateAsset(Preset preset = Preset.Standard)
		{
			var path = EditorUtility.SaveFilePanelInProject(
														   "Create Setup Profile",
														   "SetupProfile",
														   "asset",
														   "Saved in the project so the package set travels with the repository.");

			if (string.IsNullOrEmpty(path)) return null;

			var profile = CreateInstance<SetupProfile>();
			profile.ApplyPreset(preset);

			AssetDatabase.CreateAsset(profile, path);
			AssetDatabase.SaveAssets();
			Remember(profile);

			return profile;
		}

		/// <summary>
		/// A profile that exists only in memory, used when the project has no asset yet so the window
		/// still opens with a sensible set ticked. Nothing persists until the user saves one.
		/// </summary>
		public static SetupProfile CreateTransient(Preset preset = Preset.Standard)
		{
			var profile = CreateInstance<SetupProfile>();
			profile.name = "(unsaved defaults)";
			profile.ApplyPreset(preset);
			return profile;
		}
	}
}
