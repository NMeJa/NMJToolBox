using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Text.RegularExpressions;

namespace NMJTools.Extensions
{
	[SuppressMessage("ReSharper", "MemberCanBePrivate.Global")]
	[SuppressMessage("ReSharper", "UnusedMember.Global")]
	// ReSharper disable once UnusedType.Global
	public static class StringExtensions
	{
#region Trimming

		/// <summary>Removes <paramref name="wordToRemove"/> from the start, if present.</summary>
		/// <remarks>
		/// A string overload of a BCL name that normally takes <c>char[]</c>. The compiler picks this
		/// extension only because no instance overload of TrimStart accepts a string.
		/// </remarks>
		public static string TrimStart(this string str, string wordToRemove)
		{
			if (str.StartsWith(wordToRemove))
			{
				str = str.Remove(0, wordToRemove.Length);
			}

			return str;
		}

		/// <summary>Removes <paramref name="wordToRemove"/> from the end, if present.</summary>
		public static string TrimEnd(this string str, string wordToRemove)
		{
			if (str.EndsWith(wordToRemove))
			{
				str = str.Remove(str.Length - wordToRemove.Length, wordToRemove.Length);
			}

			return str;
		}

#endregion

#region Beautifying

		// Merged in from StringBeautifier. Two static classes of string extension methods in the same
		// namespace were indistinguishable at every call site - extension resolution ignores the
		// declaring type - so the split only ever cost a file to open.

		/// <summary>Title-cases every word (invariant culture).</summary>
		public static string CapitalizeFirstLetter(this string s)
		{
			return string.IsNullOrEmpty(s)
					   ? string.Empty
					   : CultureInfo.InvariantCulture.TextInfo.ToTitleCase(s.ToLowerInvariant());
		}

		/// <summary>Collapses runs of whitespace into a single space.</summary>
		public static string RemoveExcessWhitespace(this string s)
		{
			return string.IsNullOrEmpty(s) ? string.Empty : Regex.Replace(s, @"\s+", " ");
		}

		public static string ReplaceUnderscoresWithSpaces(this string s)
		{
			return string.IsNullOrEmpty(s) ? string.Empty : s.Replace("_", " ");
		}

		/// <summary>Inserts spaces at camelCase and PascalCase boundaries.</summary>
		public static string SplitCamelCase(this string s)
		{
			return string.IsNullOrEmpty(s)
					   ? string.Empty
					   : Regex.Replace(s, "([a-z](?=[A-Z])|[A-Z](?=[A-Z][a-z]))", "$1 ").Trim();
		}

		/// <summary>
		/// Turns an identifier into a human-readable label:
		/// <c>player_healthBar</c> becomes <c>Player Health Bar</c>.
		/// </summary>
		public static string Beautify(this string s)
		{
			return string.IsNullOrEmpty(s)
					   ? string.Empty
					   : s.ReplaceUnderscoresWithSpaces()
						  .SplitCamelCase()
						  .CapitalizeFirstLetter()
						  .RemoveExcessWhitespace();
		}

#endregion
	}
}
