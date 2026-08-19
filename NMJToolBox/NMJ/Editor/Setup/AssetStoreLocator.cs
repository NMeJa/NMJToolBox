using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using UnityEngine;

namespace NMJTools.Setup
{
	/// <summary>
	/// Finds <c>.unitypackage</c> files Unity has already downloaded from the Asset Store.
	///
	/// This is the honest ceiling for automating paid packages. Unity exposes no API to purchase or
	/// download store content — a license check happens server-side against the signed-in account —
	/// so nothing can make a package the user does not own appear. What <em>is</em> possible: once
	/// they have downloaded it (in this or any other project on the machine), Unity keeps the
	/// .unitypackage in a shared per-user cache, and <see cref="UnityEditor.AssetDatabase.ImportPackage"/>
	/// can import it from there without another trip through the Package Manager UI.
	/// </summary>
	[SuppressMessage("ReSharper", "MemberCanBePrivate.Global")]
	public static class AssetStoreLocator
	{
		/// <summary>
		/// Per-user Asset Store download cache. The "5.x" suffix is historical — Unity has used the
		/// same folder since Unity 5 and still does.
		/// </summary>
		public static string CacheRoot =>
			Application.platform switch
			{
				RuntimePlatform.WindowsEditor => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Unity", "Asset Store-5.x"),
				RuntimePlatform.OSXEditor     => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Personal), "Library", "Unity", "Asset Store-5.x"),
				_                             => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Personal), ".local", "share", "unity3d", "Asset Store-5.x")
			};

		public static bool CacheExists => Directory.Exists(CacheRoot);

		private static List<string> cached;

		/// <summary>Every cached .unitypackage path. Enumerated once, then reused.</summary>
		public static IReadOnlyList<string> AllPackages
		{
			get
			{
				if (cached != null) return cached;

				cached = new List<string>();
				if (!CacheExists) return cached;

				try
				{
					cached.AddRange(Directory.EnumerateFiles(CacheRoot, "*.unitypackage", SearchOption.AllDirectories));
				}
				catch (Exception e)
				{
					Debug.LogWarning($"[NMJ] Could not read the Asset Store cache at '{CacheRoot}': {e.Message}");
				}

				return cached;
			}
		}

		public static void Refresh() => cached = null;

		/// <summary>
		/// Best cached download for an entry, or null. Matches on <see cref="PackageEntry.cacheHints"/>
		/// then the display name, and prefers the most recently written file — store downloads keep
		/// older versions around, and the newest is almost always the wanted one.
		/// </summary>
		public static string FindCachedPackage(PackageEntry entry)
		{
			if (entry == null) return null;

			var hints = entry.cacheHints.Length > 0
							? entry.cacheHints
							: new[] { entry.displayName };

			var matches = AllPackages
						  .Where(path => hints.Any(hint =>
													   !string.IsNullOrEmpty(hint) &&
													   path.IndexOf(hint, StringComparison.OrdinalIgnoreCase) >= 0))
						  .ToList();

			return matches.Count == 0 ? null : matches.OrderByDescending(File.GetLastWriteTimeUtc).First();
		}
	}
}