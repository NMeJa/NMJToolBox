using System;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace NMJTools.Setup
{
	/// <summary>
	/// One window to bring a fresh project up to the standard package set — and one button to do it.
	///
	/// What it can and cannot do, plainly: UPM packages (git, OpenUPM and Unity's registry) install in
	/// a single transaction with no further input. Paid Asset Store packages cannot be downloaded —
	/// Unity checks entitlement server-side against the signed-in account, so nothing can make a
	/// package the user does not own appear. What it does instead is find the copy Unity already
	/// cached when the package was last downloaded on this machine, and import it silently. Only a
	/// package that has never been downloaded here needs a trip to the store page.
	///
	/// Every item is a toggle, and the set of toggles is a <see cref="SetupProfile"/> asset, so the
	/// choice travels with the project rather than with the machine.
	/// </summary>
	public class ProjectSetupWindow : EditorWindow
	{
		private Vector2 scroll;
		private SetupProfile profile;
		private SetupProfile[] availableProfiles = Array.Empty<SetupProfile>();
		private string[] profileNames = Array.Empty<string>();
		private int profileIndex;
		private bool showRemovals = true;

		[MenuItem("Tools/NMJ/Setup/Project Setup", false, 290)]
		public static void Open()
		{
			var window = GetWindow<ProjectSetupWindow>(true, "NMJ Project Setup");
			window.minSize = new Vector2(620, 620);
			AssetStoreLocator.Refresh();
			window.Show();
		}

		private void OnEnable()
		{
			RefreshProfiles();
			PackageInstaller.Changed += Repaint;
		}

		private void OnDisable() => PackageInstaller.Changed -= Repaint;

		private void RefreshProfiles()
		{
			availableProfiles = SetupProfile.FindAll().ToArray();
			profileNames = availableProfiles.Select(p => p.name).ToArray();

			if (availableProfiles.Length == 0)
			{
				// Nothing saved yet: work against an in-memory profile so the window opens with a
				// sensible set ticked instead of an empty list and a nag to create an asset first.
				if (!profile || AssetDatabase.Contains(profile))
					profile = SetupProfile.CreateTransient();

				profileIndex = -1;
				return;
			}

			var resolved = SetupProfile.Resolve();
			profileIndex = Mathf.Max(0, Array.IndexOf(availableProfiles, resolved));
			profile = availableProfiles[profileIndex];
		}

		private bool IsTransient => profile && !AssetDatabase.Contains(profile);

		private void OnGUI()
		{
			DrawProfileBar();
			DrawDefinesBar();

			scroll = EditorGUILayout.BeginScrollView(scroll);

			DrawRemovals();

			foreach (PackageCategory category in Enum.GetValues(typeof(PackageCategory)))
				DrawCategory(category);

			EditorGUILayout.EndScrollView();

			DrawFooter();
		}

		// ===================================================================================
		// Header
		// ===================================================================================

		private void DrawProfileBar()
		{
			EditorGUILayout.Space(6);

			using (new EditorGUILayout.HorizontalScope())
			{
				EditorGUILayout.LabelField("Profile", EditorStyles.boldLabel, GUILayout.Width(50));

				if (availableProfiles.Length > 0)
				{
					EditorGUI.BeginChangeCheck();
					profileIndex = EditorGUILayout.Popup(profileIndex, profileNames);
					if (EditorGUI.EndChangeCheck())
					{
						profile = availableProfiles[profileIndex];
						SetupProfile.Remember(profile);
					}
				}
				else
				{
					EditorGUILayout.LabelField("(unsaved defaults)", EditorStyles.miniLabel);
				}

				if (GUILayout.Button("Preset ▾", EditorStyles.miniButton, GUILayout.Width(70))) ShowPresetMenu();

				if (GUILayout.Button(IsTransient ? "Save Profile…" : "New…", EditorStyles.miniButton, GUILayout.Width(95)))
				{
					var created = SetupProfile.CreateAsset();
					if (created)
					{
						if (IsTransient) CopyInto(profile, created);
						RefreshProfiles();
					}
				}

				if (GUILayout.Button("Reload", EditorStyles.miniButton, GUILayout.Width(60)))
				{
					AssetStoreLocator.Refresh();
					RefreshProfiles();
				}
			}

			if (IsTransient)
			{
				EditorGUILayout.HelpBox(
										"No SetupProfile asset in this project, so these toggles are not saved yet. " +
										"Run Setup works fine without one — press Save Profile… to commit the " +
										"selection so a clone reproduces it.",
										MessageType.Info);
			}
			else if (profile && !string.IsNullOrWhiteSpace(profile.Description))
			{
				EditorGUILayout.LabelField(profile.Description, EditorStyles.wordWrappedMiniLabel);
			}
		}

		private void ShowPresetMenu()
		{
			var menu = new GenericMenu();

			foreach (SetupProfile.Preset preset in Enum.GetValues(typeof(SetupProfile.Preset)))
			{
				var captured = preset;
				menu.AddItem(new GUIContent(ObjectNames.NicifyVariableName(preset.ToString())), false,
							 () =>
								 {
									 if (!EditorUtility.DisplayDialog(
																	  "Apply Preset",
																	  $"Replace every toggle in '{profile.name}' with the {captured} preset?",
																	  "Apply", "Cancel"))
										 return;

									 profile.ApplyPreset(captured);
									 Repaint();
								 });
			}

			menu.ShowAsContext();
		}

		private static void CopyInto(SetupProfile source, SetupProfile destination)
		{
			foreach (var entry in PackageCatalog.All) destination.SetEnabled(entry, source.IsEnabled(entry));
			foreach (var removal in PackageCatalog.Removals) destination.SetEnabled(removal, source.IsEnabled(removal));

			EditorUtility.SetDirty(destination);
			AssetDatabase.SaveAssets();
		}

		private void DrawDefinesBar()
		{
			using (new EditorGUILayout.HorizontalScope())
			{
				PackageDetector.AutoManage = EditorGUILayout.ToggleLeft(
																		new GUIContent("Auto-manage define symbols",
																					   "Keep NMJ_* defines in sync with what is installed, after every recompile."),
																		PackageDetector.AutoManage);

				if (GUILayout.Button("Re-scan", GUILayout.Width(80))) PackageDetector.Sync(true);
			}

			var odin = PackageCatalog.All.FirstOrDefault(e => e.displayName == "Odin Inspector");
			if (odin == null || !PackageDetector.IsPresent(odin)) return;

			EditorGUI.BeginChangeCheck();
			PackageDetector.ForceAttributesWithOdin = EditorGUILayout.ToggleLeft(
																				 new GUIContent("Force NMJ attributes alongside Odin",
																								"Both packages define [ReadOnly], [Required] and [Button]. Enabling this makes " +
																								"those names ambiguous wherever both namespaces are imported."),
																				 PackageDetector.ForceAttributesWithOdin);

			if (EditorGUI.EndChangeCheck()) PackageDetector.Sync(true);
		}

		// ===================================================================================
		// Body
		// ===================================================================================

		private void DrawRemovals()
		{
			var removals = PackageCatalog.Removals;
			if (removals.Count == 0) return;

			EditorGUILayout.Space(6);
			showRemovals = EditorGUILayout.Foldout(showRemovals, "Remove (Unity template packages)", true, EditorStyles.foldoutHeader);
			if (!showRemovals) return;

			var installed = PackageManifest.ReadDependencies();

			foreach (var removal in removals)
			{
				using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
				{
					using (new EditorGUILayout.HorizontalScope())
					{
						var enabled = profile.IsEnabled(removal);

						EditorGUI.BeginChangeCheck();
						enabled = EditorGUILayout.ToggleLeft(removal.displayName, enabled, GUILayout.Width(220));
						if (EditorGUI.EndChangeCheck())
						{
							profile.SetEnabled(removal, enabled);
							MarkProfileDirty();
						}

						GUILayout.FlexibleSpace();

						EditorGUILayout.LabelField(
												   installed.Contains(removal.packageId) ? "present" : "not installed",
												   EditorStyles.miniLabel, GUILayout.Width(90));
					}

					if (!string.IsNullOrEmpty(removal.reason))
						EditorGUILayout.LabelField(removal.reason, EditorStyles.wordWrappedMiniLabel);
				}
			}
		}

		private void DrawCategory(PackageCategory category)
		{
			var entries = PackageCatalog.InCategory(category).ToList();
			if (entries.Count == 0) return;

			EditorGUILayout.Space(8);
			EditorGUILayout.LabelField(HeadingFor(category), EditorStyles.boldLabel);

			using (new EditorGUILayout.HorizontalScope())
			{
				GUILayout.Space(4);
				if (GUILayout.Button("All", EditorStyles.miniButtonLeft, GUILayout.Width(40)))
					SetAll(entries, true);
				if (GUILayout.Button("None", EditorStyles.miniButtonRight, GUILayout.Width(45)))
					SetAll(entries, false);
				GUILayout.FlexibleSpace();
			}

			foreach (var entry in entries) DrawEntry(entry);
		}

		private void SetAll(System.Collections.Generic.IEnumerable<PackageEntry> entries, bool enabled)
		{
			foreach (var entry in entries) profile.SetEnabled(entry, enabled);
			MarkProfileDirty();
		}

		private static string HeadingFor(PackageCategory category) => category switch
		{
			PackageCategory.Core      => "Core",
			PackageCategory.Debugging => "Debugging & profiling",
			PackageCategory.Authoring => "Authoring & content",
			PackageCategory.Optional  => "Optional",
			PackageCategory.Mobile    => "Mobile",
			PackageCategory.Paid      => "Paid — Asset Store",
			_                         => category.ToString()
		};

		private void DrawEntry(PackageEntry entry)
		{
			var present = PackageDetector.IsPresent(entry);

			using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
			{
				using (new EditorGUILayout.HorizontalScope())
				{
					// An installed package's toggle is disabled rather than hidden: the tick still
					// means "this project wants this package", which is what a profile records.
					using (new EditorGUI.DisabledScope(present))
					{
						var enabled = profile.IsEnabled(entry);

						EditorGUI.BeginChangeCheck();
						enabled = EditorGUILayout.ToggleLeft(
															 new GUIContent($"{(present ? "✓ " : string.Empty)}{entry.displayName}",
																			entry.packageId),
															 enabled, GUILayout.Width(230));
						if (EditorGUI.EndChangeCheck())
						{
							profile.SetEnabled(entry, enabled);
							MarkProfileDirty();
						}
					}

					GUILayout.FlexibleSpace();

					if (!string.IsNullOrEmpty(entry.define))
						EditorGUILayout.LabelField(entry.define, EditorStyles.miniLabel, GUILayout.Width(170));

					EditorGUILayout.LabelField(StatusFor(entry, present), EditorStyles.miniLabel, GUILayout.Width(120));

					if (!string.IsNullOrEmpty(entry.url) &&
						GUILayout.Button(new GUIContent("↗", "Open the package page"), EditorStyles.miniButton, GUILayout.Width(22)))
					{
						Application.OpenURL(entry.url);
					}
				}

				if (!string.IsNullOrEmpty(entry.notes))
					EditorGUILayout.LabelField(entry.notes, EditorStyles.wordWrappedMiniLabel);
			}
		}

		private static string StatusFor(PackageEntry entry, bool present)
		{
			if (present) return "installed";
			if (entry.IsUpm) return entry.source.ToString();

			return AssetStoreLocator.FindCachedPackage(entry) != null ? "cached — importable" : "not downloaded";
		}

		private void MarkProfileDirty()
		{
			if (!IsTransient) EditorUtility.SetDirty(profile);
		}

		// ===================================================================================
		// Footer
		// ===================================================================================

		private void DrawFooter()
		{
			EditorGUILayout.Space(4);

			var plan = PackageInstaller.BuildPlan(profile);
			var busy = PackageInstaller.IsBusy;

			var status = PackageInstaller.Status;
			if (!string.IsNullOrEmpty(status)) EditorGUILayout.HelpBox(status, MessageType.None);

			if (plan.Unavailable.Count > 0)
			{
				EditorGUILayout.HelpBox(
										$"{plan.Unavailable.Count} paid package(s) are selected but have never been " +
										"downloaded on this machine, so there is nothing to import: " +
										string.Join(", ", plan.Unavailable.Select(e => e.displayName)) +
										".\n\nDownload each once via Package Manager ▸ My Assets and they will be " +
										"imported automatically from then on, in this and every other project.",
										MessageType.Warning);
			}

			using (new EditorGUILayout.HorizontalScope())
			{
				EditorGUILayout.LabelField(
										   plan.IsEmpty
											   ? "Nothing to do."
											   : $"{plan.ToInstall.Count} to install · {plan.ToRemove.Count} to remove · {plan.ToImport.Count} to import",
										   EditorStyles.miniLabel);

				GUILayout.FlexibleSpace();

				if (busy && GUILayout.Button(new GUIContent("Reset", "Clear a stuck run"), GUILayout.Width(60)))
					PackageInstaller.Reset();

				using (new EditorGUI.DisabledScope(busy || plan.IsEmpty))
				{
					if (GUILayout.Button($"Run Setup ({plan.TotalActions})", GUILayout.Height(28), GUILayout.Width(200)))
						Run(plan);
				}
			}
		}

		private void Run(PackageInstaller.Plan plan)
		{
			var summary = string.Join("\n", new[]
			{
				plan.ToInstall.Count > 0 ? "Install: " + string.Join(", ", plan.ToInstall.Select(e => e.displayName)) : null,
				plan.ToRemove.Count > 0 ? "Remove: " + string.Join(", ", plan.ToRemove.Select(r => r.displayName)) : null,
				plan.ToImport.Count > 0 ? "Import from cache: " + string.Join(", ", plan.ToImport.Select(e => e.displayName)) : null
			}.Where(s => s != null));

			if (!EditorUtility.DisplayDialog(
											 "Run Project Setup",
											 summary + "\n\nThis runs as one package transaction, then imports the cached " +
											 "Asset Store packages without prompting. The editor will recompile.",
											 "Run", "Cancel"))
				return;

			PackageInstaller.Run(profile, plan);
		}
	}
}
