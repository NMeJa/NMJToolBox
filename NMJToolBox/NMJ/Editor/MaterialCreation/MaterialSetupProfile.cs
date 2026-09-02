using System;
using System.Collections.Generic;
using UnityEngine;

namespace NMJTools.MaterialCreation
{
	/// <summary>How a map must be imported.</summary>
	public enum TextureKind
	{
		/// <summary>sRGB. Only ever albedo and emissive.</summary>
		Colour,

		/// <summary>Linear. Everything that is data rather than colour — masks, roughness, AO.</summary>
		LinearData,

		/// <summary>Imported as a normal map.</summary>
		NormalMap
	}

	/// <summary>One map role: how to recognise it, where it goes, and how it must be imported.</summary>
	[Serializable]
	public class MaterialMapSlot
	{
		[Tooltip("Shown in console output. Purely descriptive.")]
		public string label = "Map";

		[Tooltip("Filename suffixes matched case-insensitively, current convention first. " +
				 "Leave the list empty to mark this as the albedo — the map identified by carrying " +
				 "none of the other suffixes.")]
		public string[] suffixes = Array.Empty<string>();

		[Tooltip("Shader property this map is assigned to, e.g. _BaseMap. Leave empty to recognise " +
				 "the map — so it is neither mistaken for the albedo nor left on the wrong import " +
				 "settings — without assigning it anywhere.")]
		public string shaderProperty = string.Empty;

		public TextureKind kind = TextureKind.LinearData;

		public bool IsAlbedo => suffixes == null || suffixes.Length == 0;
		public bool HasShaderSlot => !string.IsNullOrWhiteSpace(shaderProperty);
	}

	/// <summary>A value written onto a newly created material and never touched again.</summary>
	[Serializable]
	public class MaterialDefault
	{
		public enum ValueKind
		{
			Float,
			Colour,

			/// <summary>A texture found by asset name — a ramp, a noise mask, a fallback.</summary>
			TextureByName
		}

		public string shaderProperty = string.Empty;
		public ValueKind kind = ValueKind.Float;
		public float floatValue;
		public Color colourValue = Color.white;
		public string textureAssetName = string.Empty;
	}

	/// <summary>
	/// Describes how to build a material from a folder of textures, for one project's conventions.
	/// </summary>
	/// <remarks>
	/// this changed: what used to live here was <c>StandardMaterialCreator</c>, which was not a
	/// reusable tool at all — it was one project's code that had been copied into the shared toolbox.
	/// It hardcoded the namespace <c>StockAndSorcery.EditorTools</c>, the shader
	/// <c>Elinis/TCP2_StockAndSorcery_Standard</c>, a ramp asset called <c>Ramp_WarmCosy</c>, and an
	/// <c>Assets/Elinis/Create Standard</c> menu item. Installed anywhere else it added a menu entry
	/// that could only ever fail with "shader not found".
	///
	/// The logic underneath it was worth keeping — suffix aliases with priority, an albedo identified
	/// by the absence of a suffix, import flags corrected even on maps the shader cannot sample — so
	/// it now reads all of that from an asset instead of from constants. A project's conventions live
	/// in the project.
	/// </remarks>
	[CreateAssetMenu(fileName = "MaterialSetupProfile", menuName = "NMJ/Material Setup Profile", order = 202)]
	public class MaterialSetupProfile : ScriptableObject
	{
		[Tooltip("Shader assigned to created materials, as Shader.Find takes it.")]
		public string shaderName = "Universal Render Pipeline/Lit";

		[Tooltip("Prepended to the folder name to name the material.")]
		public string materialPrefix = "M_";

		[Tooltip("Preferred albedo filename, where {0} is the folder name. Tested before the suffix " +
				 "table, so a folder named e.g. Wood_Plank_H cannot have its own albedo matched as a " +
				 "height map.")]
		public string albedoNamePattern = "T_{0}";

		[Tooltip("Correct each map's import type and colour space. Getting these wrong is silent: a " +
				 "normal map not flagged as one lights incorrectly, and a mask left on sRGB has a " +
				 "gamma curve applied to what is data, not colour.")]
		public bool correctImportSettings = true;

		[Tooltip("Re-running on an existing material updates its textures but never its tuning values.")]
		public bool updateExistingMaterials = true;

		public List<MaterialMapSlot> maps = new();

		[Tooltip("Written only when a material is first created, so re-running never stamps over " +
				 "values tuned by hand in the inspector.")]
		public List<MaterialDefault> defaults = new();

		/// <summary>
		/// Fills a fresh profile with the URP Lit conventions, as a starting point that works out of
		/// the box rather than an empty list and a manual.
		/// </summary>
		public void ApplyUrpLitDefaults()
		{
			shaderName = "Universal Render Pipeline/Lit";
			materialPrefix = "M_";
			albedoNamePattern = "T_{0}";

			maps = new List<MaterialMapSlot>
			{
				new() { label = "albedo", suffixes = Array.Empty<string>(), shaderProperty = "_BaseMap", kind = TextureKind.Colour },
				new() { label = "normal", suffixes = new[] { "_N", "_Normal" }, shaderProperty = "_BumpMap", kind = TextureKind.NormalMap },
				new() { label = "metallic/smoothness", suffixes = new[] { "_MS", "_MetallicSmoothness" }, shaderProperty = "_MetallicGlossMap", kind = TextureKind.LinearData },
				new() { label = "occlusion", suffixes = new[] { "_AO", "_Occlusion", "_ambient" }, shaderProperty = "_OcclusionMap", kind = TextureKind.LinearData },
				new() { label = "emissive", suffixes = new[] { "_E", "_Emissive" }, shaderProperty = "_EmissionMap", kind = TextureKind.Colour },
				new() { label = "height", suffixes = new[] { "_H", "_displacement" }, shaderProperty = "_ParallaxMap", kind = TextureKind.LinearData },

				// Recognised but unassigned. This is not cosmetic: without an entry, an unknown suffix
				// falls through to the albedo fallback, so a folder with no exact T_<folder> match
				// could end up with its roughness map wired into _BaseMap.
				new() { label = "roughness", suffixes = new[] { "_R" }, shaderProperty = string.Empty, kind = TextureKind.LinearData },
				new() { label = "smoothness", suffixes = new[] { "_S" }, shaderProperty = string.Empty, kind = TextureKind.LinearData },
				new() { label = "cavity", suffixes = new[] { "_Cav" }, shaderProperty = string.Empty, kind = TextureKind.LinearData },
				new() { label = "curvature", suffixes = new[] { "_Curv" }, shaderProperty = string.Empty, kind = TextureKind.LinearData },
				new() { label = "dirt", suffixes = new[] { "_Dirt" }, shaderProperty = string.Empty, kind = TextureKind.LinearData },
				new() { label = "wear", suffixes = new[] { "_Wear" }, shaderProperty = string.Empty, kind = TextureKind.LinearData },
				new() { label = "packed ORM", suffixes = new[] { "_ORM" }, shaderProperty = string.Empty, kind = TextureKind.LinearData },
				new() { label = "packed MRA", suffixes = new[] { "_MRA" }, shaderProperty = string.Empty, kind = TextureKind.LinearData }
			};

			defaults = new List<MaterialDefault>();
		}

		private void Reset() => ApplyUrpLitDefaults();
	}
}
