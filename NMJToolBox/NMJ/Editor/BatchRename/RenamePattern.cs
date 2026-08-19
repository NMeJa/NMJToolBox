using System;
using System.Globalization;
using System.Text;
using NMJTools.Extensions;
using UnityEngine;

namespace NMJTools
{
	/// <summary>
	/// Per-item data a <see cref="RenamePattern"/> token can read.
	/// </summary>
	public readonly struct RenameContext
	{
		/// <summary>The name entering the pattern step (after trim and find/replace).</summary>
		public string Name { get; }

		/// <summary>Zero-based position of this item in the ordered selection.</summary>
		public int Index { get; }

		/// <summary>Total items in the batch.</summary>
		public int Total { get; }

		/// <summary>Parent GameObject name, or containing folder name for assets.</summary>
		public string Parent { get; }

		/// <summary>Type name — the asset's type, or the first non-Transform component for GameObjects.</summary>
		public string TypeName { get; }

		/// <summary>Asset file extension without the dot. Empty for GameObjects.</summary>
		public string Extension { get; }

		public RenameContext(string name, int index, int total, string parent, string typeName, string extension)
		{
			Name = name ?? string.Empty;
			Index = index;
			Total = total;
			Parent = parent ?? string.Empty;
			TypeName = typeName ?? string.Empty;
			Extension = extension ?? string.Empty;
		}
	}

	/// <summary>
	/// Expands <c>{TOKEN}</c> / <c>{TOKEN:args}</c> expressions in a batch-rename pattern.
	///
	/// Unknown tokens are left in the output verbatim rather than silently vanishing — a typo you
	/// can see in the preview is far easier to fix than a name that quietly lost a segment.
	/// </summary>
	public static class RenamePattern
	{
		/// <summary>Human-readable token reference, shown in the window's help panel.</summary>
		public const string Reference =
			"{N}            original name\n" +
			"{N:i}  {N:i,c}  substring of the name (i may be negative = from the end)\n" +
			"{S:i,c}        same as {N:i,c}\n" +
			"{C}            counter, starts at 1\n" +
			"{C:s} {C:s,st} {C:s,st,pad}   counter: start, step, zero-padding\n" +
			"{I}            index, starts at 0 (same options as {C})\n" +
			"{A}  {A:s}     letter counter — a, b, … z, aa, ab\n" +
			"{AU} {AU:s}    uppercase letter counter\n" +
			"{T}            total number of items in the batch\n" +
			"{P}            parent object / containing folder\n" +
			"{TY}           type name (Texture2D, Rigidbody, …)\n" +
			"{E}            file extension, assets only\n" +
			"{U} {L}        name upper-cased / lower-cased\n" +
			"{TC}           name Title Cased\n" +
			"{B}            name beautified (my_healthBar → My Health Bar)\n" +
			"{D} {D:format} date, default yyyy-MM-dd\n" +
			"{{  }}          a literal { or }";

		/// <summary>
		/// Expands <paramref name="pattern"/> for one item. Returns the pattern unchanged if it
		/// contains no tokens, so a plain string is a valid "rename everything to this" pattern.
		/// </summary>
		public static string Apply(string pattern, in RenameContext ctx)
		{
			if (string.IsNullOrEmpty(pattern)) return ctx.Name;

			var output = new StringBuilder(pattern.Length + 16);

			for (var i = 0; i < pattern.Length; i++)
			{
				var c = pattern[i];

				if (c == '{')
				{
					// {{ escapes a literal brace.
					if (i + 1 < pattern.Length && pattern[i + 1] == '{')
					{
						output.Append('{');
						i++;
						continue;
					}

					var close = pattern.IndexOf('}', i + 1);
					if (close < 0)
					{
						// Unterminated token: emit the rest literally rather than throwing while
						// the user is still mid-keystroke in the pattern field.
						output.Append(pattern, i, pattern.Length - i);
						break;
					}

					var body = pattern.Substring(i + 1, close - i - 1);
					output.Append(Expand(body, ctx));
					i = close;
					continue;
				}

				if (c == '}' && i + 1 < pattern.Length && pattern[i + 1] == '}')
				{
					output.Append('}');
					i++;
					continue;
				}

				output.Append(c);
			}

			return output.ToString();
		}

		private static string Expand(string body, in RenameContext ctx)
		{
			if (string.IsNullOrEmpty(body)) return "{}";

			var colon = body.IndexOf(':');
			var token = (colon < 0 ? body : body[..colon]).Trim().ToUpperInvariant();
			var argText = colon < 0 ? string.Empty : body[(colon + 1)..];

			switch (token)
			{
				case "N":
				case "S":
				{
					if (string.IsNullOrEmpty(argText)) return ctx.Name;
					var args = ParseInts(argText);
					var index = args.Length > 0 ? args[0] : 0;
					var count = args.Length > 1 ? args[1] : -1;
					return Substring(ctx.Name, index, count);
				}

				case "C": return Counter(argText, ctx.Index, defaultStart: 1);
				case "I": return Counter(argText, ctx.Index, defaultStart: 0);

				case "A":  return Letters(argText, ctx.Index, upper: false);
				case "AU": return Letters(argText, ctx.Index, upper: true);

				case "T":  return ctx.Total.ToString(CultureInfo.InvariantCulture);
				case "P":  return ctx.Parent;
				case "TY": return ctx.TypeName;
				case "E":  return ctx.Extension;

				case "U":  return ctx.Name.ToUpperInvariant();
				case "L":  return ctx.Name.ToLowerInvariant();
				case "TC": return CultureInfo.InvariantCulture.TextInfo.ToTitleCase(ctx.Name.ToLowerInvariant());
				case "B":  return ctx.Name.Beautify();

				case "D":
				{
					var format = string.IsNullOrWhiteSpace(argText) ? "yyyy-MM-dd" : argText;
					try
					{
						return DateTime.Now.ToString(format, CultureInfo.InvariantCulture);
					}
					catch (FormatException)
					{
						return "{D:?}";
					}
				}

				// Deliberately echoed back so the preview shows the typo.
				default: return "{" + body + "}";
			}
		}

		private static string Counter(string argText, int index, int defaultStart)
		{
			var args = ParseInts(argText);
			var start = args.Length > 0 ? args[0] : defaultStart;
			var step = args.Length > 1 ? args[1] : 1;
			var pad = args.Length > 2 ? Mathf.Clamp(args[2], 0, 16) : 0;

			var value = start + index * step;
			var text = Mathf.Abs(value).ToString(CultureInfo.InvariantCulture);
			if (pad > 0) text = text.PadLeft(pad, '0');
			return value < 0 ? "-" + text : text;
		}

		private static string Letters(string argText, int index, bool upper)
		{
			var args = ParseInts(argText);
			var start = args.Length > 0 ? args[0] : 0;
			var value = Mathf.Max(0, start + index);

			// Bijective base-26: 0→a, 25→z, 26→aa. Plain base-26 would produce an empty digit
			// at every multiple of 26, giving a, b, … z, ba instead of z, aa.
			var sb = new StringBuilder(4);
			value++;
			while (value > 0)
			{
				value--;
				sb.Insert(0, (char)('a' + value % 26));
				value /= 26;
			}

			var text = sb.ToString();
			return upper ? text.ToUpperInvariant() : text;
		}

		private static string Substring(string source, int index, int count)
		{
			if (string.IsNullOrEmpty(source)) return string.Empty;

			if (index < 0) index = Mathf.Max(0, source.Length + index);
			if (index >= source.Length) return string.Empty;

			if (count < 0) count = source.Length - index;
			count = Mathf.Min(count, source.Length - index);

			return count <= 0 ? string.Empty : source.Substring(index, count);
		}

		private static int[] ParseInts(string argText)
		{
			if (string.IsNullOrWhiteSpace(argText)) return Array.Empty<int>();

			var pieces = argText.Split(',');
			var result = new int[pieces.Length];
			for (var i = 0; i < pieces.Length; i++)
				int.TryParse(pieces[i].Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out result[i]);

			return result;
		}
	}
}