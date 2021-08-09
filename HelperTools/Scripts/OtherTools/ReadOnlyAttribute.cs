using UnityEngine;

namespace HelperTools.OtherTools
{
    /// <summary>
    /// This is script to display public/SerializeField as non-editable in inspector. The script was made by It3ration
    /// And Take From : https://answers.unity.com/questions/489942/how-to-make-a-readonly-property-in-inspector.html
    /// This Helper tool is combination of two.
    /// ReadOnlyDrawer-> in Editor Folder
    /// ReadOnlyAttribute-> in non-Editor folder
    /// </summary>
    public class ReadOnlyAttribute : PropertyAttribute
    {
    }
}