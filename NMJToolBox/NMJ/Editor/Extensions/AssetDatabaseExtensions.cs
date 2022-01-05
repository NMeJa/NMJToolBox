using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace NMJToolBox
{
    public static class AssetDatabaseExtensions
    {
        /// <summary>
        /// Quickly Create Scriptable Object at specified path without worrying about folder and sub-folders existence
        /// </summary>
        /// <param name="instance"></param>
        /// <param name="folderPath">Folder Path should start with 'Assets/'. NOTE. can use AssetDatabaseExtensions.AssetPath constant</param>
        /// <param name="objectName">What to name the asset. NOTE! it doesn't need File Format</param>
        public static void CreateAsset(this ScriptableObject instance, string folderPath, string objectName)
        {
            if (folderPath.Substring(0, AssetDatabaseUtilities.AssetPath.Length) != AssetDatabaseUtilities.AssetPath)
            {
                throw new Exception($"Path doesn't start with '{AssetDatabaseUtilities.AssetPath}' string." +
                                    "\nPlease Correct the problem!");
            }

            string directoryPath = folderPath.TrimEnd('/', '\\', ' ');
            Directory.CreateDirectory(directoryPath);
            AssetDatabase.Refresh();
            if (!AssetDatabase.IsValidFolder(directoryPath))
                throw new DirectoryNotFoundException("The Path was Invalid Please Check and Try again");
            string uniquePath = AssetDatabase.GenerateUniqueAssetPath($"{directoryPath}/{objectName}.asset");
            AssetDatabase.CreateAsset(instance, uniquePath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }
    }
}