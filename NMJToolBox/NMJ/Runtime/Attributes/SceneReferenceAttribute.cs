using UnityEngine;

namespace NMJToolBox
{
	/// <summary>
	/// Turns a <see cref="string"/> field into a scene picker in the inspector: drag a scene asset in
	/// and the field stores its path, which is what the runtime <c>SceneManager</c> API actually
	/// wants.
	///
	/// <code>
	/// [SerializeField, SceneReference] private string mainMenu;
	/// ...
	/// SceneManager.LoadScene(mainMenu);
	/// </code>
	///
	/// This generalises the hand-written inspector that <c>SceneChanger</c> used to need. A raw
	/// string is fragile — rename or move the scene and it breaks silently at runtime — while a
	/// <c>SceneAsset</c> field cannot exist in a build at all, since <c>SceneAsset</c> is an editor
	/// type. Storing the path but *editing* it as an asset gets both halves: the drawer rewrites the
	/// path whenever the asset moves, and warns when the scene is missing from Build Settings.
	/// </summary>
	public class SceneReferenceAttribute : PropertyAttribute
	{
		/// <summary>Warn in the inspector when the scene is absent or disabled in Build Settings.</summary>
		public bool RequireInBuildSettings { get; }

		public SceneReferenceAttribute(bool requireInBuildSettings = true)
		{
			RequireInBuildSettings = requireInBuildSettings;
		}
	}
}
