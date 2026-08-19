using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace NMJTools.SceneTools
{
	/// <summary>
	/// Records what you have selected so you can step back and forward through it — the browser
	/// history Unity has never had.
	///
	/// Static and <c>[InitializeOnLoad]</c> rather than owned by the overlay, because history is only
	/// useful if it survives the overlay being closed, the Scene view being re-docked, and a domain
	/// reload mid-session.
	/// </summary>
	[InitializeOnLoad]
	public static class SelectionHistory
	{
		private const int MaxEntries = 50;

		private static readonly List<Object[]> entries = new();
		private static int cursor = -1;

		/// <summary>
		/// Set while stepping through history. Without it, applying a historical selection would
		/// itself be recorded as a new entry, and the history could never move backwards.
		/// </summary>
		private static bool navigating;

		public static event Action Changed;

		static SelectionHistory()
		{
			Selection.selectionChanged -= Record;
			Selection.selectionChanged += Record;
		}

		public static IReadOnlyList<Object[]> Entries => entries;
		public static int Cursor => cursor;
		public static bool CanGoBack => cursor > 0;
		public static bool CanGoForward => cursor >= 0 && cursor < entries.Count - 1;

		private static void Record()
		{
			if (navigating) return;

			var current = Selection.objects;
			if (current == null || current.Length == 0) return;

			// Ignore a re-selection of what is already at the cursor, otherwise clicking the same
			// object twice fills the history with identical entries.
			if (cursor >= 0 && cursor < entries.Count && SameSelection(entries[cursor], current)) return;

			// Selecting something new after stepping back discards the forward branch, exactly like
			// navigating away from a browser's back-stack.
			if (cursor < entries.Count - 1 && cursor >= 0)
				entries.RemoveRange(cursor + 1, entries.Count - cursor - 1);

			entries.Add((Object[])current.Clone());

			if (entries.Count > MaxEntries) entries.RemoveAt(0);

			cursor = entries.Count - 1;
			Changed?.Invoke();
		}

		private static bool SameSelection(IReadOnlyList<Object> a, IReadOnlyList<Object> b)
		{
			if (a == null || b == null || a.Count != b.Count) return false;
			return !a.Where((t, i) => t != b[i]).Any();
		}

		public static void Back()
		{
			if (!CanGoBack) return;
			Apply(cursor - 1);
		}

		public static void Forward()
		{
			if (!CanGoForward) return;
			Apply(cursor + 1);
		}

		public static void GoTo(int index) => Apply(index);

		private static void Apply(int index)
		{
			Prune();
			if (index < 0 || index >= entries.Count) return;

			// Objects can be destroyed between being recorded and being revisited; a null in the
			// array would clear the whole selection instead of restoring the survivors.
			var alive = entries[index].Where(o => o != null).ToArray();
			if (alive.Length == 0)
			{
				entries.RemoveAt(index);
				cursor = Mathf.Clamp(cursor, -1, entries.Count - 1);
				Changed?.Invoke();
				return;
			}

			navigating = true;
			try
			{
				Selection.objects = alive;
				cursor = index;
			}
			finally
			{
				navigating = false;
			}

			Changed?.Invoke();
		}

		/// <summary>Drops entries whose objects have all been destroyed.</summary>
		private static void Prune()
		{
			for (var i = entries.Count - 1; i >= 0; i--)
			{
				if (entries[i].Any(o => o)) continue;
				entries.RemoveAt(i);
				if (cursor >= i) cursor--;
			}
		}

		public static void Clear()
		{
			entries.Clear();
			cursor = -1;
			Changed?.Invoke();
		}

		/// <summary>Short human label for an entry, used by the overlay list.</summary>
		public static string Describe(int index)
		{
			if (index < 0 || index >= entries.Count) return string.Empty;

			var alive = entries[index].Where(o => o).ToArray();
			if (alive.Length == 0) return "<destroyed>";
			return alive.Length == 1 ? alive[0].name : $"{alive[0].name} +{alive.Length - 1}";
		}
	}
}
