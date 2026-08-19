using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace NMJToolBox
{
	/// <summary>
	/// Draws a <see cref="SerializableDictionary{TKey,TValue}"/> as paired key/value rows instead of
	/// the two disconnected lists Unity would otherwise show.
	///
	/// Works on the serialized lists rather than the live dictionary on purpose: mid-edit the lists
	/// legitimately contain duplicate and empty keys that a dictionary cannot hold, and those rows
	/// still have to be visible and fixable.
	/// </summary>
	[CustomPropertyDrawer(typeof(SerializableDictionary<,>), true)]
	public class SerializableDictionaryDrawer : PropertyDrawer
	{
		private const float Spacing = 2f;
		private const float ButtonWidth = 22f;
		private const float KeyRatio = 0.42f;

		private static readonly Color DuplicateTint = new(1f, 0.4f, 0.4f, 0.25f);

		public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
		{
			var line = EditorGUIUtility.singleLineHeight + Spacing;
			if (!property.isExpanded) return line;

			var keys = property.FindPropertyRelative("keys");
			if (keys == null) return line;

			var height = line * 2; // header + column titles

			for (var i = 0; i < keys.arraySize; i++)
				height += RowHeight(property, i) + Spacing;

			return height + line; // footer with add/clear
		}

		private static float RowHeight(SerializedProperty property, int index)
		{
			var keys = property.FindPropertyRelative("keys");
			var values = property.FindPropertyRelative("values");

			var keyHeight = index < keys.arraySize
								? EditorGUI.GetPropertyHeight(keys.GetArrayElementAtIndex(index), true)
								: EditorGUIUtility.singleLineHeight;

			var valueHeight = index < values.arraySize
								  ? EditorGUI.GetPropertyHeight(values.GetArrayElementAtIndex(index), true)
								  : EditorGUIUtility.singleLineHeight;

			return Mathf.Max(keyHeight, valueHeight);
		}

		public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
		{
			var keys = property.FindPropertyRelative("keys");
			var values = property.FindPropertyRelative("values");

			if (keys == null || values == null)
			{
				EditorGUI.LabelField(position, label.text, "Not a SerializableDictionary");
				return;
			}

			var line = EditorGUIUtility.singleLineHeight;
			var row = new Rect(position.x, position.y, position.width, line);

			property.isExpanded = EditorGUI.Foldout(row, property.isExpanded,
													$"{label.text}  ({keys.arraySize})", true);
			if (!property.isExpanded) return;

			var duplicates = FindDuplicates(keys);

			using (new EditorGUI.IndentLevelScope())
			{
				row.y += line + Spacing;

				var keyWidth = (row.width - ButtonWidth - Spacing * 2) * KeyRatio;
				var valueWidth = row.width - ButtonWidth - Spacing * 2 - keyWidth;

				var indented = EditorGUI.IndentedRect(row);
				EditorGUI.LabelField(new Rect(indented.x, indented.y, keyWidth, line), "Key", EditorStyles.miniBoldLabel);
				EditorGUI.LabelField(new Rect(indented.x + keyWidth + Spacing, indented.y, valueWidth, line), "Value", EditorStyles.miniBoldLabel);

				row.y += line + Spacing;

				for (var i = 0; i < keys.arraySize; i++)
				{
					var height = RowHeight(property, i);
					var rowRect = EditorGUI.IndentedRect(new Rect(row.x, row.y, row.width, height));

					if (duplicates.Contains(i))
						EditorGUI.DrawRect(new Rect(rowRect.x - 2, rowRect.y - 1, rowRect.width + 4, height + 2), DuplicateTint);

					var keyRect = new Rect(rowRect.x, rowRect.y, keyWidth, height);
					var valueRect = new Rect(rowRect.x + keyWidth + Spacing, rowRect.y, valueWidth, height);
					var buttonRect = new Rect(rowRect.xMax - ButtonWidth, rowRect.y, ButtonWidth, line);

					// Indent is already baked into the rects; leaving it on would double-indent.
					var previousIndent = EditorGUI.indentLevel;
					EditorGUI.indentLevel = 0;

					EditorGUI.PropertyField(keyRect, keys.GetArrayElementAtIndex(i), GUIContent.none, true);

					if (i < values.arraySize)
						EditorGUI.PropertyField(valueRect, values.GetArrayElementAtIndex(i), GUIContent.none, true);

					if (GUI.Button(buttonRect, new GUIContent("−", "Remove this entry"), EditorStyles.miniButton))
					{
						keys.DeleteArrayElementAtIndex(i);
						if (i < values.arraySize) values.DeleteArrayElementAtIndex(i);
						EditorGUI.indentLevel = previousIndent;
						return;
					}

					EditorGUI.indentLevel = previousIndent;
					row.y += height + Spacing;
				}

				var footer = EditorGUI.IndentedRect(new Rect(row.x, row.y, row.width, line));
				var addRect = new Rect(footer.x, footer.y, 60f, line);
				var clearRect = new Rect(footer.x + 64f, footer.y, 60f, line);

				var indentCache = EditorGUI.indentLevel;
				EditorGUI.indentLevel = 0;

				if (GUI.Button(addRect, "Add", EditorStyles.miniButton))
				{
					keys.arraySize++;
					values.arraySize = keys.arraySize;
				}

				if (GUI.Button(clearRect, "Clear", EditorStyles.miniButton))
				{
					keys.ClearArray();
					values.ClearArray();
				}

				if (duplicates.Count > 0)
				{
					var warnRect = new Rect(footer.x + 130f, footer.y, footer.width - 130f, line);
					EditorGUI.LabelField(warnRect,
										 $"{duplicates.Count} duplicate key(s) — only the first is kept",
										 EditorStyles.miniLabel);
				}

				EditorGUI.indentLevel = indentCache;
			}
		}

		/// <summary>
		/// Indices whose key already appeared earlier. Compares the boxed value of whatever the key
		/// property happens to be, so it works for strings, enums, ints and object references alike
		/// without knowing TKey.
		/// </summary>
		private static HashSet<int> FindDuplicates(SerializedProperty keys)
		{
			var seen = new HashSet<string>();
			var duplicates = new HashSet<int>();

			for (var i = 0; i < keys.arraySize; i++)
			{
				var element = keys.GetArrayElementAtIndex(i);
				var identity = KeyIdentity(element);

				if (!seen.Add(identity)) duplicates.Add(i);
			}

			return duplicates;
		}

		private static string KeyIdentity(SerializedProperty property)
		{
			return property.propertyType switch
			{
				SerializedPropertyType.String => "s:" + property.stringValue,
				SerializedPropertyType.Integer => "i:" + property.longValue,
				SerializedPropertyType.Enum => "e:" + property.enumValueIndex,
				SerializedPropertyType.Float => "f:" + property.doubleValue,
				SerializedPropertyType.Boolean => "b:" + property.boolValue,
				SerializedPropertyType.ObjectReference => "o:" + (property.objectReferenceValue
																	 ? property.objectReferenceInstanceIDValue.ToString()
																	 : "null"),
				_ => "?:" + property.propertyPath
			};
		}
	}
}
