using UnityEngine;

namespace NMJToolBox.Demos
{
    public class TestGameObjectsToggler : MonoBehaviour
    {
        public void OnToggle(GameObject go)
        {
            Debug.Log("Toggle ->" + go);
        }

        public void OnToggleA()
        {
            Debug.Log("A is active");
        }

        public void OnToggleB()
        {
            Debug.Log("B is active");
        }
    }
}