using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace NMJToolBox
{
	[CustomPropertyDrawer(typeof(ReadOnlyAttribute))]
	public class ReadOnlyDrawer : PropertyDrawer
	{
		public override float GetPropertyHeight(SerializedProperty property, GUIContent label) =>
			EditorGUI.GetPropertyHeight(property, label, true);

		public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
		{
			using (new EditorGUI.DisabledScope(true))
				EditorGUI.PropertyField(position, property, label, true);
		}
	}

	[CustomPropertyDrawer(typeof(RequiredAttribute))]
	public class RequiredDrawer : PropertyDrawer
	{
		private const float BoxHeight = 26f;

		public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
		{
			var height = EditorGUI.GetPropertyHeight(property, label, true);
			if (IsMissing(property)) height += BoxHeight + 2f;
			return height;
		}

		public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
		{
			var fieldHeight = EditorGUI.GetPropertyHeight(property, label, true);
			var fieldRect = new Rect(position.x, position.y, position.width, fieldHeight);

			EditorGUI.PropertyField(fieldRect, property, label, true);

			if (!IsMissing(property)) return;

			var attr = (RequiredAttribute)attribute;
			var message = string.IsNullOrEmpty(attr.Message)
							  ? $"'{property.displayName}' must be assigned."
							  : attr.Message;

			EditorGUI.HelpBox(new Rect(position.x, fieldRect.yMax + 2f, position.width, BoxHeight),
							  message, MessageType.Error);
		}

		/// <summary>
		/// "Missing" means an unassigned reference or a blank string. Value types are skipped —
		/// there is no such thing as an unassigned int, so flagging one would only ever be noise.
		/// </summary>
		private static bool IsMissing(SerializedProperty property) => property.propertyType switch
		{
			SerializedPropertyType.ObjectReference => property.objectReferenceValue == null,
			SerializedPropertyType.String => string.IsNullOrWhiteSpace(property.stringValue),
			SerializedPropertyType.ExposedReference => property.exposedReferenceValue == null,
			SerializedPropertyType.ManagedReference => property.managedReferenceValue == null,
			_ => false
		};
	}

	/// <summary>
	/// Renders <see cref="ButtonAttribute"/> methods under the default inspector.
	///
	/// Registered against <see cref="MonoBehaviour"/> and <see cref="ScriptableObject"/> with
	/// <c>editorForChildClasses</c>, so it becomes the fallback inspector for types that do not
	/// declare their own. Unity always prefers the most specific <c>[CustomEditor]</c>, so any
	/// existing custom inspector still wins — this only fills the gap where the default inspector
	/// would have been drawn.
	///
	/// This wholesale claim on the default inspector is exactly why the whole attribute set sits
	/// behind the <c>NMJ_ATTRIBUTES</c> define: Odin does the same thing, and two packages cannot
	/// both own it.
	/// </summary>
	[CustomEditor(typeof(MonoBehaviour), true), CanEditMultipleObjects]
	public class MonoBehaviourButtonEditor : ButtonEditorBase { }

	[CustomEditor(typeof(ScriptableObject), true), CanEditMultipleObjects]
	public class ScriptableObjectButtonEditor : ButtonEditorBase { }

	public abstract class ButtonEditorBase : Editor
	{
		private static readonly Dictionary<Type, List<(MethodInfo method, ButtonAttribute attribute)>> Cache = new();

		public override void OnInspectorGUI()
		{
			DrawDefaultInspector();

			var buttons = GetButtons(target.GetType());
			if (buttons.Count == 0) return;

			EditorGUILayout.Space(4);

			foreach (var (method, attribute) in buttons)
			{
				var enabled = attribute.Mode switch
				{
					ButtonMode.EditModeOnly => !Application.isPlaying,
					ButtonMode.PlayModeOnly => Application.isPlaying,
					_ => true
				};

				var label = string.IsNullOrEmpty(attribute.Label)
								? ObjectNames.NicifyVariableName(method.Name)
								: attribute.Label;

				using (new EditorGUI.DisabledScope(!enabled))
				{
					if (!GUILayout.Button(label)) continue;

					// Invoke on every selected target so the button works on a multi-selection.
					foreach (var selected in targets)
					{
						Undo.RecordObject(selected, label);
						try
						{
							method.Invoke(selected, null);
						}
						catch (TargetInvocationException e)
						{
							Debug.LogError($"[NMJ] Button '{label}' threw: {e.InnerException?.Message}", selected);
						}

						EditorUtility.SetDirty(selected);
					}
				}
			}
		}

		/// <summary>
		/// Reflection results are cached per type — without this the inspector would re-scan every
		/// method on every repaint, which is many times a second while the mouse moves.
		/// </summary>
		private static List<(MethodInfo, ButtonAttribute)> GetButtons(Type type)
		{
			if (Cache.TryGetValue(type, out var cached)) return cached;

			const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public |
									   BindingFlags.NonPublic | BindingFlags.FlattenHierarchy;

			var buttons = new List<(MethodInfo, ButtonAttribute)>();

			foreach (var method in type.GetMethods(flags))
			{
				var attribute = method.GetCustomAttribute<ButtonAttribute>();
				if (attribute == null) continue;

				if (method.GetParameters().Length > 0)
				{
					Debug.LogWarning($"[NMJ] [Button] on '{type.Name}.{method.Name}' was ignored: " +
									 "only parameterless methods can be shown as buttons.");
					continue;
				}

				buttons.Add((method, attribute));
			}

			Cache[type] = buttons;
			return buttons;
		}
	}
}
