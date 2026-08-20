using UnityEngine;

namespace NMJTools.Scaffolding
{
	/// <summary>
	/// A folder layout saved as a project asset.
	///
	/// This is deliberately the project-level setting rather than a <c>SettingsProvider</c>: a
	/// template asset lives in the repository, so a team shares one structure and changes to it show
	/// up in review. Editor settings are per-user and per-machine, which is the wrong scope for a
	/// convention everyone is supposed to follow.
	///
	/// Create one from <c>Assets ▸ Create ▸ NMJ ▸ Folder Template</c>, or from the
	/// <see cref="FolderStructureWindow"/>'s <b>Save as Template…</b> button.
	/// </summary>
	[CreateAssetMenu(fileName = "FolderTemplate", menuName = "NMJ/Folder Template", order = 200)]
	public class FolderTemplate : ScriptableObject
	{
		[SerializeField] private FolderLayout layout = new();

		public FolderLayout Layout => layout;

		/// <summary>A copy, so the window can edit freely without dirtying the asset.</summary>
		public FolderLayout CloneLayout()
		{
			var clone = layout.Clone();
			if (string.IsNullOrWhiteSpace(clone.layoutName)) clone.layoutName = name;
			return clone;
		}

		public void Apply(FolderLayout source)
		{
			layout = source.Clone();
			layout.layoutName = string.IsNullOrWhiteSpace(source.layoutName) ? name : source.layoutName;
		}
	}
}
