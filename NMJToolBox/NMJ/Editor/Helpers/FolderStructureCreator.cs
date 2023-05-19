using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public static class FolderStructureCreator
{
	[MenuItem("Tools/Create Custom Folder Structure")]
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
			$"{mainFolderPath}/Runtime/Scenes",
			$"{mainFolderPath}/Runtime/Scripts",
			$"{mainFolderPath}/Runtime/Materials",
			$"{mainFolderPath}/Runtime/Textures",
			$"{mainFolderPath}/Runtime/Prefabs",
			$"{mainFolderPath}/Runtime/Audio",
			$"{mainFolderPath}/Runtime/Animations",
			$"{mainFolderPath}/Runtime/Fonts",
			$"{mainFolderPath}/Runtime/Resources",
			$"{mainFolderPath}/Runtime/ScriptableObjects",
			$"{mainFolderPath}/Runtime/Scripts/Managers",
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