using System.Diagnostics.CodeAnalysis;
using UnityEngine;
using UnityEngine.UIElements;

namespace NMJTools.Extensions
{
	[SuppressMessage("ReSharper", "MemberCanBePrivate.Global")]
	[SuppressMessage("ReSharper", "UnusedMember.Global")]
	// ReSharper disable once UnusedType.Global
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