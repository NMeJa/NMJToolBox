using UnityEngine;

namespace NMJToolBox
{
    [ExecuteAlways]
    public class InstantiateInGrid : MonoBehaviour
    {
        [SerializeField, Min(0)] private int column = 1;
        [SerializeField, Min(0)] private int row = 1;
        [SerializeField] private Transform parent;
        [SerializeField] private GameObject objectToInstantiate;
        [SerializeField] private float xSize = 1f;
        [SerializeField] private float zSize = 1f;
        [SerializeField] private float startXPos = 0f;
        [SerializeField] private float startZPos = 0f;

        [ContextMenu("Instantiate In Grid")]
        private void InstantiateGrid()
        {
            for (int x = 0; x < column; x++)
            {
                for (int z = 0; z < row; z++)
                {
                    var clone = Instantiate(objectToInstantiate, parent);
                    clone.name += $"{x} {z}";
                    var cloneTransform = clone.transform;
                    var pos = cloneTransform.localPosition;
                    pos.x = startXPos + x * xSize;
                    pos.z = startZPos + z * zSize;
                    cloneTransform.localPosition = pos;
                }
            }
        }
    }
}