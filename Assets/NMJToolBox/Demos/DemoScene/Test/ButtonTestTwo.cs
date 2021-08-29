using UnityEngine;

namespace NMJToolBox.Demos
{
    public class ButtonTestTwo : MonoBehaviour
    {
        [Button]
        private void Do()
        {
            Debug.Log($"Do");
        }

        [ButtonBeginHorizontal]
        [ButtonBeginVertical("A")]
        [Button]
        public void A1()
        {
            Debug.Log($"A1");
        }

        [Button]
        protected void A2()
        {
            Debug.Log($"A2");
        }

        [Button]
        private void A3()
        {
            Debug.Log($"A3");
        }

        [ButtonEndVertical]
        [Button]
        private void A4()
        {
            Debug.Log($"A4");
        }


        [Button]
        [ButtonBeginVertical("B")]
        private void B1()
        {
            Debug.Log($"B1");
        }

        [Button]
        private void B2()
        {
            Debug.Log($"B2");
        }


        [ButtonEndHorizontal, Button()]
        internal void B3()
        {
            Debug.Log($"B3");
        }
    }
}