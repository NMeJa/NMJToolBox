using HelperTools.OtherTools;
using UnityEditor;
using UnityEngine;

namespace HelperTools.Editor.OtherTools
{
    /// <summary>
    /// This is script to display public/SerializeField as non-editable in inspector. The script was made by It3ration
    /// And Take From : https://answers.unity.com/questions/489942/how-to-make-a-readonly-property-in-inspector.html
    /// This Helper tool is combination of two.
    /// ReadOnlyDrawer-> in Editor Folder
    /// ReadOnlyAttribute-> in non-Editor folder
    /// </summary>
    [CustomPropertyDrawer(typeof(ReadOnlyAttribute))]
    public class ReadOnlyDrawer : PropertyDrawer
    {
        public override float GetPropertyHeight(SerializedProperty property,
            GUIContent label)
        {
            return EditorGUI.GetPropertyHeight(property, label, true);
        }

        public override void OnGUI(Rect position,
            SerializedProperty property,
            GUIContent label)
        {
            GUI.enabled = false;
            EditorGUI.PropertyField(position, property, label, true);
            GUI.enabled = true;
        }
    }
}