using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Vis.SceneSwitcher
{
    [Serializable]
    public class SerializableScenesContainer : ScriptableObject
    {
        [HideInInspector]
        public List<SceneAsset> SceneAssets;
    }
}
