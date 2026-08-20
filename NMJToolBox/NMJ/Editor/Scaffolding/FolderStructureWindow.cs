using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace NMJTools.Scaffolding
{
	/// <summary>
	/// Wizard for creating a project folder structure: pick a built-in layout or a project template
	/// asset, edit it freely, see exactly what will be created, then run it.
	///
	/// The edited layout is always a copy, so tweaking a built-in or an asset never mutates the
	/// original — changes only reach disk through <b>Save as Template…</b>.
	/// </summary>
	public class FolderStructureWindow : EditorWindow
	{
		private const string LastTemplateKey = "NMJ.Scaffold.LastTemplate";
		private const string LastRootKey = "NMJ.Scaffold.LastRoot";

		private FolderLayout layout;
		private List<FolderTemplate> projectTemplates = new();
		private string[] templateNames = System.Array.Empty<string>();
		private int templateIndex;

		private string targetParent = "Assets";
		private Vector2 folderScroll;
		private Vector2 previewScroll;
		private string newFolderEntry = string.Empty;
		private bool showPreview = true;

		[MenuItem("Tools/NMJ/Setup/Create Folder Structure", false, 280)]
		public static void Open()
		{
			var window = GetWindow<FolderStructureWindow>(true, "Create Folder Structure");
			window.minSize = new Vector2(620, 560);
			window.Show();
		}

		private void OnEnable()
		{
			RefreshTemplateList();

			var remembered = EditorPrefs.GetString(LastTemplateKey, "Standard");
			templateIndex = Mathf.Max(0, System.Array.FindIndex(templateNames, n => n == remembered));
			LoadSelectedTemplate();

			var rememberedRoot = EditorPrefs.GetString(LastRootKey, string.Empty);
			if (!string.IsNullOrEmpty(rememberedRoot)) layout.rootFolderName = rememberedRoot;

			UseSelectionAsTarget();
		}

		private void RefreshTemplateList()
		{
			projectTemplates = FolderTemplateLibrary.FindProjectTemplates();

			var names = FolderTemplateLibrary.BuiltIn.Select(l => l.layoutName).ToList();
			names.AddRange(projectTemplates.Select(t => $"Project/{t.name}"));
			templateNames = names.ToArray();
		}

		private void LoadSelectedTemplate()
		{
			var builtInCount = FolderTemplateLibrary.BuiltIn.Count;

			layout = templateIndex < builtInCount
						 ? FolderTemplateLibrary.BuiltIn[templateIndex].Clone()
						 : projectTemplates[templateIndex - builtInCount].CloneLayout();
		}

		/// <summary>Defaults the target to the selected Project-window folder if there is one.</summary>
		private void UseSelectionAsTarget()
		{
			var selected = Selection.activeObject;
			if (!selected) return;

			var path = AssetDatabase.GetAssetPath(selected);
			if (string.IsNullOrEmpty(path)) return;

			targetParent = AssetDatabase.IsValidFolder(path) ? path : Path.GetDirectoryName(path)?.Replace('\\', '/');
			if (string.IsNullOrEmpty(targetParent)) targetParent = "Assets";
		}

		private void OnGUI()
		{
			DrawTemplatePicker();
			DrawTarget();
			DrawOptions();
			DrawFolderList();
			DrawPreview();
			DrawFooter();
		}

		private void DrawTemplatePicker()
		{
			EditorGUILayout.Space(6);
			EditorGUILayout.LabelField("Template", EditorStyles.boldLabel);

			using (new EditorGUILayout.HorizontalScope())
			{
				EditorGUI.BeginChangeCheck();
				templateIndex = EditorGUILayout.Popup(templateIndex, templateNames);
				if (EditorGUI.EndChangeCheck())
				{
					LoadSelectedTemplate();
					EditorPrefs.SetString(LastTemplateKey, templateNames[templateIndex]);
					GUI.FocusControl(null);
				}

				if (GUILayout.Button("Reload", GUILayout.Width(70)))
				{
					RefreshTemplateList();
					templateIndex = Mathf.Clamp(templateIndex, 0, templateNames.Length - 1);
					LoadSelectedTemplate();
				}
			}

			if (!string.IsNullOrWhiteSpace(layout.description))
				EditorGUILayout.LabelField(layout.description, EditorStyles.wordWrappedMiniLabel);
		}

		private void DrawTarget()
		{
			EditorGUILayout.Space(6);
			EditorGUILayout.LabelField("Location", EditorStyles.boldLabel);

			using (new EditorGUILayout.HorizontalScope())
			{
				targetParent = EditorGUILayout.TextField("Create in", targetParent);
				if (GUILayout.Button("Use Selection", GUILayout.Width(110))) UseSelectionAsTarget();
			}

			EditorGUI.BeginChangeCheck();
			layout.rootFolderName = EditorGUILayout.TextField("Root folder", layout.rootFolderName);
			if (EditorGUI.EndChangeCheck()) EditorPrefs.SetString(LastRootKey, layout.rootFolderName);

			if (!AssetDatabase.IsValidFolder(targetParent))
				EditorGUILayout.HelpBox($"'{targetParent}' is not an existing folder.", MessageType.Error);

			if (FolderLayout.SanitiseFolderName(layout.rootFolderName) == null)
				EditorGUILayout.HelpBox("Root folder name is empty or contains only invalid characters.",
										MessageType.Error);
		}

		private void DrawOptions()
		{
			EditorGUILayout.Space(6);
			EditorGUILayout.LabelField("Options", EditorStyles.boldLabel);

			layout.createGitKeep = EditorGUILayout.ToggleLeft(
															  new GUIContent("Write .gitkeep files",
																			 "Git cannot track an empty directory. Without these, only the orphaned " +
																			 ".meta reaches the repository and Unity deletes it on clone — the whole " +
																			 "structure disappears for everyone but you."),
															  layout.createGitKeep);

			layout.createAssemblyDefinitions = EditorGUILayout.ToggleLeft(
																		  new GUIContent("Create assembly definitions",
																						 "Adds <Root>.Runtime and <Root>.Editor asmdefs, with the editor one " +
																						 "referencing the runtime one by GUID. Needs Runtime and/or Editor folders."),
																		  layout.createAssemblyDefinitions);

			layout.createReadme = EditorGUILayout.ToggleLeft(
															 new GUIContent("Write README.md in the root",
																			"Documents the layout so the convention travels with the project."),
															 layout.createReadme);
		}

		private void DrawFolderList()
		{
			EditorGUILayout.Space(6);

			using (new EditorGUILayout.HorizontalScope())
			{
				EditorGUILayout.LabelField($"Folders ({layout.folders.Count})", EditorStyles.boldLabel);
				GUILayout.FlexibleSpace();
				if (GUILayout.Button("Clear", EditorStyles.miniButton, GUILayout.Width(60)))
					layout.folders.Clear();
			}

			using (var scope = new EditorGUILayout.ScrollViewScope(folderScroll, EditorStyles.helpBox,
																   GUILayout.MinHeight(120), GUILayout.MaxHeight(200)))
			{
				folderScroll = scope.scrollPosition;

				var removeAt = -1;
				for (var i = 0; i < layout.folders.Count; i++)
				{
					using (new EditorGUILayout.HorizontalScope())
					{
						layout.folders[i] = EditorGUILayout.TextField(layout.folders[i]);
						if (GUILayout.Button(new GUIContent("−", "Remove"), EditorStyles.miniButton, GUILayout.Width(24)))
							removeAt = i;
					}
				}

				if (removeAt >= 0) layout.folders.RemoveAt(removeAt);

				if (layout.folders.Count == 0)
					EditorGUILayout.LabelField("No folders — only the root will be created.",
											   EditorStyles.centeredGreyMiniLabel);
			}

			using (new EditorGUILayout.HorizontalScope())
			{
				GUI.SetNextControlName("NewFolderEntry");
				newFolderEntry = EditorGUILayout.TextField(newFolderEntry);

				var enterPressed = Event.current.type == EventType.KeyDown &&
								   Event.current.keyCode is KeyCode.Return or KeyCode.KeypadEnter &&
								   GUI.GetNameOfFocusedControl() == "NewFolderEntry";

				if ((GUILayout.Button("Add", GUILayout.Width(60)) || enterPressed) &&
					!string.IsNullOrWhiteSpace(newFolderEntry))
				{
					layout.folders.Add(FolderLayout.Normalise(newFolderEntry));
					newFolderEntry = string.Empty;
					Event.current.Use();
					GUI.FocusControl("NewFolderEntry");
				}
			}
		}

		private void DrawPreview()
		{
			EditorGUILayout.Space(6);
			showPreview = EditorGUILayout.Foldout(showPreview, "Preview", true);
			if (!showPreview) return;

			if (FolderLayout.SanitiseFolderName(layout.rootFolderName) == null) return;

			var paths = FolderStructureCreator.ResolvePaths(layout, targetParent);
			var toCreate = paths.Count(p => !FolderStructureCreator.Exists(p));

			EditorGUILayout.LabelField($"{toCreate} to create, {paths.Count - toCreate} already exist",
									   EditorStyles.miniLabel);

			using var scope = new EditorGUILayout.ScrollViewScope(previewScroll, EditorStyles.helpBox,
																  GUILayout.MinHeight(110));
			previewScroll = scope.scrollPosition;

			foreach (var path in paths)
			{
				var exists = FolderStructureCreator.Exists(path);
				var depth = path.Count(c => c == '/');

				var style = new GUIStyle(EditorStyles.label)
				{
					normal =
					{
						textColor = exists
										? EditorStyles.label.normal.textColor * 0.6f
										: new Color(0.45f, 0.85f, 0.45f)
					},
					fontSize = 11
				};

				EditorGUILayout.LabelField($"{new string(' ', depth * 2)}{(exists ? "•" : "+")} {path}", style);
			}
		}

		private void DrawFooter()
		{
			EditorGUILayout.Space(6);

			var rootValid = FolderLayout.SanitiseFolderName(layout.rootFolderName) != null;
			var targetValid = AssetDatabase.IsValidFolder(targetParent);

			using (new EditorGUILayout.HorizontalScope())
			{
				if (GUILayout.Button("Save as Template…", GUILayout.Width(150))) SaveAsTemplate();

				GUILayout.FlexibleSpace();

				if (GUILayout.Button("Close", GUILayout.Width(80))) Close();

				using (new EditorGUI.DisabledScope(!rootValid || !targetValid))
				{
					if (GUILayout.Button("Create", GUILayout.Width(120), GUILayout.Height(24))) Run();
				}
			}
		}

		private void Run()
		{
			var paths = FolderStructureCreator.ResolvePaths(layout, targetParent);
			var toCreate = paths.Count(p => !FolderStructureCreator.Exists(p));

			// Folder creation is not undoable, so confirmation is the only safety net there is.
			if (!EditorUtility.DisplayDialog(
											 "Create Folder Structure",
											 $"Create {toCreate} folder(s) under '{targetParent}/{layout.rootFolderName}'?\n\n" +
											 "This cannot be undone with Ctrl+Z.",
											 "Create", "Cancel"))
				return;

			var result = FolderStructureCreator.Create(layout, targetParent);

			if (result.failed.Count > 0)
				Debug.LogError($"[NMJ] Folder structure: {result.Summary()}\nFailed:\n  " +
							   string.Join("\n  ", result.failed));
			else
				Debug.Log($"[NMJ] Folder structure ({layout.layoutName}): {result.Summary()}");

			var rootPath = paths.First();
			var rootAsset = AssetDatabase.LoadAssetAtPath<DefaultAsset>(rootPath);
			if (rootAsset) EditorGUIUtility.PingObject(rootAsset);

			Repaint();
		}

		private void SaveAsTemplate()
		{
			var path = EditorUtility.SaveFilePanelInProject(
														   "Save Folder Template",
														   $"{layout.layoutName}FolderTemplate",
														   "asset",
														   "Save this layout as a project asset so the team shares it.");

			if (string.IsNullOrEmpty(path)) return;

			var asset = CreateInstance<FolderTemplate>();
			asset.Apply(layout);

			AssetDatabase.CreateAsset(asset, path);
			AssetDatabase.SaveAssets();

			RefreshTemplateList();
			templateIndex = System.Array.FindIndex(templateNames, n => n == $"Project/{Path.GetFileNameWithoutExtension(path)}");
			if (templateIndex < 0) templateIndex = 0;

			EditorGUIUtility.PingObject(asset);
			Debug.Log($"[NMJ] Saved folder template to '{path}'.");
		}
	}
}
