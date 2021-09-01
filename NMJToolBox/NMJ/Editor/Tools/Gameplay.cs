using UnityEditor;
using UnityEngine;

namespace NMJToolBox
{
    public static class GameplayTools
    {
    
        [MenuItem("Tools/NMJ/Reset Transform %_R")]
        public static void ResetTransform()
        {
            var selectedGameObjects = Selection.transforms;
            foreach (var element in selectedGameObjects)
            {
                SetUndo(element, $"{element.name}: Transform Reset");
                if (element.TryGetComponent(out RectTransform rect))
                {
                    Vector2 half = Vector2.one * 0.5f;
                    rect.pivot = half;
                    rect.anchorMin = half;
                    rect.anchorMax = half;
                    rect.sizeDelta = Vector2.one * 100f;
                }

                element.localPosition = Vector3.zero;
                element.localRotation = Quaternion.identity;
                element.localScale = Vector3.one;
            }
        }

        private static void SetUndo(Object target, string title)
        {
            Undo.RecordObject(target, title);
        }
    }
}
