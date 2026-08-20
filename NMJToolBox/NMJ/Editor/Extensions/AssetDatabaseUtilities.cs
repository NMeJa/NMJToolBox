using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace NMJTools.Extensions
{
	[SuppressMessage("ReSharper", "UnusedMember.Global")]
	public static class AssetDatabaseUtilities
	{
		public const string AssetPath = "Assets/";

		/// <summary>
		/// 
		/// </summary>
		/// <typeparam name="T">For every type derived from ScriptableObject </typeparam>
		/// <returns></returns>
		public static IEnumerable<T> GetAllInstances<T>() where T : ScriptableObject
		{
			//FindAssets uses tags check documentation for more info
			var guids = AssetDatabase.FindAssets("t:" + typeof(T).Name);
			var a = new T[guids.Length];
			for (var i = 0; i < guids.Length; i++) //probably could get optimized 
			{
				var path = AssetDatabase.GUIDToAssetPath(guids[i]);
				a[i] = AssetDatabase.LoadAssetAtPath<T>(path);
			}

			return a;
		}

		/// <summary>
		/// Load Stylesheets from the path
		/// </summary>
		/// <param name="path">It doesn't need 'Assets/' or '.uss',
		/// but it would still compile correctly with these included</param>
		/// <returns></returns>
		public static StyleSheet LoadUss(string path)
		{
			if (string.IsNullOrEmpty(path)) throw new NullReferenceException("Path for StyleSheet/USS is null or empty");
			const string extension = ".uss";
			var correctedPath = path.TrimStart(AssetPath).TrimEnd(extension);
			var styleSheet = AssetDatabase.LoadAssetAtPath<StyleSheet>(AssetPath + correctedPath + extension);
			return styleSheet ?? throw new NullReferenceException("StyleSheet/USS is null! Path was incorrect");
		}

		public static StyleSheet LoadUssWithName(string name)
		{
			//FindAssets uses tags check documentation for more info
			var guids = AssetDatabase.FindAssets("t:" + nameof(StyleSheet));
			if (guids.IsEmpty()) throw new NullReferenceException("No StyleSheet/USS found at all!");
			const string extension = ".uss";
			var correctedName = $"{name.TrimEnd(extension)}{extension}";
			var count = 0;
			var styleSheetPath = "";
			foreach (var guid in guids)
			{
				var path = AssetDatabase.GUIDToAssetPath(guid);
				var fileName = Path.GetFileName(path);
				if (!fileName.Equals(correctedName)) continue;
				styleSheetPath = path;
				count++;
				if (count > 1) throw new Exception("More than one StyleSheet/USS found with the same name!");
			}

			var styleSheet = AssetDatabase.LoadAssetAtPath<StyleSheet>(styleSheetPath);
			return styleSheet ?? throw new NullReferenceException("StyleSheet/USS is null! Path was incorrect");
		}

		/// <summary>
		/// Load Stylesheets from the path
		/// </summary>
		/// <param name="path">It doesn't need 'Assets/' or '.uss',
		/// but it would still compile correctly with these included</param>
		/// <returns></returns>
		public static VisualTreeAsset LoadUxml(string path)
		{
			if (string.IsNullOrEmpty(path)) throw new NullReferenceException("Path for VisualTreeAsset/UXML is null or empty");
			const string extension = ".uxml";
			var correctedPath = path.TrimStart(AssetPath).TrimEnd(extension);
			var uxml = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(AssetPath + correctedPath + extension);
			return uxml ?? throw new NullReferenceException("VisualTreeAsset/UXML is null! Path was incorrect");
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="name"></param>
		/// <returns></returns>
		/// <exception cref="NullReferenceException"></exception>
		/// <exception cref="Exception"></exception>
		public static VisualTreeAsset LoadUxmlWithName(string name)
		{
			//FindAssets uses tags check documentation for more info
			var guids = AssetDatabase.FindAssets("t:" + nameof(VisualTreeAsset));
			if (guids.IsEmpty()) throw new NullReferenceException("No VisualTreeAsset/UXML found at all!");
			const string extension = ".uxml";
			var correctedName = $"{name.TrimEnd(extension)}{extension}";
			var count = 0;
			var visualTreePath = "";
			foreach (var guid in guids)
			{
				var path = AssetDatabase.GUIDToAssetPath(guid);
				var fileName = Path.GetFileName(path);
				if (!fileName.Equals(correctedName)) continue;
				visualTreePath = path;
				count++;
				if (count > 1) throw new Exception("More than one VisualTreeAsset/UXML found with the same name!");
			}

			var visualTree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(visualTreePath);
			return visualTree ?? throw new NullReferenceException("VisualTreeAsset/UXML is null! Path was incorrect");
		}

		public static List<SceneAsset> GetAllScenes(SerializedProperty scenesProperty)
		{
			var scenes = new List<SceneAsset>();
			for (var i = 0; i < scenesProperty.arraySize; i++)
			{
				var path = scenesProperty.GetArrayElementAtIndex(i).stringValue;
				var sceneAsset = AssetDatabase.LoadAssetAtPath<SceneAsset>(path);
				if (sceneAsset is null) throw new NullReferenceException("SceneAsset is null! Path was incorrect");
				scenes.Add(sceneAsset);
			}

			return scenes;
		}

		public static SceneAsset GetScene(SerializedProperty scenesProperty)
		{
			var path = scenesProperty.stringValue;
			var scene = AssetDatabase.LoadAssetAtPath<SceneAsset>(path);
			return scene;
		}
	}
}