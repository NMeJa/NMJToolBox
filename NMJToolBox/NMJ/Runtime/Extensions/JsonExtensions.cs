using System;
using System.Diagnostics.CodeAnalysis;
using Newtonsoft.Json;

namespace NMJTools.Extensions
{
	/// <summary>
	/// JSON helpers, kept in their own file deliberately.
	///
	/// This is the only code in NMJ ToolBox that touches Newtonsoft, and therefore the only reason
	/// <c>com.unity.nuget.newtonsoft-json</c> is a package dependency at all. Isolated like this it
	/// can be lifted into a <c>defineConstraints</c>-gated assembly — the same pattern the UniTask
	/// and LitMotion integrations already use — which would leave the package with no hard
	/// dependencies whatsoever. Folded into StringExtensions, that move would mean pulling the
	/// string helpers apart again.
	/// </summary>
	[SuppressMessage("ReSharper", "MemberCanBePrivate.Global")]
	[SuppressMessage("ReSharper", "UnusedMember.Global")]
	// ReSharper disable once UnusedType.Global
	public static class JsonExtensions
	{
		/// <summary>
		/// Deserializes JSON into <typeparamref name="T"/>, rethrowing with the offending payload in
		/// the message — Newtonsoft's own exception says what went wrong but never what it was reading.
		/// </summary>
		public static T ParseJson<T>(this string json)
		{
			try
			{
				return JsonConvert.DeserializeObject<T>(json);
			}
			catch (Exception exception)
			{
				throw new Exception($"Error parsing json!\n " +
									$"Data: {json}\n" +
									$"Exception: {exception}\n" +
									$"Message: {exception.Message}");
			}
		}
	}
}
