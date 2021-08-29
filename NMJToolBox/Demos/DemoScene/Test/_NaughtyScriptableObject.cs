using System.Collections.Generic;
using UnityEngine;

namespace NMJToolBox.Demos
{
	//[CreateAssetMenu(fileName = "NaughtyScriptableObject", menuName = "NaughtyAttributes/_NaughtyScriptableObject")]
	public class _NaughtyScriptableObject : ScriptableObject
	{
		[Expandable]
		public List<_TestScriptableObject> list;
	}
}
