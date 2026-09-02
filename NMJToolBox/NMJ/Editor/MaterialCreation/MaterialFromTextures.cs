using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace NMJTools.MaterialCreation
{
	/// <summary>
	/// Project-window context menu: right-click one or more texture folders →
	/// <b>NMJ ▸ Create Material From Textures</b>.
	///
	/// Creates (or updates) one material per folder, wiring up the textures found there by filename
	/// suffix and correcting each one's import settings. What counts as a suffix, which shader
	/// property each map goes to and how each must be imported all come from a
	/// <see cref="MaterialSetupProfile"/> asset, so this works for whatever naming convention a
	/// project's texture generator happens to emit.
	/// </summary>
	public static class MaterialFromTextures
	{
		private const string MenuPath = "Assets/NMJ/Create Material From Textures";

		[MenuItem(MenuPath, false, 22)]
		private static void Create()
		{
			var folders = SelectedFolders();
			if (folders.Count == 0) return;

			var profile = ResolveProfile();
			if (!profile) return;

			var shader = Shader.Find(profile.shaderName);
			if (!shader)
			{
				EditorUtility.DisplayDialog(
											"Shader not found",
											$"Could not find '{profile.shaderName}'.\n\nCorrect the Shader Name on " +
											$"the '{profile.name}' profile, or generate the shader first.", "OK");
				return;
			}

			var created = new List<Object>();

			try
			{
				AssetDatabase.StartAssetEditing();

				for (var i = 0; i < folders.Count; i++)
				{
					EditorUtility.DisplayProgressBar("Creating materials", folders[i], (float)i / folders.Count);

					var material = CreateForFolder(folders[i], profile, shader);
					if (material) created.Add(material);
				}
			}
			finally
			{
				AssetDatabase.StopAssetEditing();
				EditorUtility.ClearProgressBar();
			}

			AssetDatabase.SaveAssets();

			if (created.Count == 0) return;

			Selection.objects = created.ToArray();
			EditorGUIUtility.PingObject(created[0]);
		}

		[MenuItem(MenuPath, true)]
		private static bool Validate() => SelectedFolders().Count > 0;

		private static List<string> SelectedFolders() =>
			Selection.assetGUIDs
					 .Select(AssetDatabase.GUIDToAssetPath)
					 .Where(path => !string.IsNullOrEmpty(path) && AssetDatabase.IsValidFolder(path))
					 .ToList();

		/// <summary>
		/// The project's profile. With none in the project, offers to create one seeded with the URP
		/// Lit conventions rather than failing — the tool is unusable without one, and a first run
		/// should not be a scavenger hunt.
		/// </summary>
		private static MaterialSetupProfile ResolveProfile()
		{
			var profiles = AssetDatabase.FindAssets($"t:{nameof(MaterialSetupProfile)}")
										.Select(AssetDatabase.GUIDToAssetPath)
										.Select(AssetDatabase.LoadAssetAtPath<MaterialSetupProfile>)
										.Where(p => p)
										.ToList();

			switch (profiles.Count)
			{
				case 1: return profiles[0];

				case > 1:
					// Ambiguous rather than wrong: picking one silently would apply another team's
					// conventions to this folder.
					var names = string.Join(", ", profiles.Select(p => p.name));
					Debug.LogWarning($"[NMJ] {profiles.Count} MaterialSetupProfile assets found ({names}); " +
									 $"using '{profiles[0].name}'. Keep one per project to avoid the ambiguity.");
					return profiles[0];
			}

			if (!EditorUtility.DisplayDialog(
											 "No Material Setup Profile",
											 "This tool reads your texture naming conventions from a Material Setup Profile " +
											 "asset, and this project has none.\n\nCreate one now, pre-filled with the URP " +
											 "Lit conventions?",
											 "Create", "Cancel"))
				return null;

			var path = EditorUtility.SaveFilePanelInProject("Create Material Setup Profile",
														   "MaterialSetupProfile", "asset", string.Empty);
			if (string.IsNullOrEmpty(path)) return null;

			var profile = ScriptableObject.CreateInstance<MaterialSetupProfile>();
			profile.ApplyUrpLitDefaults();

			AssetDatabase.CreateAsset(profile, path);
			AssetDatabase.SaveAssets();

			return profile;
		}

		// ===================================================================================
		// Per-folder work
		// ===================================================================================

		private static Material CreateForFolder(string folderPath, MaterialSetupProfile profile, Shader shader)
		{
			var folderName = Path.GetFileName(folderPath);
			var found = FindTextures(folderPath, folderName, profile);

			var albedoSlot = profile.maps.FirstOrDefault(m => m.IsAlbedo);

			if (albedoSlot == null || !found.TryGetValue(albedoSlot, out var albedo) || !albedo)
			{
				var expected = string.Format(profile.albedoNamePattern, folderName);
				Debug.LogWarning($"[NMJ] '{folderName}': no albedo texture found. Expected one named " +
								 $"'{expected}', or any texture in the folder carrying none of the known " +
								 "suffixes. Skipped.");
				return null;
			}

			// Import settings first — assigning a normal map that is not flagged as one produces the
			// familiar "this texture is not marked as a normal map" nag and wrong lighting until fixed.
			if (profile.correctImportSettings) ApplyImportSettings(found);

			var materialPath = $"{folderPath}/{profile.materialPrefix}{folderName}.mat";
			var existing = AssetDatabase.LoadAssetAtPath<Material>(materialPath);

			if (existing && !profile.updateExistingMaterials) return null;

			var isNew = !existing;
			var material = existing;

			if (isNew) material = new Material(shader) { name = $"{profile.materialPrefix}{folderName}" };
			else if (material.shader != shader) material.shader = shader;

			var assigned = new List<string>();

			foreach (var (slot, texture) in found)
			{
				if (!slot.HasShaderSlot || !texture) continue;
				if (!material.HasProperty(slot.shaderProperty)) continue;

				material.SetTexture(slot.shaderProperty, texture);
				assigned.Add(slot.label);
			}

			if (isNew)
			{
				ApplyDefaults(material, profile);
				AssetDatabase.CreateAsset(material, materialPath);
			}
			else
			{
				EditorUtility.SetDirty(material);
			}

			ReportUnusedMaps(folderName, found);

			Debug.Log($"[NMJ] {(isNew ? "Created" : "Updated")} {profile.materialPrefix}{folderName} " +
					  $"({string.Join(" + ", assigned)})", material);

			return material;
		}

		/// <summary>
		/// One texture per slot. Ordered by suffix length, longest first, so a short alias can never
		/// shadow a longer one that also matches.
		/// </summary>
		private static Dictionary<MaterialMapSlot, Texture2D> FindTextures(
			string folderPath, string folderName, MaterialSetupProfile profile)
		{
			var result = new Dictionary<MaterialMapSlot, Texture2D>();
			var priorities = new Dictionary<MaterialMapSlot, int>();

			var lookup = profile.maps
								.Where(m => !m.IsAlbedo)
								.SelectMany(m => m.suffixes.Select((suffix, index) => (suffix, slot: m, index)))
								.OrderByDescending(e => e.suffix.Length)
								.ToList();

			var albedoSlot = profile.maps.FirstOrDefault(m => m.IsAlbedo);
			var preferredAlbedo = string.Format(profile.albedoNamePattern, folderName);

			foreach (var guid in AssetDatabase.FindAssets("t:Texture2D", new[] { folderPath }))
			{
				var path = AssetDatabase.GUIDToAssetPath(guid);

				// FindAssets recurses; keep to this folder's own textures so a nested variant folder
				// cannot pull its maps into the parent's material.
				if (!string.Equals(Path.GetDirectoryName(path)?.Replace('\\', '/'), folderPath,
								   StringComparison.OrdinalIgnoreCase))
					continue;

				var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
				if (!texture) continue;

				var name = Path.GetFileNameWithoutExtension(path);

				if (albedoSlot != null && string.Equals(name, preferredAlbedo, StringComparison.OrdinalIgnoreCase))
				{
					result[albedoSlot] = texture;
					priorities[albedoSlot] = -1; // an exact match outranks the fallback
					continue;
				}

				var match = lookup.FirstOrDefault(e => name.EndsWith(e.suffix, StringComparison.OrdinalIgnoreCase));

				if (match.slot != null)
				{
					// Lower index means an alias listed earlier, i.e. the current convention.
					if (!result.ContainsKey(match.slot) || match.index < priorities[match.slot])
					{
						result[match.slot] = texture;
						priorities[match.slot] = match.index;
					}

					continue;
				}

				// Fallback: any suffix-less texture, so an off-convention name still works.
				if (albedoSlot != null && !result.ContainsKey(albedoSlot))
				{
					result[albedoSlot] = texture;
					priorities[albedoSlot] = int.MaxValue;
				}
			}

			return result;
		}

		private static void ApplyDefaults(Material material, MaterialSetupProfile profile)
		{
			foreach (var setting in profile.defaults)
			{
				if (string.IsNullOrWhiteSpace(setting.shaderProperty)) continue;
				if (!material.HasProperty(setting.shaderProperty)) continue;

				switch (setting.kind)
				{
					case MaterialDefault.ValueKind.Float:
						material.SetFloat(setting.shaderProperty, setting.floatValue);
						break;

					case MaterialDefault.ValueKind.Colour:
						material.SetColor(setting.shaderProperty, setting.colourValue);
						break;

					case MaterialDefault.ValueKind.TextureByName:
						var texture = FindTextureByName(setting.textureAssetName);
						if (texture) material.SetTexture(setting.shaderProperty, texture);
						else Debug.LogWarning($"[NMJ] Default texture '{setting.textureAssetName}' not found.");
						break;
				}
			}
		}

		private static Texture2D FindTextureByName(string assetName)
		{
			if (string.IsNullOrWhiteSpace(assetName)) return null;

			return AssetDatabase.FindAssets($"{assetName} t:Texture2D")
								.Select(AssetDatabase.GUIDToAssetPath)
								.Where(path => string.Equals(Path.GetFileNameWithoutExtension(path), assetName,
															 StringComparison.OrdinalIgnoreCase))
								.Select(AssetDatabase.LoadAssetAtPath<Texture2D>)
								.FirstOrDefault(t => t);
		}

		/// <summary>
		/// Most of what a texture generator emits often has no home in the chosen shader. Say so once
		/// per folder rather than dropping it silently — a missing map nobody mentioned is how art
		/// bugs start.
		/// </summary>
		private static void ReportUnusedMaps(string folderName, Dictionary<MaterialMapSlot, Texture2D> found)
		{
			var unused = found.Where(pair => !pair.Key.HasShaderSlot && pair.Value)
							  .Select(pair => pair.Key.label)
							  .ToArray();

			if (unused.Length == 0) return;

			Debug.Log($"[NMJ] '{folderName}': {unused.Length} map(s) found but unused — " +
					  $"{string.Join(", ", unused)}. Import flags were still corrected, so they are " +
					  "already right the day the shader grows a slot for them.");
		}

		/// <summary>
		/// Sets each map's import type and colour space, reimporting only what actually changed.
		/// Applied to every map found, including the ones nothing samples yet.
		/// </summary>
		private static void ApplyImportSettings(Dictionary<MaterialMapSlot, Texture2D> found)
		{
			foreach (var (slot, texture) in found) Configure(texture, slot.kind);
		}

		private static void Configure(Texture2D texture, TextureKind kind)
		{
			if (!texture) return;

			var path = AssetDatabase.GetAssetPath(texture);
			if (AssetImporter.GetAtPath(path) is not TextureImporter importer) return;

			var type = kind == TextureKind.NormalMap ? TextureImporterType.NormalMap : TextureImporterType.Default;
			var sRgb = kind == TextureKind.Colour;
			var changed = false;

			if (importer.textureType != type)
			{
				importer.textureType = type;
				changed = true;
			}

			// sRGB is meaningless on a normal map and Unity ignores it there, so only touch it otherwise.
			if (kind != TextureKind.NormalMap && importer.sRGBTexture != sRgb)
			{
				importer.sRGBTexture = sRgb;
				changed = true;
			}

			if (changed) importer.SaveAndReimport();
		}
	}
}
