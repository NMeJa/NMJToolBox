using System.Collections;
using UnityEngine;

namespace NMJToolBox.Demos
{
    public class ButtonTestOne : MonoBehaviour
    {
        public int myInt;

        [ButtonBeginHorizontal, ButtonBeginVertical]
        [Button(enabledMode: EButtonEnableMode.Always)]
        private void IncrementMyInt()
        {
            myInt++;
        }

        [Button("Decrement My Int", EButtonEnableMode.Editor)]
        private void DecrementMyInt()
        {
            myInt--;
        }

        [ButtonEndVertical]
        [Button(enabledMode: EButtonEnableMode.Playmode)]
        private void LogMyInt(string prefix = "MyInt = ")
        {
            Debug.Log(prefix + myInt);
        }

        [ButtonEndHorizontal, Button("StartCoroutine")]
        private IEnumerator IncrementMyIntCoroutine()
        {
            int seconds = 5;
            for (int i = 0; i < seconds; i++)
            {
                myInt++;
                yield return new WaitForSeconds(1.0f);
            }
        }
    }
}