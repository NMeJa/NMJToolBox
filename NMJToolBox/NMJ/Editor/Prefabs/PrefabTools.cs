using System.Collections.Generic;
using System.Linq;
using NMJTools.Extensions;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace NMJTools.Prefabs
{
	/// <summary>
	/// Prefab operations Unity leaves to hand-work: swapping a selection for a prefab, finding every
	/// instance of one, and hunting down missing scripts.
	/// </summary>
	public static class PrefabTools
	{
		// ========== Select instances ==========

		[MenuItem("Assets/NMJ/Select Instances In Open Scenes", false, 30)]
		private static void SelectInstances()
		{
			var prefab = Selection.activeObject as GameObject;
			if (!prefab) return;

			var matches = FindInstances(prefab);

			if (matches.Count == 0)
			{
				EditorUtility.DisplayDialog("No Instances",
											$"No instances of '{prefab.name}' found in the open scenes.\n\n" +
											"Only loaded scenes are searched — open the scenes you want to include.",
											"OK");
				return;
			}

			Selection.objects = matches.Cast<Object>().ToArray();
			Debug.Log($"[NMJ] Selected {matches.Count} instance(s) of '{prefab.name}'.");
		}

		[MenuItem("Assets/NMJ/Select Instances In Open Scenes", true)]
		private static bool ValidateSelectInstances() =>
			Selection.activeObject is GameObject go && PrefabUtility.IsPartOfPrefabAsset(go);

		private static List<GameObject> FindInstances(GameObject prefab)
		{
			var prefabPath = AssetDatabase.GetAssetPath(prefab);
			var matches = new List<GameObject>();

			// ReSharper disable LoopCanBeConvertedToQuery
			foreach (var root in LoadedRootObjects())
			{
				foreach (var transform in root.GetComponentsInChildren<Transform>(true))
				{
					var go = transform.gameObject;
					if (!PrefabUtility.IsAnyPrefabInstanceRoot(go)) continue;

					var source = PrefabUtility.GetCorrespondingObjectFromOriginalSource(go);
					if (!source) continue;

					if (AssetDatabase.GetAssetPath(source) == prefabPath) matches.Add(go);
				}
			}
			// ReSharper restore LoopCanBeConvertedToQuery

			return matches;
		}

		// ========== Replace with prefab ==========

		[MenuItem("GameObject/NMJ/Replace With Prefab...", false, 12)]
		private static void OpenReplaceWindow() => ReplaceWithPrefabWindow.Open();

		[MenuItem("GameObject/NMJ/Replace With Prefab...", true)]
		private static bool ValidateReplace() => Selection.gameObjects is { Length: > 0 };

		/// <summary>
		/// Swaps each selected object for an instance of <paramref name="prefab"/>, preserving its
		/// place in the world and the hierarchy. Returns how many were replaced.
		/// </summary>
		public static int Replace(IReadOnlyList<GameObject> targets, GameObject prefab,
								  bool keepName, bool keepScale, bool keepLayerAndTag)
		{
			if (!prefab || targets == null) return 0;

			var replaced = 0;
			var created = new List<Object>(targets.Count);

			foreach (var target in targets)
			{
				if (!target) continue;
				if (PrefabUtility.IsPartOfPrefabAsset(target)) continue;

				var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, target.scene);
				if (!instance) continue;

				Undo.RegisterCreatedObjectUndo(instance, "Replace With Prefab");

				var source = target.transform;
				var destination = instance.transform;

				// Sibling index must be set after parenting, and both must happen before the original
				// is destroyed or the index shifts underneath us.
				Undo.SetTransformParent(destination, source.parent, "Replace With Prefab");
				destination.SetPositionAndRotation(source.position, source.rotation);
				if (keepScale) destination.localScale = source.localScale;
				destination.SetSiblingIndex(source.GetSiblingIndex());

				if (keepName) instance.name = target.name;

				if (keepLayerAndTag)
				{
					instance.layer = target.layer;
					instance.tag = target.tag;
				}

				instance.SetActive(target.activeSelf);

				created.Add(instance);
				Undo.DestroyObjectImmediate(target);
				replaced++;
			}

			if (created.Count > 0) Selection.objects = created.ToArray();
			return replaced;
		}

		// ========== Missing scripts ==========

		[MenuItem("Tools/NMJ/Find Missing Scripts In Open Scenes", false, 40)]
		private static void FindMissingScripts()
		{
			var affected = new List<GameObject>();
			var total = 0;

			foreach (var root in LoadedRootObjects())
			{
				foreach (var transform in root.GetComponentsInChildren<Transform>(true))
				{
					var go = transform.gameObject;
					var count = GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(go);
					if (count <= 0) continue;

					affected.Add(go);
					total += count;
				}
			}

			if (affected.Count == 0)
			{
				Debug.Log("[NMJ] No missing scripts found in the open scenes.");
				EditorUtility.DisplayDialog("Missing Scripts", "None found in the open scenes.", "OK");
				return;
			}

			Selection.objects = affected.Cast<Object>().ToArray();

			foreach (var go in affected)
				Debug.LogWarning($"[NMJ] Missing script on '{go.transform.GetPath()}'", go);

			// ReSharper disable once InvertIf
			if (EditorUtility.DisplayDialog("Missing Scripts",
											$"Found {total} missing script(s) on {affected.Count} object(s).\n\n" +
											"They are now selected and listed in the Console.\n\n" +
											"Remove the empty components?",
											"Remove All", "Leave Them"))
			{
				var removed = affected.Sum(GameObjectUtility.RemoveMonoBehavioursWithMissingScript);
				Debug.Log($"[NMJ] Removed {removed} missing script component(s).");
			}
		}

		private static IEnumerable<GameObject> LoadedRootObjects()
		{
			for (var i = 0; i < SceneManager.sceneCount; i++)
			{
				var scene = SceneManager.GetSceneAt(i);
				if (!scene.isLoaded) continue;

				foreach (var root in scene.GetRootGameObjects()) yield return root;
			}
		}
	}

	/// <summary>Options dialog for <see cref="PrefabTools.Replace"/>.</summary>
	public class ReplaceWithPrefabWindow : EditorWindow
	{
		private GameObject prefab;
		private bool keepName = true;
		private bool keepScale = true;
		private bool keepLayerAndTag;

		public static void Open()
		{
			var window = GetWindow<ReplaceWithPrefabWindow>(true, "Replace With Prefab");
			window.minSize = new Vector2(340, 190);
			window.maxSize = new Vector2(500, 190);
			window.ShowUtility();
		}

		private void OnGUI()
		{
			var targets = Selection.gameObjects.Where(go => !PrefabUtility.IsPartOfPrefabAsset(go)).ToList();

			EditorGUILayout.Space(4);
			EditorGUILayout.LabelField($"{targets.Count} scene object(s) selected", EditorStyles.boldLabel);
			EditorGUILayout.Space(4);

			prefab = (GameObject)EditorGUILayout.ObjectField("Prefab", prefab, typeof(GameObject), false);

			EditorGUILayout.Space(4);
			keepName = EditorGUILayout.Toggle("Keep original name", keepName);
			keepScale = EditorGUILayout.Toggle("Keep original scale", keepScale);
			keepLayerAndTag = EditorGUILayout.Toggle("Keep layer and tag", keepLayerAndTag);

			EditorGUILayout.Space(6);
			EditorGUILayout.HelpBox("Position, rotation, parent, sibling index and active state are always preserved.",
									MessageType.Info);

			GUILayout.FlexibleSpace();

			using (new EditorGUILayout.HorizontalScope())
			{
				GUILayout.FlexibleSpace();
				if (GUILayout.Button("Cancel", GUILayout.Width(80))) Close();

				using (new EditorGUI.DisabledScope(!prefab || targets.Count == 0))
				{
					if (!GUILayout.Button("Replace", GUILayout.Width(110))) return;

					var group = Undo.GetCurrentGroup();
					Undo.SetCurrentGroupName("Replace With Prefab");
					var count = PrefabTools.Replace(targets, prefab, keepName, keepScale, keepLayerAndTag);
					Undo.CollapseUndoOperations(group);

					Debug.Log($"[NMJ] Replaced {count} object(s) with '{prefab.name}'.");
					Close();
				}
			}
		}
	}
}