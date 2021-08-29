using UnityEngine;

namespace NMJToolBox.Demos
{
    public class TestTriggerNotifier : MonoBehaviour
    {
        public void OnEnter(Collider col)
        {
            Debug.Log(col + " entered the cube");
        }

        public void OnStay(Collider col)
        {
            Debug.Log(col + " stays in the cube");
        }

        public void OnExit(Collider col)
        {
            Debug.Log(col + " left the cube");
        }
    }
}