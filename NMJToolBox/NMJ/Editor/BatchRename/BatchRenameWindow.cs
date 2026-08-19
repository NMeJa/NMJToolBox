using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

namespace NMJToolBox
{
	/// <summary>
	/// Batch rename for Hierarchy objects and Project assets.
	///
	/// Opened by F2 when more than one object is selected — a single selection falls through to
	/// Unity's own inline rename, which is faster for the one-object case and is what everyone's
	/// fingers already expect.
	/// </summary>
	public class BatchRenameWindow : EditorWindow
	{
		private const string PatternPrefKey = "NMJ.BatchRename.Pattern";

		private string pattern = "{N}";
		private string find = string.Empty;
		private string replace = string.Empty;
		private bool useRegex;
		private bool matchCase = true;

		private string prefix = string.Empty;
		private string suffix = string.Empty;
		private int trimStart;
		private int trimEnd;

		private RenameOrder order = RenameOrder.Natural;

		private List<RenameTarget> targets = new();
		private HashSet<string> collisions = new();
		private List<string> invalid = new();
		private string selectionError;
		private string regexError;

		private Vector2 previewScroll;
		private bool showTokenHelp;

		[MenuItem("Tools/NMJ/Batch Rename %#r", false, 20)]
		[MenuItem("GameObject/NMJ/Batch Rename", false, 11)]
		public static void Open()
		{
			var window = GetWindow<BatchRenameWindow>(true, "Batch Rename");
			window.minSize = new Vector2(520, 420);
			window.Refresh();
			window.ShowUtility();
		}

		[MenuItem("Assets/NMJ/Batch Rename", false, 20)]
		private static void OpenFromProject() => Open();

		[MenuItem("Tools/NMJ/Batch Rename %#r", true)]
		[MenuItem("Assets/NMJ/Batch Rename", true)]
		private static bool ValidateOpen() => Selection.objects is { Length: > 0 };

		private void OnEnable()
		{
			pattern = EditorPrefs.GetString(PatternPrefKey, "{N}");
			Selection.selectionChanged += Refresh;
		}

		private void OnDisable()
		{
			Selection.selectionChanged -= Refresh;
			EditorPrefs.SetString(PatternPrefKey, pattern);
		}

		private void Refresh()
		{
			targets = RenameTarget.FromSelection(order, out selectionError);
			Recalculate();
			Repaint();
		}

		/// <summary>
		/// Recomputes every preview name. Operations compose in a fixed order — trim, then
		/// find/replace, then the pattern, then prefix/suffix — and {N} sees the result of the
		/// steps before it, so a pattern can build on a replace rather than fighting it.
		/// </summary>
		private void Recalculate()
		{
			regexError = null;
			Regex regex = null;

			if (useRegex && !string.IsNullOrEmpty(find))
			{
				try
				{
					regex = new Regex(find, matchCase ? RegexOptions.None : RegexOptions.IgnoreCase);
				}
				catch (System.ArgumentException e)
				{
					regexError = e.Message;
				}
			}

			for (var i = 0; i < targets.Count; i++)
			{
				var target = targets[i];
				var working = target.CurrentName;

				if (trimStart > 0) working = working.Length > trimStart ? working[trimStart..] : string.Empty;
				if (trimEnd > 0) working = working.Length > trimEnd ? working[..^trimEnd] : string.Empty;

				if (!string.IsNullOrEmpty(find))
				{
					if (regex != null) working = regex.Replace(working, replace ?? string.Empty);
					else if (!useRegex) working = ReplaceLiteral(working, find, replace ?? string.Empty, matchCase);
				}

				var context = target.BuildContext(working, i, targets.Count);
				var result = RenamePattern.Apply(pattern, context);

				target.NewName = prefix + result + suffix;
			}

			collisions = RenameTarget.FindCollisions(targets);
			invalid = RenameTarget.FindInvalid(targets);
		}

		/// <summary>
		/// Case-aware literal replace. Written out rather than using the
		/// <c>string.Replace(string, string, StringComparison)</c> overload, which only exists on
		/// .NET Standard 2.1 — a project set to the .NET Framework API compatibility level would
		/// fail to compile against it.
		/// </summary>
		private static string ReplaceLiteral(string source, string search, string replacement, bool caseSensitive)
		{
			if (string.IsNullOrEmpty(source) || string.IsNullOrEmpty(search)) return source;

			var comparison = caseSensitive ? System.StringComparison.Ordinal : System.StringComparison.OrdinalIgnoreCase;
			var builder = new System.Text.StringBuilder(source.Length);
			var cursor = 0;

			while (true)
			{
				var hit = source.IndexOf(search, cursor, comparison);
				if (hit < 0) break;

				builder.Append(source, cursor, hit - cursor).Append(replacement);
				cursor = hit + search.Length;
			}

			builder.Append(source, cursor, source.Length - cursor);
			return builder.ToString();
		}

		private void OnGUI()
		{
			if (!string.IsNullOrEmpty(selectionError))
			{
				EditorGUILayout.HelpBox(selectionError, MessageType.Warning);
				return;
			}

			if (targets.Count == 0)
			{
				EditorGUILayout.HelpBox("Select objects in the Hierarchy or Project window.", MessageType.Info);
				return;
			}

			EditorGUI.BeginChangeCheck();

			DrawHeader();
			DrawPatternSection();
			DrawFindReplaceSection();
			DrawAffixSection();

			if (EditorGUI.EndChangeCheck()) Recalculate();

			DrawPreview();
			DrawFooter();
		}

		private void DrawHeader()
		{
			using (new EditorGUILayout.HorizontalScope(EditorStyles.helpBox))
			{
				var kind = targets[0].IsAsset ? "asset" : "object";
				EditorGUILayout.LabelField($"{targets.Count} {kind}{(targets.Count == 1 ? "" : "s")} selected",
										   EditorStyles.boldLabel);
				GUILayout.FlexibleSpace();

				EditorGUI.BeginChangeCheck();
				order = (RenameOrder)EditorGUILayout.EnumPopup(order, GUILayout.Width(160));
				if (EditorGUI.EndChangeCheck()) Refresh();
			}
		}

		private void DrawPatternSection()
		{
			EditorGUILayout.Space(4);
			EditorGUILayout.LabelField("Pattern", EditorStyles.boldLabel);

			using (new EditorGUILayout.HorizontalScope())
			{
				pattern = EditorGUILayout.TextField(pattern);
				if (GUILayout.Button("?", GUILayout.Width(24))) showTokenHelp = !showTokenHelp;
			}

			using (new EditorGUILayout.HorizontalScope())
			{
				GUILayout.Label("Insert:", GUILayout.Width(44));
				InsertButton("{N}", "Original name");
				InsertButton("{C}", "Counter from 1");
				InsertButton("{C:1,1,2}", "Zero-padded counter: 01, 02, 03");
				InsertButton("{I}", "Index from 0");
				InsertButton("{A}", "Letters: a, b, c");
				InsertButton("{P}", "Parent / folder");
				InsertButton("{TY}", "Type name");
				GUILayout.FlexibleSpace();
			}

			if (showTokenHelp)
			{
				EditorGUILayout.TextArea(RenamePattern.Reference, EditorStyles.miniLabel);
			}
		}

		private void InsertButton(string token, string tooltip)
		{
			if (!GUILayout.Button(new GUIContent(token, tooltip), EditorStyles.miniButton)) return;
			pattern += token;
			GUI.FocusControl(null);
			Recalculate();
		}

		private void DrawFindReplaceSection()
		{
			EditorGUILayout.Space(4);
			EditorGUILayout.LabelField("Find & Replace", EditorStyles.boldLabel);

			using (new EditorGUILayout.HorizontalScope())
			{
				find = EditorGUILayout.TextField("Find", find);
				replace = EditorGUILayout.TextField("Replace", replace);
			}

			using (new EditorGUILayout.HorizontalScope())
			{
				GUILayout.Space(EditorGUIUtility.labelWidth);
				useRegex = EditorGUILayout.ToggleLeft("Regex", useRegex, GUILayout.Width(70));
				matchCase = EditorGUILayout.ToggleLeft("Match case", matchCase, GUILayout.Width(100));
				GUILayout.FlexibleSpace();
			}

			if (!string.IsNullOrEmpty(regexError))
				EditorGUILayout.HelpBox($"Invalid regex: {regexError}", MessageType.Error);
		}

		private void DrawAffixSection()
		{
			EditorGUILayout.Space(4);
			EditorGUILayout.LabelField("Affixes & Trim", EditorStyles.boldLabel);

			using (new EditorGUILayout.HorizontalScope())
			{
				prefix = EditorGUILayout.TextField("Prefix", prefix);
				suffix = EditorGUILayout.TextField("Suffix", suffix);
			}

			using (new EditorGUILayout.HorizontalScope())
			{
				trimStart = Mathf.Max(0, EditorGUILayout.IntField("Trim start", trimStart));
				trimEnd = Mathf.Max(0, EditorGUILayout.IntField("Trim end", trimEnd));
			}
		}

		private void DrawPreview()
		{
			EditorGUILayout.Space(6);
			EditorGUILayout.LabelField("Preview", EditorStyles.boldLabel);

			using var scroll = new EditorGUILayout.ScrollViewScope(previewScroll, EditorStyles.helpBox);
			previewScroll = scroll.scrollPosition;

			foreach (var target in targets)
			{
				using (new EditorGUILayout.HorizontalScope())
				{
					var changed = target.Changed;
					var colliding = collisions.Contains(target.NewName);

					using (new EditorGUI.DisabledScope(!changed))
						EditorGUILayout.LabelField(target.CurrentName, GUILayout.MinWidth(120));

					GUILayout.Label("→", GUILayout.Width(18));

					var style = new GUIStyle(EditorStyles.label)
					{
						normal = { textColor = colliding ? new Color(1f, 0.55f, 0.4f) : (changed ? new Color(0.5f, 0.9f, 0.5f) : EditorStyles.label.normal.textColor) }
					};

					EditorGUILayout.LabelField(target.NewName, style, GUILayout.MinWidth(120));
				}
			}
		}

		private void DrawFooter()
		{
			var blocking = invalid.Count > 0;

			if (collisions.Count > 0)
			{
				var message = targets[0].IsAsset
								  ? $"{collisions.Count} name(s) would collide. Unity will append numbers to keep asset paths unique."
								  : $"{collisions.Count} name(s) would collide. Scene objects allow duplicates, so this is only a warning.";
				EditorGUILayout.HelpBox(message, MessageType.Warning);
			}

			if (blocking)
				EditorGUILayout.HelpBox($"{invalid.Count} name(s) are empty or contain characters that " +
										"cannot be used in a file name.", MessageType.Error);

			using (new EditorGUILayout.HorizontalScope())
			{
				GUILayout.FlexibleSpace();

				if (GUILayout.Button("Close", GUILayout.Width(80))) Close();

				using (new EditorGUI.DisabledScope(blocking))
				{
					if (GUILayout.Button("Rename", GUILayout.Width(110))) ApplyRenames();
				}
			}
		}

		private void ApplyRenames()
		{
			var errors = new List<string>();
			var isAssets = targets.Count > 0 && targets[0].IsAsset;

			// One Undo group so a whole batch reverts with a single Ctrl+Z. Assets are outside the
			// Undo system entirely — AssetDatabase.RenameAsset is not undoable — hence the warning.
			var undoGroup = Undo.GetCurrentGroup();
			Undo.SetCurrentGroupName("Batch Rename");

			if (isAssets) AssetDatabase.StartAssetEditing();

			try
			{
				foreach (var target in targets)
				{
					var error = target.Apply();
					if (!string.IsNullOrEmpty(error)) errors.Add(error);
				}
			}
			finally
			{
				if (isAssets)
				{
					AssetDatabase.StopAssetEditing();
					AssetDatabase.Refresh();
				}

				Undo.CollapseUndoOperations(undoGroup);
			}

			if (errors.Count > 0)
			{
				Debug.LogError($"[NMJ] Batch rename finished with {errors.Count} error(s):\n" + string.Join("\n", errors));
			}

			Refresh();
		}
	}
}
