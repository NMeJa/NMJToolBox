using System;
using System.Diagnostics.CodeAnalysis;
using Newtonsoft.Json;

namespace NMJTools.Integrations
{
	/// <summary>
	/// JSON helpers. The only code in NMJ ToolBox that touches Newtonsoft, which is why it lives in
	/// its own <c>defineConstraints</c>-gated assembly rather than in <c>Runtime/Extensions</c>.
	///
	/// That gating is what lets NMJ ToolBox declare **no package dependencies at all**: without
	/// <c>com.unity.nuget.newtonsoft-json</c> installed, <c>NMJ_NEWTONSOFT</c> is never set, this
	/// assembly is skipped, and <see cref="ParseJson{T}"/> is simply absent — nothing else in the
	/// package notices. Install it from Tools ▸ NMJ ▸ Setup ▸ Project Setup to switch it on.
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
				// ReSharper disable once RedundantStringInterpolation
				throw new Exception($"Error parsing json!\n " +
									$"Data: {json}\n" +
									$"Exception: {exception}\n" +
									$"Message: {exception.Message}");
			}
		}
	}
}