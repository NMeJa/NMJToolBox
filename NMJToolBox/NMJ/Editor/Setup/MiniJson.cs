using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace NMJTools.Setup
{
	/// <summary>
	/// A tiny order-preserving JSON reader/writer, used for one job: editing
	/// <c>Packages/manifest.json</c> in place.
	///
	/// Unity ships no writable manifest API and no scoped-registry API — <see cref="UnityEditor.PackageManager.Client"/>
	/// has Add, Remove and Resolve but nothing that touches <c>scopedRegistries</c> — so adding OpenUPM
	/// means editing the file. <see cref="UnityEngine.JsonUtility"/> cannot do it: it binds to a fixed
	/// class shape and silently drops every key it was not told about, which on a manifest means losing
	/// <c>lock</c>, <c>testables</c> and anything a future Unity adds. Newtonsoft would do the job but is
	/// an optional package here, and the setup window has to work before anything is installed.
	///
	/// Hence this: full JSON in, full JSON out, keys in their original order, unknown keys untouched.
	/// It is deliberately not a general-purpose library — no attribute binding, no LINQ surface, no
	/// number coercion. It reads a manifest and writes it back.
	/// </summary>
	internal abstract class JsonValue
	{
		public static JsonValue Parse(string text) => new JsonParser(text).ParseDocument();

		/// <summary>Serialises with Unity's own manifest style: two-space indent, LF line endings.</summary>
		public string ToJson()
		{
			var builder = new StringBuilder(512);
			Write(builder, 0);
			builder.Append('\n');
			return builder.ToString();
		}

		internal abstract void Write(StringBuilder builder, int depth);

		protected static void Indent(StringBuilder builder, int depth) => builder.Append(' ', depth * 2);
	}

	/// <summary>A JSON object. Backed by a list rather than a dictionary so key order survives a round trip.</summary>
	internal sealed class JsonObject : JsonValue
	{
		private readonly List<KeyValuePair<string, JsonValue>> members = new();

		public IReadOnlyList<KeyValuePair<string, JsonValue>> Members => members;

		public JsonValue this[string key]
		{
			get
			{
				foreach (var member in members)
					if (string.Equals(member.Key, key, StringComparison.Ordinal))
						return member.Value;

				return null;
			}
		}

		public bool TryGet<T>(string key, out T value) where T : JsonValue
		{
			value = this[key] as T;
			return value != null;
		}

		/// <summary>Replaces the value under <paramref name="key"/>, or appends it if the key is new.</summary>
		public void Set(string key, JsonValue value)
		{
			for (var i = 0; i < members.Count; i++)
			{
				if (!string.Equals(members[i].Key, key, StringComparison.Ordinal)) continue;
				members[i] = new KeyValuePair<string, JsonValue>(key, value);
				return;
			}

			members.Add(new KeyValuePair<string, JsonValue>(key, value));
		}

		/// <summary>
		/// Inserts at the top of the object rather than the bottom. Used for <c>scopedRegistries</c>,
		/// which Unity itself writes above <c>dependencies</c> — appending it after a several-hundred-line
		/// dependency block would work but reads as noise in every future diff.
		/// </summary>
		public void SetFirst(string key, JsonValue value)
		{
			for (var i = 0; i < members.Count; i++)
			{
				if (!string.Equals(members[i].Key, key, StringComparison.Ordinal)) continue;
				members[i] = new KeyValuePair<string, JsonValue>(key, value);
				return;
			}

			members.Insert(0, new KeyValuePair<string, JsonValue>(key, value));
		}

		internal override void Write(StringBuilder builder, int depth)
		{
			if (members.Count == 0)
			{
				builder.Append("{}");
				return;
			}

			builder.Append("{\n");

			for (var i = 0; i < members.Count; i++)
			{
				Indent(builder, depth + 1);
				JsonString.WriteLiteral(builder, members[i].Key);
				builder.Append(": ");
				members[i].Value.Write(builder, depth + 1);
				if (i < members.Count - 1) builder.Append(',');
				builder.Append('\n');
			}

			Indent(builder, depth);
			builder.Append('}');
		}
	}

	internal sealed class JsonArray : JsonValue
	{
		private readonly List<JsonValue> items = new();

		public IReadOnlyList<JsonValue> Items => items;
		public int Count => items.Count;

		public void Add(JsonValue value) => items.Add(value);

		internal override void Write(StringBuilder builder, int depth)
		{
			if (items.Count == 0)
			{
				builder.Append("[]");
				return;
			}

			builder.Append("[\n");

			for (var i = 0; i < items.Count; i++)
			{
				Indent(builder, depth + 1);
				items[i].Write(builder, depth + 1);
				if (i < items.Count - 1) builder.Append(',');
				builder.Append('\n');
			}

			Indent(builder, depth);
			builder.Append(']');
		}
	}

	internal sealed class JsonString : JsonValue
	{
		public string Value { get; }

		public JsonString(string value) => Value = value ?? string.Empty;

		internal override void Write(StringBuilder builder, int depth) => WriteLiteral(builder, Value);

		internal static void WriteLiteral(StringBuilder builder, string value)
		{
			builder.Append('"');

			foreach (var c in value)
			{
				switch (c)
				{
					case '"':  builder.Append("\\\""); break;
					case '\\': builder.Append("\\\\"); break;
					case '\b': builder.Append("\\b"); break;
					case '\f': builder.Append("\\f"); break;
					case '\n': builder.Append("\\n"); break;
					case '\r': builder.Append("\\r"); break;
					case '\t': builder.Append("\\t"); break;
					default:
						if (c < ' ') builder.Append("\\u").Append(((int)c).ToString("x4", CultureInfo.InvariantCulture));
						else builder.Append(c);
						break;
				}
			}

			builder.Append('"');
		}
	}

	/// <summary>
	/// Numbers, booleans and null, kept as their original text.
	///
	/// Round-tripping the literal rather than parsing to a double is deliberate: a manifest holds no
	/// numbers this package cares about, and re-formatting one would be the only way to corrupt a value
	/// nothing here even reads.
	/// </summary>
	internal sealed class JsonLiteral : JsonValue
	{
		public string Text { get; }

		public JsonLiteral(string text) => Text = text;

		internal override void Write(StringBuilder builder, int depth) => builder.Append(Text);
	}

	internal sealed class JsonParseException : Exception
	{
		public JsonParseException(string message) : base(message) { }
	}

	internal sealed class JsonParser
	{
		private readonly string text;
		private int position;

		public JsonParser(string text) => this.text = text ?? string.Empty;

		public JsonValue ParseDocument()
		{
			SkipWhitespace();
			var value = ParseValue();
			SkipWhitespace();

			if (position < text.Length)
				throw new JsonParseException($"Unexpected trailing content at offset {position}.");

			return value;
		}

		private JsonValue ParseValue()
		{
			SkipWhitespace();

			if (position >= text.Length) throw new JsonParseException("Unexpected end of input.");

			return text[position] switch
			{
				'{' => ParseObject(),
				'[' => ParseArray(),
				'"' => new JsonString(ParseString()),
				_   => ParseLiteral()
			};
		}

		private JsonObject ParseObject()
		{
			var result = new JsonObject();
			position++; // '{'
			SkipWhitespace();

			if (Peek() == '}')
			{
				position++;
				return result;
			}

			while (true)
			{
				SkipWhitespace();
				if (Peek() != '"') throw new JsonParseException($"Expected a key string at offset {position}.");

				var key = ParseString();

				SkipWhitespace();
				if (Peek() != ':') throw new JsonParseException($"Expected ':' at offset {position}.");
				position++;

				result.Set(key, ParseValue());

				SkipWhitespace();
				var next = Peek();

				if (next == ',')
				{
					position++;
					continue;
				}

				if (next == '}')
				{
					position++;
					return result;
				}

				throw new JsonParseException($"Expected ',' or '}}' at offset {position}.");
			}
		}

		private JsonArray ParseArray()
		{
			var result = new JsonArray();
			position++; // '['
			SkipWhitespace();

			if (Peek() == ']')
			{
				position++;
				return result;
			}

			while (true)
			{
				result.Add(ParseValue());

				SkipWhitespace();
				var next = Peek();

				if (next == ',')
				{
					position++;
					continue;
				}

				if (next == ']')
				{
					position++;
					return result;
				}

				throw new JsonParseException($"Expected ',' or ']' at offset {position}.");
			}
		}

		private string ParseString()
		{
			position++; // opening quote
			var builder = new StringBuilder(32);

			while (position < text.Length)
			{
				var c = text[position++];

				if (c == '"') return builder.ToString();

				if (c != '\\')
				{
					builder.Append(c);
					continue;
				}

				if (position >= text.Length) break;

				var escape = text[position++];
				switch (escape)
				{
					case '"':  builder.Append('"'); break;
					case '\\': builder.Append('\\'); break;
					case '/':  builder.Append('/'); break;
					case 'b':  builder.Append('\b'); break;
					case 'f':  builder.Append('\f'); break;
					case 'n':  builder.Append('\n'); break;
					case 'r':  builder.Append('\r'); break;
					case 't':  builder.Append('\t'); break;
					case 'u':
						if (position + 4 > text.Length) throw new JsonParseException("Truncated \\u escape.");
						builder.Append((char)int.Parse(text.Substring(position, 4), NumberStyles.HexNumber, CultureInfo.InvariantCulture));
						position += 4;
						break;
					default:
						throw new JsonParseException($"Unknown escape '\\{escape}' at offset {position - 1}.");
				}
			}

			throw new JsonParseException("Unterminated string.");
		}

		private JsonLiteral ParseLiteral()
		{
			var start = position;

			while (position < text.Length)
			{
				var c = text[position];
				if (c is ',' or '}' or ']' || char.IsWhiteSpace(c)) break;
				position++;
			}

			if (position == start) throw new JsonParseException($"Expected a value at offset {position}.");

			return new JsonLiteral(text[start..position]);
		}

		private char Peek() => position < text.Length ? text[position] : '\0';

		private void SkipWhitespace()
		{
			while (position < text.Length && char.IsWhiteSpace(text[position])) position++;
		}
	}
}
