using System;
using UnityEngine;

namespace NMJTools
{
	/// <summary>
	/// Shows a field in the inspector but blocks editing. Useful for values written by code that you
	/// still want to watch — ids, cached references, runtime state.
	/// </summary>
	/// <remarks>
	/// The value is still serialized; this only disables the control. It is not a security boundary
	/// and does not stop code from writing the field.
	/// </remarks>
	[AttributeUsage(AttributeTargets.Field, Inherited = true)]
	public class ReadOnlyAttribute : PropertyAttribute { }

	/// <summary>
	/// Flags a reference field that must be assigned. Draws an error box under the field while it is
	/// null, turning the most common source of a runtime <c>NullReferenceException</c> into something
	/// visible at authoring time.
	/// </summary>
	[AttributeUsage(AttributeTargets.Field, Inherited = true)]
	public class RequiredAttribute : PropertyAttribute
	{
		/// <summary>Optional explanation shown instead of the default message.</summary>
		public string Message { get; }

		public RequiredAttribute(string message = null)
		{
			Message = message;
		}
	}

	/// <summary>
	/// Draws a button in the inspector that invokes the decorated method.
	///
	/// The method must take no parameters. Unlike <c>[ContextMenu]</c> the button is visible rather
	/// than hidden behind the component's ⋮ menu, and it works on <see cref="ScriptableObject"/>s
	/// and multi-selections too.
	/// </summary>
	[AttributeUsage(AttributeTargets.Method, Inherited = true)]
	public class ButtonAttribute : Attribute
	{
		/// <summary>Button label. Defaults to the method name, nicified.</summary>
		public string Label { get; }

		/// <summary>When the button is clickable.</summary>
		public ButtonMode Mode { get; }

		public ButtonAttribute(string label = null, ButtonMode mode = ButtonMode.Always)
		{
			Label = label;
			Mode = mode;
		}
	}

	public enum ButtonMode
	{
		Always,
		EditModeOnly,
		PlayModeOnly
	}
}
