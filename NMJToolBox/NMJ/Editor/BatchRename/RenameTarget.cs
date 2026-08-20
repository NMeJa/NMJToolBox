using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace NMJTools.BatchRename
{
	/// <summary>How the selection is ordered before counters are assigned.</summary>
	public enum RenameOrder
	{
		/// <summary>Hierarchy order for GameObjects, path order for assets.</summary>
		Natural,
		Alphabetical,
		AlphabeticalDescending,

		/// <summary>Whatever order Unity handed back in <see cref="Selection.objects"/>.</summary>
		SelectionOrder
	}

	/// <summary>
	/// One renameable thing. Hides the fact that renaming a GameObject (assign <c>name</c>, record
	/// an Undo) and renaming an asset (<see cref="AssetDatabase.RenameAsset"/>, no Undo, extension
	/// must be stripped) have almost nothing in common.
	/// </summary>
	[SuppressMessage("ReSharper", "MemberCanBePrivate.Global")]
	public class RenameTarget
	{
		public UnityEngine.Object Object { get; private set; }

		/// <summary>Current name, without extension for assets.</summary>
		public string CurrentName { get; private set; }

		public string NewName { get; set; }

		public bool IsAsset { get; private set; }
		public string AssetPath { get; private set; }
		public string Extension { get; private set; }
		public string ParentName { get; private set; }
		public string TypeName { get; private set; }

		/// <summary>Chain of sibling indices root→leaf, used to sort GameObjects in hierarchy order.</summary>
		private int[] hierarchyKey = Array.Empty<int>();

		public bool Changed => !string.Equals(CurrentName, NewName, StringComparison.Ordinal);

		/// <summary>
		/// Builds targets from the current selection. Returns an empty list when the selection mixes
		/// scene objects and assets — renaming those together would need two different code paths
		/// and two different Undo stories, and the resulting counter-order would be arbitrary.
		/// </summary>
		public static List<RenameTarget> FromSelection(RenameOrder order, out string error)
		{
			error = null;
			var result = new List<RenameTarget>();

			var objects = Selection.objects;
			if (objects == null || objects.Length == 0) return result;

			var gameObjects = new List<GameObject>();
			var assets = new List<UnityEngine.Object>();

			foreach (var obj in objects)
			{
				if (!obj) continue;

				if (AssetDatabase.Contains(obj)) assets.Add(obj);
				else if (obj is GameObject go) gameObjects.Add(go);
			}

			if (gameObjects.Count > 0 && assets.Count > 0)
			{
				error = "The selection mixes scene objects and project assets. Batch rename handles " +
						"one or the other, so counters stay in a predictable order.";
				return result;
			}

			result.AddRange(gameObjects.Select(FromGameObject));
			result.AddRange(assets.Select(FromAsset));

			Sort(result, order);
			return result;
		}

		private static RenameTarget FromGameObject(GameObject go)
		{
			var t = go.transform;

			var target = new RenameTarget
			{
				Object = go,
				CurrentName = go.name,
				NewName = go.name,
				IsAsset = false,
				Extension = string.Empty,
				ParentName = t.parent ? t.parent.name : go.scene.name,
				TypeName = FirstMeaningfulComponent(go),
				hierarchyKey = BuildHierarchyKey(t)
			};

			return target;
		}

		private static RenameTarget FromAsset(UnityEngine.Object asset)
		{
			var path = AssetDatabase.GetAssetPath(asset);
			var extension = Path.GetExtension(path);

			return new RenameTarget
			{
				Object = asset,
				CurrentName = Path.GetFileNameWithoutExtension(path),
				NewName = Path.GetFileNameWithoutExtension(path),
				IsAsset = true,
				AssetPath = path,
				Extension = string.IsNullOrEmpty(extension) ? string.Empty : extension.TrimStart('.'),
				ParentName = new DirectoryInfo(Path.GetDirectoryName(path) ?? string.Empty).Name,
				TypeName = asset.GetType().Name
			};
		}

		/// <summary>
		/// The component a human would say the object "is". Transform is skipped because every
		/// GameObject has one, which would make {TY} useless.
		/// </summary>
		private static string FirstMeaningfulComponent(GameObject go)
		{
			var components = go.GetComponents<Component>();
			foreach (var component in components)
			{
				if (!component) continue; // missing script
				if (component is Transform) continue;
				return component.GetType().Name;
			}

			return nameof(GameObject);
		}

		private static int[] BuildHierarchyKey(Transform t)
		{
			var chain = new List<int>(8);
			for (var current = t; current; current = current.parent)
				chain.Add(current.GetSiblingIndex());

			chain.Reverse();
			return chain.ToArray();
		}

		private static void Sort(List<RenameTarget> targets, RenameOrder order)
		{
			switch (order)
			{
				case RenameOrder.Alphabetical:
					targets.Sort((a, b) => EditorUtility.NaturalCompare(a.CurrentName, b.CurrentName));
					break;

				case RenameOrder.AlphabeticalDescending:
					targets.Sort((a, b) => EditorUtility.NaturalCompare(b.CurrentName, a.CurrentName));
					break;

				case RenameOrder.SelectionOrder:
					break;

				case RenameOrder.Natural:
				default:
					targets.Sort(CompareNatural);
					break;
			}
		}

		private static int CompareNatural(RenameTarget a, RenameTarget b)
		{
			if (a.IsAsset || b.IsAsset)
				return string.CompareOrdinal(a.AssetPath, b.AssetPath);

			// Lexicographic comparison of the sibling-index chains reproduces exactly the top-to-bottom
			// order the Hierarchy window draws, across nesting levels.
			var keyA = a.hierarchyKey;
			var keyB = b.hierarchyKey;
			var shared = Mathf.Min(keyA.Length, keyB.Length);

			for (var i = 0; i < shared; i++)
			{
				if (keyA[i] != keyB[i]) return keyA[i].CompareTo(keyB[i]);
			}

			return keyA.Length.CompareTo(keyB.Length);
		}

		public RenameContext BuildContext(string workingName, int index, int total) =>
			new(workingName, index, total, ParentName, TypeName, Extension);

		/// <summary>Applies <see cref="NewName"/>. Returns an error message or null on success.</summary>
		public string Apply()
		{
			if (!Changed) return null;

			if (IsAsset)
			{
				var error = AssetDatabase.RenameAsset(AssetPath, NewName);
				if (!string.IsNullOrEmpty(error)) return $"{CurrentName}: {error}";
				CurrentName = NewName;
				AssetPath = AssetDatabase.GetAssetPath(Object);
				return null;
			}

			var go = (GameObject)Object;
			Undo.RecordObject(go, "Batch Rename");
			go.name = NewName;
			EditorUtility.SetDirty(go);
			CurrentName = NewName;
			return null;
		}

		/// <summary>Names that appear more than once in the batch, so the window can warn about them.</summary>
		public static HashSet<string> FindCollisions(IReadOnlyList<RenameTarget> targets)
		{
			var seen = new HashSet<string>(StringComparer.Ordinal);
			var duplicates = new HashSet<string>(StringComparer.Ordinal);

			foreach (var target in targets)
				if (!seen.Add(target.NewName))
					duplicates.Add(target.NewName);

			return duplicates;
		}

		/// <summary>
		/// Asset names that are empty or contain characters the filesystem will reject. GameObjects
		/// are exempt: Unity allows almost anything in a scene object's name.
		/// </summary>
		public static List<string> FindInvalid(IReadOnlyList<RenameTarget> targets)
		{
			var invalidChars = Path.GetInvalidFileNameChars();
			return (from target in targets
					where string.IsNullOrWhiteSpace(target.NewName) ||
						  (target.IsAsset && target.NewName.IndexOfAny(invalidChars) >= 0)
					select target.CurrentName).ToList();
		}
	}
}