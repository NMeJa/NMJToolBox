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
			if (folderPath[..AssetDatabaseUtilities.AssetPath.Length] != AssetDatabaseUtilities.AssetPath)
			{
				throw new Exception($"Path doesn't start with '{AssetDatabaseUtilities.AssetPath}' string." +
									"\nPlease Correct the problem!");
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