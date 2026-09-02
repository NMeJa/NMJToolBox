using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace NMJTools.Helpers
{
	/// <summary>
	/// Generates an enum-and-switch state machine as a C# file.
	/// </summary>
	/// <remarks>
	/// this changed, in four ways that each made the old output fail to compile or fail to be useful:
	/// <list type="bullet">
	/// <item>With "group by phase" on it emitted three <c>#region</c> directives and no
	/// <c>#endregion</c>, so the generated file did not compile at all.</item>
	/// <item>It still wrote <c>[Foldout("…")]</c> attributes, which came from NaughtyAttributes — a
	/// package this toolbox stopped bundling. The <c>using</c> was removed at some point but the
	/// attribute usage was left behind, so the "use foldouts" option produced an unresolved type.</item>
	/// <item>Output always went to the root of <c>Assets/</c> with no namespace, which is the one
	/// place a generated script should never land.</item>
	/// <item>It generated <c>_currentState</c> and four-space indents, against this project's own
	/// conventions.</item>
	/// </list>
	/// The generated machine is deliberately still the enum-and-switch shape rather than the
	/// class-per-state <see cref="NMJTools.Patterns.StateMachine"/>: they solve different problems, and
	/// a switch is the better fit when the states are a fixed, small, closed set.
	/// </remarks>
	public class StateMachineGenerator : EditorWindow
	{
		private const string LastFolderKey = "NMJ.StateMachineGenerator.Folder";
		private const string LastNamespaceKey = "NMJ.StateMachineGenerator.Namespace";

		private string prefixName = string.Empty;
		private string targetNamespace = string.Empty;
		private string targetFolder = "Assets";

		private readonly List<string> states = new();
		private int defaultState;

		private bool useUnityEvents;
		private bool useRegions;
		private bool spaceRegions;
		private bool groupByPhase;
		private bool codeStyleOpen;

		private Vector2 scroll;

		[MenuItem("Tools/NMJ/State Machine Generator", false, 60)]
		public static void ShowWindow()
		{
			var window = GetWindow<StateMachineGenerator>(true, "State Machine Generator");
			window.minSize = new Vector2(560, 420);
			window.Show();
		}

		private void OnEnable()
		{
			targetFolder = EditorPrefs.GetString(LastFolderKey, "Assets");
			targetNamespace = EditorPrefs.GetString(LastNamespaceKey, string.Empty);

			if (!AssetDatabase.IsValidFolder(targetFolder)) targetFolder = "Assets";
		}

		// ===================================================================================
		// UI
		// ===================================================================================

		private void OnGUI()
		{
			scroll = EditorGUILayout.BeginScrollView(scroll);

			DrawNaming();

			if (!string.IsNullOrEmpty(TypeName))
			{
				DrawStates();
				DrawOptions();
			}

			EditorGUILayout.EndScrollView();

			DrawFooter();
		}

		private void DrawNaming()
		{
			EditorGUILayout.Space(6);
			EditorGUILayout.LabelField("Naming", EditorStyles.boldLabel);

			prefixName = EditorGUILayout.TextField(
												   new GUIContent("Prefix", "Door → DoorState, DoorStateMachine"),
												   prefixName);

			EditorGUI.BeginChangeCheck();
			targetNamespace = EditorGUILayout.TextField(
														new GUIContent("Namespace", "Leave empty to generate into the global namespace."),
														targetNamespace);
			if (EditorGUI.EndChangeCheck()) EditorPrefs.SetString(LastNamespaceKey, targetNamespace);

			using (new EditorGUILayout.HorizontalScope())
			{
				using (new EditorGUI.DisabledScope(true))
					EditorGUILayout.TextField("Folder", targetFolder);

				if (GUILayout.Button("Browse…", GUILayout.Width(80))) BrowseForFolder();
				if (GUILayout.Button("Selection", GUILayout.Width(80))) UseSelectedFolder();
			}

			if (string.IsNullOrEmpty(TypeName))
			{
				EditorGUILayout.HelpBox("Enter a prefix to begin.", MessageType.Info);
				return;
			}

			EditorGUILayout.LabelField($"Will generate  {targetFolder}/{TypeName}StateMachine.cs",
									   EditorStyles.miniLabel);

			if (File.Exists(OutputPath))
				EditorGUILayout.HelpBox("That file already exists and will not be overwritten.", MessageType.Error);
		}

		private void DrawStates()
		{
			EditorGUILayout.Space(8);

			using (new EditorGUILayout.HorizontalScope())
			{
				EditorGUILayout.LabelField($"States ({states.Count})", EditorStyles.boldLabel);
				GUILayout.FlexibleSpace();

				if (GUILayout.Button("−", EditorStyles.miniButtonLeft, GUILayout.Width(26)) && states.Count > 0)
					states.RemoveAt(states.Count - 1);

				if (GUILayout.Button("+", EditorStyles.miniButtonRight, GUILayout.Width(26)))
					states.Add($"STATE_{states.Count}");
			}

			for (var i = 0; i < states.Count; i++)
			{
				using (new EditorGUILayout.HorizontalScope())
				{
					states[i] = SanitiseState(EditorGUILayout.TextField($"State {i}", states[i]));

					// A radio, not a checkbox: exactly one state is the default, so clicking a
					// different one has to move the flag rather than leave two set. The old version
					// used a plain toggle and could not represent that.
					if (EditorGUILayout.Toggle(defaultState == i, EditorStyles.radioButton, GUILayout.Width(20)))
						defaultState = i;

					GUILayout.Label("default", EditorStyles.miniLabel, GUILayout.Width(48));
				}
			}

			if (states.Count == 0)
				EditorGUILayout.HelpBox("Add at least one state.", MessageType.Warning);

			if (defaultState >= states.Count) defaultState = 0;

			var duplicates = states.GroupBy(s => s, StringComparer.Ordinal).Where(g => g.Count() > 1).ToList();
			if (duplicates.Count > 0)
			{
				EditorGUILayout.HelpBox(
										"Duplicate state names: " + string.Join(", ", duplicates.Select(g => g.Key)) +
										". Enum members must be unique.", MessageType.Error);
			}
		}

		private void DrawOptions()
		{
			EditorGUILayout.Space(8);
			codeStyleOpen = EditorGUILayout.Foldout(codeStyleOpen, "Code style", true);
			if (!codeStyleOpen) return;

			useUnityEvents = EditorGUILayout.ToggleLeft(
													   new GUIContent("Generate UnityEvents",
																	  "A serialised enter/exit event per state, so designers can hook them up."),
													   useUnityEvents);

			useRegions = EditorGUILayout.ToggleLeft("Wrap sections in #region", useRegions);

			using (new EditorGUI.DisabledScope(!useRegions))
				spaceRegions = EditorGUILayout.ToggleLeft("Blank line between regions", spaceRegions && useRegions);

			groupByPhase = EditorGUILayout.ToggleLeft(
													  new GUIContent("Group by phase",
																	 "Off: all three methods for a state together. On: every Enter, then every Update, then every Exit."),
													  groupByPhase);
		}

		private void DrawFooter()
		{
			EditorGUILayout.Space(6);

			var blocked = string.IsNullOrEmpty(TypeName) ||
						  states.Count == 0 ||
						  states.Distinct(StringComparer.Ordinal).Count() != states.Count ||
						  File.Exists(OutputPath);

			using (new EditorGUILayout.HorizontalScope())
			{
				GUILayout.FlexibleSpace();

				using (new EditorGUI.DisabledScope(blocked))
				{
					if (GUILayout.Button("Generate", GUILayout.Width(140), GUILayout.Height(26))) Generate();
				}
			}
		}

		private void BrowseForFolder()
		{
			var absolute = EditorUtility.OpenFolderPanel("Generate into", targetFolder, string.Empty);
			if (string.IsNullOrEmpty(absolute)) return;

			var projectRoot = Directory.GetParent(Application.dataPath)?.FullName ?? Application.dataPath;

			if (!absolute.Replace('\\', '/').StartsWith(projectRoot.Replace('\\', '/'), StringComparison.OrdinalIgnoreCase))
			{
				EditorUtility.DisplayDialog("Outside the project",
											"Pick a folder inside this project's Assets directory.", "OK");
				return;
			}

			targetFolder = "Assets" + absolute.Replace('\\', '/')[(projectRoot.Replace('\\', '/').Length + "/Assets".Length)..];
			EditorPrefs.SetString(LastFolderKey, targetFolder);
		}

		private void UseSelectedFolder()
		{
			var path = AssetDatabase.GetAssetPath(Selection.activeObject);
			if (string.IsNullOrEmpty(path)) return;

			targetFolder = AssetDatabase.IsValidFolder(path) ? path : Path.GetDirectoryName(path)?.Replace('\\', '/');
			if (string.IsNullOrEmpty(targetFolder)) targetFolder = "Assets";

			EditorPrefs.SetString(LastFolderKey, targetFolder);
		}

		// ===================================================================================
		// Naming
		// ===================================================================================

		/// <summary>The prefix as a valid PascalCase type-name fragment, or empty.</summary>
		private string TypeName
		{
			get
			{
				var cleaned = new string(prefixName.Where(c => char.IsLetterOrDigit(c) || c == '_').ToArray());
				if (cleaned.Length == 0) return string.Empty;
				if (char.IsDigit(cleaned[0])) cleaned = "_" + cleaned;
				return char.ToUpperInvariant(cleaned[0]) + cleaned[1..];
			}
		}

		private string OutputPath
		{
			get
			{
				var projectRoot = Directory.GetParent(Application.dataPath)?.FullName ?? Application.dataPath;
				return Path.Combine(projectRoot, targetFolder, TypeName + "StateMachine.cs");
			}
		}

		/// <summary>Uppercase, underscore-separated, and always a legal C# identifier.</summary>
		private static string SanitiseState(string raw)
		{
			var cleaned = new string((raw ?? string.Empty).Trim()
													  .Select(c => char.IsLetterOrDigit(c) ? char.ToUpperInvariant(c) : '_')
													  .ToArray());

			cleaned = cleaned.Trim('_');
			if (cleaned.Length == 0) return "STATE";
			return char.IsDigit(cleaned[0]) ? "_" + cleaned : cleaned;
		}

		/// <summary>STATE_NAME → StateName, for the per-state method names.</summary>
		private static string ToPascal(string state)
		{
			var info = CultureInfo.InvariantCulture.TextInfo;
			return info.ToTitleCase(state.Replace("_", " ").ToLowerInvariant()).Replace(" ", string.Empty);
		}

		// ===================================================================================
		// Generation
		// ===================================================================================

		private void Generate()
		{
			var path = OutputPath;

			if (File.Exists(path))
			{
				Debug.LogError($"[NMJ] '{path}' already exists; nothing was written.");
				return;
			}

			var directory = Path.GetDirectoryName(path);
			if (!string.IsNullOrEmpty(directory)) Directory.CreateDirectory(directory);

			File.WriteAllText(path, BuildSource());
			EditorPrefs.SetString(LastFolderKey, targetFolder);

			AssetDatabase.Refresh();

			var asset = AssetDatabase.LoadAssetAtPath<MonoScript>($"{targetFolder}/{TypeName}StateMachine.cs");
			if (asset) EditorGUIUtility.PingObject(asset);

			Debug.Log($"[NMJ] Generated {targetFolder}/{TypeName}StateMachine.cs with {states.Count} state(s).");
		}

		private string BuildSource()
		{
			var builder = new StringBuilder(2048);
			var hasNamespace = !string.IsNullOrWhiteSpace(targetNamespace);
			var indent = hasNamespace ? "\t" : string.Empty;

			builder.AppendLine("using UnityEngine;");
			if (useUnityEvents) builder.AppendLine("using UnityEngine.Events;");
			builder.AppendLine();

			if (hasNamespace)
			{
				builder.AppendLine($"namespace {targetNamespace.Trim()}");
				builder.AppendLine("{");
			}

			BuildEnum(builder, indent);
			builder.AppendLine();
			BuildClass(builder, indent);

			if (hasNamespace) builder.AppendLine("}");

			return builder.ToString();
		}

		private void BuildEnum(StringBuilder builder, string indent)
		{
			Region(builder, indent, "States", true);

			builder.AppendLine($"{indent}public enum {TypeName}State");
			builder.AppendLine($"{indent}{{");

			foreach (var state in states) builder.AppendLine($"{indent}\t{state},");

			builder.AppendLine($"{indent}}}");

			Region(builder, indent, "States", false);
		}

		private void BuildClass(StringBuilder builder, string indent)
		{
			var type = TypeName;
			var inner = indent + "\t";

			builder.AppendLine($"{indent}public class {type}StateMachine : MonoBehaviour");
			builder.AppendLine($"{indent}{{");

			builder.AppendLine($"{inner}public {type}State CurrentState {{ get; private set; }}");
			builder.AppendLine();

			Region(builder, inner, "Unity messages", true);
			builder.AppendLine($"{inner}private void Start()");
			builder.AppendLine($"{inner}{{");
			builder.AppendLine($"{inner}\tOnStateEnter(CurrentState);");
			builder.AppendLine($"{inner}}}");
			builder.AppendLine();
			builder.AppendLine($"{inner}private void Update()");
			builder.AppendLine($"{inner}{{");
			builder.AppendLine($"{inner}\tOnStateUpdate(CurrentState);");
			builder.AppendLine($"{inner}}}");
			Region(builder, inner, "Unity messages", false);
			if (spaceRegions) builder.AppendLine();

			Region(builder, inner, "Transitions", true);
			builder.AppendLine($"{inner}public void TransitionToState({type}State toState)");
			builder.AppendLine($"{inner}{{");
			builder.AppendLine($"{inner}\t// A self-transition would replay Exit and Enter for a state that never changed,");
			builder.AppendLine($"{inner}\t// resetting whatever those methods set up.");
			builder.AppendLine($"{inner}\tif (toState == CurrentState) return;");
			builder.AppendLine();
			builder.AppendLine($"{inner}\tOnStateExit(CurrentState);");
			builder.AppendLine($"{inner}\tCurrentState = toState;");
			builder.AppendLine($"{inner}\tOnStateEnter(toState);");
			builder.AppendLine($"{inner}}}");
			Region(builder, inner, "Transitions", false);
			if (spaceRegions) builder.AppendLine();

			Region(builder, inner, "Dispatch", true);
			BuildSwitch(builder, inner, "OnStateEnter", "OnEnter");
			builder.AppendLine();
			BuildSwitch(builder, inner, "OnStateUpdate", "OnUpdate");
			builder.AppendLine();
			BuildSwitch(builder, inner, "OnStateExit", "OnExit");
			Region(builder, inner, "Dispatch", false);
			if (spaceRegions) builder.AppendLine();

			if (groupByPhase) BuildByPhase(builder, inner);
			else BuildByState(builder, inner);

			builder.AppendLine($"{indent}}}");
		}

		private void BuildSwitch(StringBuilder builder, string indent, string methodName, string handlerPrefix)
		{
			builder.AppendLine($"{indent}private void {methodName}({TypeName}State state)");
			builder.AppendLine($"{indent}{{");
			builder.AppendLine($"{indent}\tswitch (state)");
			builder.AppendLine($"{indent}\t{{");

			foreach (var state in states)
			{
				builder.AppendLine($"{indent}\t\tcase {TypeName}State.{state}:");
				builder.AppendLine($"{indent}\t\t\t{handlerPrefix}{ToPascal(state)}();");
				builder.AppendLine($"{indent}\t\t\tbreak;");
			}

			builder.AppendLine();
			builder.AppendLine($"{indent}\t\tdefault:");
			builder.AppendLine($"{indent}\t\t\tDebug.LogError($\"{methodName}: unhandled state {{state}}\", this);");
			builder.AppendLine($"{indent}\t\t\tbreak;");
			builder.AppendLine($"{indent}\t}}");
			builder.AppendLine($"{indent}}}");
		}

		private void BuildByState(StringBuilder builder, string indent)
		{
			foreach (var state in states)
			{
				var method = ToPascal(state);

				Region(builder, indent, state, true);

				if (useUnityEvents)
				{
					builder.AppendLine($"{indent}public UnityEvent on{method}Enter = new UnityEvent();");
					builder.AppendLine($"{indent}public UnityEvent on{method}Exit = new UnityEvent();");
					builder.AppendLine();
				}

				Handler(builder, indent, "OnEnter", method, useUnityEvents ? $"on{method}Enter" : null);
				builder.AppendLine();
				Handler(builder, indent, "OnUpdate", method, null);
				builder.AppendLine();
				Handler(builder, indent, "OnExit", method, useUnityEvents ? $"on{method}Exit" : null);

				Region(builder, indent, state, false);
				if (spaceRegions) builder.AppendLine();
			}
		}

		private void BuildByPhase(StringBuilder builder, string indent)
		{
			if (useUnityEvents)
			{
				Region(builder, indent, "Events", true);
				foreach (var method in states.Select(ToPascal))
				{
					builder.AppendLine($"{indent}public UnityEvent on{method}Enter = new UnityEvent();");
					builder.AppendLine($"{indent}public UnityEvent on{method}Exit = new UnityEvent();");
				}

				Region(builder, indent, "Events", false);
				if (spaceRegions) builder.AppendLine();
			}

			// Each phase opens and closes its own region. The old version opened three and closed
			// none, which is what stopped the generated file compiling.
			BuildPhase(builder, indent, "Enter", "OnEnter", true);
			BuildPhase(builder, indent, "Update", "OnUpdate", false);
			BuildPhase(builder, indent, "Exit", "OnExit", true);
		}

		private void BuildPhase(StringBuilder builder, string indent, string label, string prefix, bool raisesEvent)
		{
			Region(builder, indent, label, true);

			for (var i = 0; i < states.Count; i++)
			{
				var method = ToPascal(states[i]);
				var eventName = raisesEvent && useUnityEvents ? $"on{method}{label}" : null;

				Handler(builder, indent, prefix, method, eventName);
				if (i < states.Count - 1) builder.AppendLine();
			}

			Region(builder, indent, label, false);
			if (spaceRegions) builder.AppendLine();
		}

		private static void Handler(StringBuilder builder, string indent, string prefix, string method, string eventName)
		{
			builder.AppendLine($"{indent}private void {prefix}{method}()");
			builder.AppendLine($"{indent}{{");
			if (!string.IsNullOrEmpty(eventName)) builder.AppendLine($"{indent}\t{eventName}?.Invoke();");
			builder.AppendLine($"{indent}}}");
		}

		private void Region(StringBuilder builder, string indent, string label, bool open)
		{
			if (!useRegions) return;
			builder.AppendLine(open ? $"{indent}#region {label}" : $"{indent}#endregion");
		}
	}
}
