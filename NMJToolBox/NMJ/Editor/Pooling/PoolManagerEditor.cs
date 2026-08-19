using UnityEditor;
using UnityEngine;

namespace NMJToolBox
{
	/// <summary>
	/// Inspector for <see cref="PoolManager"/>. Adds the thing a pool actually needs at runtime and
	/// that a default inspector cannot show: live active/inactive counts per pool, so you can watch
	/// a pool grow and size the prewarm/max values from evidence rather than guesswork.
	/// </summary>
	[CustomEditor(typeof(PoolManager))]
	public class PoolManagerEditor : Editor
	{
		[MenuItem("GameObject/NMJ/Pool Manager", false, 10)]
		private static void CreatePoolManager()
		{
			var go = new GameObject("Pool Manager", typeof(PoolManager));
			GameObjectUtility.SetParentAndAlign(go, Selection.activeGameObject);
			Undo.RegisterCreatedObjectUndo(go, "Create Pool Manager");
			Selection.activeGameObject = go;
		}

		private static readonly Color BarBackground = new(0f, 0f, 0f, 0.25f);
		private static readonly Color BarActive = new(0.35f, 0.75f, 1f, 0.9f);
		private static readonly Color BarInactive = new(0.35f, 0.75f, 1f, 0.35f);
		private static readonly Color BarOverflow = new(1f, 0.55f, 0.35f, 0.9f);

		public override bool RequiresConstantRepaint() => Application.isPlaying;

		public override void OnInspectorGUI()
		{
			DrawDefaultInspector();

			var manager = (PoolManager)target;

			EditorGUILayout.Space();

			if (!Application.isPlaying)
			{
				EditorGUILayout.HelpBox("Live pool statistics appear here in Play mode.", MessageType.Info);
				return;
			}

			EditorGUILayout.LabelField("Live Statistics", EditorStyles.boldLabel);

			var pools = manager.Pools;
			if (pools == null || pools.Count == 0)
			{
				EditorGUILayout.HelpBox("No pools registered.", MessageType.None);
				return;
			}

			var totalActive = 0;
			var totalAll = 0;

			foreach (var pool in pools)
			{
				if (pool == null) continue;

				totalActive += pool.CountActive;
				totalAll += pool.CountAll;

				DrawPoolRow(pool);
			}

			EditorGUILayout.Space(2);
			EditorGUILayout.LabelField($"Total: {totalActive} active / {totalAll} instances", EditorStyles.miniBoldLabel);
		}

		private static void DrawPoolRow(PrefabPool pool)
		{
			using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
			{
				var overflowing = pool.CountAll > pool.MaxSize;

				using (new EditorGUILayout.HorizontalScope())
				{
					EditorGUILayout.LabelField(pool.Id, EditorStyles.boldLabel);
					GUILayout.FlexibleSpace();
					EditorGUILayout.LabelField($"{pool.CountActive} active · {pool.CountInactive} pooled · {pool.CountAll}/{pool.MaxSize}",
											   EditorStyles.miniLabel, GUILayout.Width(200));
				}

				var rect = GUILayoutUtility.GetRect(0, 6, GUILayout.ExpandWidth(true));
				EditorGUI.DrawRect(rect, BarBackground);

				// Scale against max size so an over-budget pool visibly overshoots the bar instead of
				// silently pinning at 100%.
				var scale = Mathf.Max(pool.MaxSize, pool.CountAll);
				if (scale <= 0) return;

				var activeWidth = rect.width * pool.CountActive / scale;
				var allWidth = rect.width * pool.CountAll / scale;

				EditorGUI.DrawRect(new Rect(rect.x, rect.y, allWidth, rect.height), BarInactive);
				EditorGUI.DrawRect(new Rect(rect.x, rect.y, activeWidth, rect.height),
								   overflowing ? BarOverflow : BarActive);

				if (overflowing)
				{
					EditorGUILayout.HelpBox($"This pool has grown past its max size ({pool.MaxSize}). " +
											"Instances beyond it are destroyed on return instead of reused — " +
											"raise Max Size or reduce concurrent spawns.", MessageType.Warning);
				}
			}
		}
	}
}
