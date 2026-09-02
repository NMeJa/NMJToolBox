using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace NMJTools.Extensions
{
	// ReSharper disable once UnusedType.Global
	public static class AssetDatabaseExtensions
	{
		/// <summary>
		/// Quickly Create Scriptable Object at a specified path without worrying about folder and subfolders existence
		/// </summary>
		/// <param name="instance"></param>
		/// <param name="folderPath">Folder Path should start with 'Assets/'. NOTE. Can use AssetDatabaseExtensions.AssetPath constant</param>
		/// <param name="objectName">What to name the asset. NOTE! It doesn't need File Format</param>
		// ReSharper disable once UnusedMember.Global
		public static void CreateAsset(this ScriptableObject instance, string folderPath, string objectName)
		{
			// this changed: was folderPath[..AssetPath.Length], which throws ArgumentOutOfRangeException
			// on any path shorter than "Assets/" - so passing "" or "Foo" gave an index error instead
			// of the intended message. StartsWith is also the correct comparison for a prefix test.
			if (string.IsNullOrWhiteSpace(folderPath) ||
				!folderPath.StartsWith(AssetDatabaseUtilities.AssetPath, StringComparison.Ordinal))
			{
				throw new ArgumentException(
											$"Path must start with '{AssetDatabaseUtilities.AssetPath}'. Received: '{folderPath}'.",
											nameof(folderPath));
			}

			var directoryPath = folderPath.TrimEnd('/', '\\', ' ');
			Directory.CreateDirectory(directoryPath);
			AssetDatabase.Refresh();
			if (!AssetDatabase.IsValidFolder(directoryPath)) throw new DirectoryNotFoundException("The Path was Invalid Please Check and Try again");
			var uniquePath = AssetDatabase.GenerateUniqueAssetPath($"{directoryPath}/{objectName}.asset");
			AssetDatabase.CreateAsset(instance, uniquePath);
			AssetDatabase.SaveAssets();
			AssetDatabase.Refresh();
		}
	}
}