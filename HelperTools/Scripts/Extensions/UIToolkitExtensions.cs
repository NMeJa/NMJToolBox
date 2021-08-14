using UnityEngine;
using UnityEngine.UIElements;

namespace FOF.Extensions.Editor
{
    public static class UIToolkitExtensions
    {
        public static void DisplayVisibility(this IStyle style, DisplayStyle displayStyle)
        {
            var styleDisplay = style.display;
            styleDisplay.value = displayStyle;
            style.display = styleDisplay;
        }

        public static void ChangeBackgroundColor(this IStyle style, Color color)
        {
            var styleBackgroundColor = style.backgroundColor;
            styleBackgroundColor.value = color;
            style.backgroundColor = styleBackgroundColor;
        }
    }
}