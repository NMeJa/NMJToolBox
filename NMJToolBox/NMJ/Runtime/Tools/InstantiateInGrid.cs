using UnityEngine;

namespace NMJTools.Tools
{
	/// <summary>
	/// Lays copies of a prefab out on a grid.
	/// </summary>
	/// <remarks>
	/// this changed: this file lived in <c>Editor/Tools/</c>, inside an assembly definition whose
	/// only platform is Editor. A MonoBehaviour in an editor-only assembly cannot exist in a build:
	/// Unity strips the assembly, and every scene or prefab referencing the component loads with a
	/// missing script. It has moved to the runtime assembly, keeping its .meta so the GUID - and
	/// therefore any existing scene reference - survives the move.
	///
	/// The grid is built by <c>InstantiateInGridEditor</c> in edit mode, which routes through
	/// <see cref="UnityEditor.PrefabUtility"/> and registers Undo. The runtime <see cref="Build"/>
	/// below is the play-mode path and deliberately does neither.
	/// </remarks>
	[AddComponentMenu("NMJ/Tools/Instantiate In Grid")]
	public class InstantiateInGrid : MonoBehaviour
	{
		[Header("Source")]

		[SerializeField] private GameObject objectToInstantiate;

		[Tooltip("Parent for the copies. Falls back to this transform.")]
		[SerializeField] private Transform parent;

		[Header("Grid")]

		[SerializeField, Min(1)] private int columns = 1;
		[SerializeField, Min(1)] private int rows = 1;

		[Tooltip("Spacing between cells, on the X and Z axes.")]
		[SerializeField] private Vector2 spacing = Vector2.one;

		[Tooltip("Local-space offset of the first cell.")]
		[SerializeField] private Vector3 origin;

		[Tooltip("Centre the grid on the origin instead of growing from it towards +X/+Z.")]
		[SerializeField] private bool centred;

		public GameObject Prefab => objectToInstantiate;
		public Transform Parent => parent ? parent : transform;
		public int Columns => columns;
		public int Rows => rows;
		public int CellCount => columns * rows;

		/// <summary>Local position of one cell.</summary>
		public Vector3 CellPosition(int column, int row)
		{
			// this changed: the old version wrote absolute x/z into localPosition and dropped y
			// entirely, so a start offset on Y was impossible and the copies always sat on the
			// parent's plane. Offsetting from a full Vector3 origin keeps Y meaningful.
			var offset = centred
							 ? new Vector3((columns - 1) * spacing.x * -0.5f, 0f, (rows - 1) * spacing.y * -0.5f)
							 : Vector3.zero;

			return origin + offset + new Vector3(column * spacing.x, 0f, row * spacing.y);
		}

		/// <summary>Suffix appended to each copy's name, so cells are identifiable in the hierarchy.</summary>
		public static string CellSuffix(int column, int row) => $" [{column},{row}]";

		/// <summary>
		/// Builds the grid at runtime. In the editor use the inspector button instead — it registers
		/// Undo and keeps the prefab connection, neither of which plain <c>Instantiate</c> does.
		/// </summary>
		public void Build()
		{
			if (!objectToInstantiate)
			{
				Debug.LogError("[NMJ] InstantiateInGrid has no prefab assigned.", this);
				return;
			}

			var target = Parent;

			for (var column = 0; column < columns; column++)
			{
				for (var row = 0; row < rows; row++)
				{
					var clone = Instantiate(objectToInstantiate, target);
					clone.name = objectToInstantiate.name + CellSuffix(column, row);
					clone.transform.localPosition = CellPosition(column, row);
				}
			}
		}

		private void OnDrawGizmosSelected()
		{
			if (columns <= 0 || rows <= 0) return;

			Gizmos.color = new Color(0.4f, 0.85f, 1f, 0.6f);
			Gizmos.matrix = Parent.localToWorldMatrix;

			var size = Mathf.Min(spacing.x, spacing.y) * 0.15f;

			for (var column = 0; column < columns; column++)
			for (var row = 0; row < rows; row++)
				Gizmos.DrawWireCube(CellPosition(column, row), Vector3.one * size);
		}
	}
}
