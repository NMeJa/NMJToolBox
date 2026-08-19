using System;
using System.Diagnostics.CodeAnalysis;
using Newtonsoft.Json;

namespace NMJTools.Extensions
{
	[SuppressMessage("ReSharper", "MemberCanBePrivate.Global")]
	[SuppressMessage("ReSharper", "UnusedMember.Global")]
	// ReSharper disable once UnusedType.Global
	public static class ParsingExtensions
	{
		public static bool ToBoolean(this float value, bool useAbsolute = false, bool useInverse = false,
									 float differentialValue = 0.5f)
		{
			if (useAbsolute) value = Math.Abs(value);
			return useInverse ? value < differentialValue : value > differentialValue;
		}

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