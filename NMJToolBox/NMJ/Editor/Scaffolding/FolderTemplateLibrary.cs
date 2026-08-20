using System.Collections.Generic;
using System.Linq;
using UnityEditor;

namespace NMJTools.Scaffolding
{
	/// <summary>
	/// The layouts the window offers: a handful shipped as code, plus every
	/// <see cref="FolderTemplate"/> asset found in the project.
	///
	/// Built-ins are code rather than assets on purpose — a freshly installed package should offer
	/// something useful without first making the user create an asset, and shipping assets in a UPM
	/// package puts them in an immutable folder where they cannot be edited anyway.
	/// </summary>
	public static class FolderTemplateLibrary
	{
		/// <summary>Every project template asset, newest discovery order, or empty.</summary>
		public static List<FolderTemplate> FindProjectTemplates() =>
			AssetDatabase.FindAssets($"t:{nameof(FolderTemplate)}")
						 .Select(AssetDatabase.GUIDToAssetPath)
						 .Select(AssetDatabase.LoadAssetAtPath<FolderTemplate>)
						 .Where(t => t)
						 .OrderBy(t => t.name)
						 .ToList();

		public static IReadOnlyList<FolderLayout> BuiltIn => builtIn;

		private static readonly List<FolderLayout> builtIn = new()
		{
			new FolderLayout
			{
				layoutName = "Standard",
				description = "Runtime/Editor split with art, audio, data and UI broken out. " +
							  "A good default for a full 3D project.",
				rootFolderName = "__Game",
				createAssemblyDefinitions = true,
				folders = new List<string>
				{
					"Editor/Scripts",
					"Editor/EditorResources",
					"Runtime/Scripts",
					"Runtime/Scenes/Gameplay",
					"Runtime/Scenes/Testing",
					"Runtime/Prefabs/UI",
					"Runtime/Prefabs/Gameplay",
					"Runtime/Prefabs/Environment",
					"Runtime/Art/Animations",
					"Runtime/Art/Materials",
					"Runtime/Art/Models",
					"Runtime/Art/Shaders",
					"Runtime/Art/Textures",
					"Runtime/Art/VFX",
					"Runtime/Audio/Music",
					"Runtime/Audio/SFX",
					"Runtime/Audio/Ambience",
					"Runtime/Data/ScriptableObjects",
					"Runtime/Settings",
					"Runtime/UI/Fonts",
					"Runtime/UI/Icons"
				}
			},

			new FolderLayout
			{
				layoutName = "Minimal",
				description = "The smallest structure worth having. Good for jams and prototypes " +
							  "that should not spend their first hour on folders.",
				rootFolderName = "__Game",
				folders = new List<string>
				{
					"Scripts",
					"Scenes",
					"Prefabs",
					"Art",
					"Audio"
				}
			},

			new FolderLayout
			{
				layoutName = "Feature-based",
				description = "Grouped by feature instead of by asset type, so everything one " +
							  "system needs sits together. Scales better on large projects, where " +
							  "type-based folders turn every feature into a scavenger hunt.",
				rootFolderName = "__Game",
				createAssemblyDefinitions = true,
				folders = new List<string>
				{
					"Core/Scripts",
					"Core/Prefabs",
					"Features/_Template/Scripts",
					"Features/_Template/Prefabs",
					"Features/_Template/Art",
					"Shared/Art",
					"Shared/Audio",
					"Shared/UI",
					"Shared/Data",
					"Editor/Scripts",
					"Scenes/Gameplay",
					"Scenes/Testing"
				}
			},

			new FolderLayout
			{
				layoutName = "2D Game",
				description = "Sprite-first layout with atlases, tilemaps and 2D animation.",
				rootFolderName = "__Game",
				folders = new List<string>
				{
					"Editor/Scripts",
					"Runtime/Scripts",
					"Runtime/Scenes",
					"Runtime/Prefabs",
					"Runtime/Art/Sprites",
					"Runtime/Art/Atlases",
					"Runtime/Art/Tilemaps/Palettes",
					"Runtime/Art/Tilemaps/Tiles",
					"Runtime/Art/Animations",
					"Runtime/Art/Materials",
					"Runtime/Audio/Music",
					"Runtime/Audio/SFX",
					"Runtime/Data/ScriptableObjects",
					"Runtime/UI/Fonts",
					"Runtime/UI/Icons"
				}
			},

			new FolderLayout
			{
				layoutName = "Empty",
				description = "Nothing but the root folder. Start from here to build your own, then " +
							  "save it as a template asset.",
				rootFolderName = "__Game",
				createGitKeep = true,
				folders = new List<string>()
			}
		};
	}
}
