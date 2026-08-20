using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace NMJTools.Helpers
{
	public static class FolderStructureCreator
	{
		[MenuItem("Tools/NMJ/Create Custom Folder Structure")]
		private static void CreateCustomFolderStructure()
		{
			const string projectName = "__Game";
			var mainFolderPath = $"Assets/{projectName}";

			CreateFolderRecursively(mainFolderPath);

			var editorFolders = new[]
			{
				$"{mainFolderPath}/Editor/Scripts",
				$"{mainFolderPath}/Editor/EditorResources"
			};

			var runtimeFolders = new[]
			{
				$"{mainFolderPath}/Runtime/Scripts",
				$"{mainFolderPath}/Runtime/Scenes/Gameplay",
				$"{mainFolderPath}/Runtime/Scenes/Testing",
				$"{mainFolderPath}/Runtime/Prefabs/UI",
				$"{mainFolderPath}/Runtime/Prefabs/Gameplay",
				$"{mainFolderPath}/Runtime/Prefabs/Environment",
				$"{mainFolderPath}/Runtime/Art/Animations",
				$"{mainFolderPath}/Runtime/Art/Materials",
				$"{mainFolderPath}/Runtime/Art/Models",
				$"{mainFolderPath}/Runtime/Art/Shaders",
				$"{mainFolderPath}/Runtime/Art/Textures",
				$"{mainFolderPath}/Runtime/Art/VFX",
				$"{mainFolderPath}/Runtime/Audio/Music",
				$"{mainFolderPath}/Runtime/Audio/SFX",
				$"{mainFolderPath}/Runtime/Audio/Ambience",
				$"{mainFolderPath}/Runtime/Data/ScriptableObjects",
				$"{mainFolderPath}/Runtime/Settings",
				$"{mainFolderPath}/Runtime/UI/Fonts",
				$"{mainFolderPath}/Runtime/UI/Icons"
			};

			CreateFolders(editorFolders);
			CreateFolders(runtimeFolders);

			AssetDatabase.Refresh();
		}

		private static void CreateFolders(IEnumerable<string> folders)
		{
			foreach (var folder in folders)
				CreateFolderRecursively(folder);
		}

		private static void CreateFolderRecursively(string folderPath)
		{
			if (!AssetDatabase.IsValidFolder(folderPath))
			{
				var parentFolder = folderPath[..folderPath.LastIndexOf('/')];
				if (!AssetDatabase.IsValidFolder(parentFolder))
				{
					CreateFolderRecursively(parentFolder);
				}

				var newFolderName = folderPath[(folderPath.LastIndexOf('/') + 1)..];
				AssetDatabase.CreateFolder(parentFolder, newFolderName);
				Debug.Log($"Created folder: {folderPath}");
			}
			else
			{
				Debug.Log($"Folder already exists: {folderPath}");
			}
		}
	}
}