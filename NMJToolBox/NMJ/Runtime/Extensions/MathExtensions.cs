using System.Diagnostics.CodeAnalysis;
using UnityEngine;

namespace NMJTools.Extensions
{
	[SuppressMessage("ReSharper", "MemberCanBePrivate.Global")]
	[SuppressMessage("ReSharper", "UnusedMember.Global")]
	// ReSharper disable once UnusedType.Global
	public static class MathExtensions
	{
		/// <summary>
		/// Maps a value from one range to another. Returns <paramref name="outMin"/> when the input
		/// range is degenerate, rather than dividing by zero and producing NaN — a NaN here silently
		/// poisons every transform it touches and is miserable to track down.
		/// </summary>
		public static float Remap(this float value, float inMin, float inMax, float outMin, float outMax)
		{
			var span = inMax - inMin;
			if (Mathf.Abs(span) < Mathf.Epsilon) return outMin;
			return outMin + (value - inMin) * (outMax - outMin) / span;
		}

		/// <summary><see cref="Remap"/> clamped to the output range.</summary>
		public static float RemapClamped(this float value, float inMin, float inMax, float outMin, float outMax)
		{
			var result = value.Remap(inMin, inMax, outMin, outMax);
			return outMin < outMax ? Mathf.Clamp(result, outMin, outMax) : Mathf.Clamp(result, outMax, outMin);
		}

		/// <summary>Normalizes a value in [min, max] to [0, 1].</summary>
		public static float Normalize(this float value, float min, float max) => value.Remap(min, max, 0f, 1f);

		/// <summary>
		/// Equality with an explicit tolerance. <see cref="Mathf.Approximately"/> scales its epsilon
		/// with magnitude, which is right for general use but useless when you need "within 0.01".
		/// </summary>
		public static bool Approximately(this float a, float b, float tolerance) => Mathf.Abs(a - b) <= tolerance;

		/// <summary>Wraps into [0, length), handling negatives — unlike the <c>%</c> operator.</summary>
		public static int Wrap(this int value, int length)
		{
			if (length <= 0) return 0;
			var result = value % length;
			return result < 0 ? result + length : result;
		}

		/// <summary>Wraps into [min, max).</summary>
		public static float Wrap(this float value, float min, float max)
		{
			var span = max - min;
			if (Mathf.Abs(span) < Mathf.Epsilon) return min;
			var result = (value - min) % span;
			return result < 0f ? result + span + min : result + min;
		}

		/// <summary>Shortest signed difference between two angles, in [-180, 180].</summary>
		public static float AngleDifference(this float from, float to) => Mathf.DeltaAngle(from, to);

		/// <summary>Threshold a float into a bool, mirroring <see cref="ParsingExtensions.ToBoolean"/>.</summary>
		public static bool IsBetween(this float value, float min, float max, bool inclusive = true) =>
			inclusive ? value >= min && value <= max : value > min && value < max;

		public static bool IsBetween(this int value, int min, int max, bool inclusive = true) =>
			inclusive ? value >= min && value <= max : value > min && value < max;

		/// <summary>Percentage of the way from <paramref name="min"/> to <paramref name="max"/>, clamped.</summary>
		public static float PercentBetween(this float value, float min, float max) =>
			Mathf.Clamp01(value.Remap(min, max, 0f, 1f));

		/// <summary>Rounds to the nearest multiple of <paramref name="step"/>.</summary>
		public static float SnapTo(this float value, float step) =>
			step <= 0f ? value : Mathf.Round(value / step) * step;

		/// <summary>
		/// Thresholds a float into a bool. Moved here from ParsingExtensions, which had nothing to do
		/// with parsing in this case — it is scalar maths.
		/// </summary>
		/// <param name="useAbsolute">Compare the magnitude, ignoring sign.</param>
		/// <param name="useInverse">Flip the comparison, so the result is true <em>below</em> the threshold.</param>
		public static bool ToBoolean(this float value, bool useAbsolute = false, bool useInverse = false,
									 float differentialValue = 0.5f)
		{
			if (useAbsolute) value = Mathf.Abs(value);
			return useInverse ? value < differentialValue : value > differentialValue;
		}
	}
}