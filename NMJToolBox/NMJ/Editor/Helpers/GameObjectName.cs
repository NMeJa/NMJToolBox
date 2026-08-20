using NMJTools.Extensions;
using UnityEditor;
using UnityEngine;

namespace NMJTools.Helpers
{
	/// <summary>
	/// Renames a GameObject after the component you invoke this on, using the beautified type name
	/// (<c>PlayerHealthBar</c> -> <c>"Player Health Bar"</c>).
	///
	/// Lives on the component's context menu (the ⋮ / hamburger button in its Inspector header).
	/// Unity exposes no public API for adding a button to an arbitrary component's header row —
	/// that is why the previous version of this script depended on Ultimate Editor Enhancer, which
	/// patches the Inspector to provide one. The CONTEXT menu is the supported equivalent and needs
	/// no third-party package, so this file no longer sits behind an <c>#if UEE</c> guard and the
	/// UEE assembly references have been removed from NMJ.NMJTools.Editor.asmdef.
	/// </summary>
	public static class GameObjectName
	{
		private const string ContextMenuPath = "CONTEXT/Component/Rename GameObject To This Component";

		[MenuItem(ContextMenuPath, false, 1000)]
		private static void RenameFromContextMenu(MenuCommand command)
		{
			if (command.context is not Component component) return;
			Rename(component);
		}

		/// <summary>
		/// Hidden on Transform/RectTransform — every GameObject has one, so "Transform" is never a
		/// useful name — and on components whose beautified name already matches the GameObject.
		/// </summary>
		[MenuItem(ContextMenuPath, true)]
		private static bool ValidateRenameFromContextMenu(MenuCommand command)
		{
			if (command.context is not Component component) return false;
			if (component is Transform) return false;
			return component.gameObject.name != GetDisplayName(component);
		}

		/// <summary>
		/// Renames <paramref name="component"/>'s GameObject to the component's beautified type
		/// name. Registers an Undo step and flags the scene dirty, so the change survives a
		/// domain reload and can be reverted with Ctrl+Z.
		/// </summary>
		// ReSharper disable once MemberCanBePrivate.Global
		public static void Rename(Component component)
		{
			if (!component) return;

			var gameObject = component.gameObject;
			var newName = GetDisplayName(component);
			if (gameObject.name == newName) return;

			Undo.RecordObject(gameObject, $"Rename to {newName}");
			gameObject.name = newName;
			EditorUtility.SetDirty(gameObject);
		}

		private static string GetDisplayName(Component component) => component.GetType().Name.Beautify();
	}
}