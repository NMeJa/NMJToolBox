using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace NMJToolBox
{
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
            string[] guids = AssetDatabase.FindAssets("t:" + typeof(T).Name);
            var a = new T[guids.Length];
            for (int i = 0; i < guids.Length; i++) //probably could get optimized 
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                a[i] = AssetDatabase.LoadAssetAtPath<T>(path);
            }

            return a;
        }

        /// <summary>
        /// Easily Load Stylesheets from the path
        /// </summary>
        /// <param name="path">It doesn't need 'Assets/' or '.uss',
        /// but it would still compile correctly with these included</param>
        /// <returns></returns>
        public static StyleSheet LoadUss(string path)
        {
            if (string.IsNullOrEmpty(path))
                throw new NullReferenceException("Path for StyleSheet/USS is null or empty");
            const string extension = ".uss";
            string correctedPath = path.TrimStart(AssetPath).TrimEnd(extension);
            var styleSheet = AssetDatabase.LoadAssetAtPath<StyleSheet>(AssetPath + correctedPath + extension);
            if (styleSheet is null) throw new NullReferenceException("StyleSheet/USS is null! Path was incorrect");
            return styleSheet;
        }

        public static StyleSheet LoadUssWithName(string name)
        {
            //FindAssets uses tags check documentation for more info
            string[] guids = AssetDatabase.FindAssets("t:" + nameof(StyleSheet));
            if (guids.IsEmpty()) throw new NullReferenceException("No StyleSheet/USS found at all!");
            const string extension = ".uss";
            string correctedName = $"{name.TrimEnd(extension)}{extension}";
            int count = 0;
            string styleSheetPath = "";
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                string fileName = Path.GetFileName(path);
                if (!fileName.Equals(correctedName)) continue;
                styleSheetPath = path;
                count++;
                if (count > 1) throw new Exception("More than one StyleSheet/USS found with the same name!");
            }

            var styleSheet = AssetDatabase.LoadAssetAtPath<StyleSheet>(styleSheetPath);
            if (styleSheet is null) throw new NullReferenceException("StyleSheet/USS is null! Path was incorrect");
            return styleSheet;
        }

        /// <summary>
        /// Easily Load Stylesheets from the path
        /// </summary>
        /// <param name="path">It doesn't need 'Assets/' or '.uss',
        /// but it would still compile correctly with these included</param>
        /// <returns></returns>
        public static VisualTreeAsset LoadUxml(string path)
        {
            if (string.IsNullOrEmpty(path))
                throw new NullReferenceException("Path for VisualTreeAsset/UXML is null or empty");
            const string extension = ".uxml";
            string correctedPath = path.TrimStart(AssetPath).TrimEnd(extension);
            var uxml = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(AssetPath + correctedPath + extension);
            if (uxml is null) throw new NullReferenceException("VisualTreeAsset/UXML is null! Path was incorrect");
            return uxml;
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
            string[] guids = AssetDatabase.FindAssets("t:" + nameof(VisualTreeAsset));
            if (guids.IsEmpty()) throw new NullReferenceException("No VisualTreeAsset/UXML found at all!");
            const string extension = ".uxml";
            string correctedName = $"{name.TrimEnd(extension)}{extension}";
            int count = 0;
            string visualTreePath = "";
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                string fileName = Path.GetFileName(path);
                if (!fileName.Equals(correctedName)) continue;
                visualTreePath = path;
                count++;
                if (count > 1) throw new Exception("More than one VisualTreeAsset/UXML found with the same name!");
            }

            var visualTree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(visualTreePath);
            if (visualTree is null)
                throw new NullReferenceException("VisualTreeAsset/UXML is null! Path was incorrect");
            return visualTree;
        }

        public static List<SceneAsset> GetAllScenes(SerializedProperty scenesProperty)
        {
            List<SceneAsset> scenes = new List<SceneAsset>();
            for (int i = 0; i < scenesProperty.arraySize; i++)
            {
                string path = scenesProperty.GetArrayElementAtIndex(i).stringValue;
                SceneAsset sceneAsset = AssetDatabase.LoadAssetAtPath<SceneAsset>(path);
                if (sceneAsset is null) throw new NullReferenceException("SceneAsset is null! Path was incorrect");
                scenes.Add(sceneAsset);
            }

            return scenes;
        }

        public static SceneAsset GetScene(SerializedProperty scenesProperty)
        {
            string path = scenesProperty.stringValue;
            SceneAsset scene = AssetDatabase.LoadAssetAtPath<SceneAsset>(path);
            return scene;
        }
    }
}