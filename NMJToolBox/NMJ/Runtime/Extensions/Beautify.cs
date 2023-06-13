using System.Text.RegularExpressions;
using System.Globalization;

namespace NMJToolBox
{
	public static class StringBeautifier
	{
		public static string CapitalizeFirstLetter(this string s)
		{
			return string.IsNullOrEmpty(s) ? string.Empty : new CultureInfo("en-US", false).TextInfo.ToTitleCase(s.ToLower());
		}

		public static string RemoveExcessWhitespace(this string s)
		{
			return string.IsNullOrEmpty(s) ? string.Empty : Regex.Replace(s, @"\s+", " ");
		}

		public static string ReplaceUnderscoresWithSpaces(this string s)
		{
			return string.IsNullOrEmpty(s) ? string.Empty : s.Replace("_", " ");
		}

		public static string SplitCamelCase(this string s)
		{
			return string.IsNullOrEmpty(s) ? string.Empty : Regex.Replace(s, "([a-z](?=[A-Z])|[A-Z](?=[A-Z][a-z]))", "$1 ").Trim();
		}

		public static string Beautify(this string s)
		{
			return string.IsNullOrEmpty(s) ? string.Empty : s.ReplaceUnderscoresWithSpaces().SplitCamelCase().CapitalizeFirstLetter().RemoveExcessWhitespace();
		}
	}
}
