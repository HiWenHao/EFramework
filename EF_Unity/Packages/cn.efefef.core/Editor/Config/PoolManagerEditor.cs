#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace EasyFramework.Systems.Pool.Editor
{
    [CustomEditor(typeof(PoolSystem))]
    public class PoolManagerEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            var mgr = (PoolSystem)target;

            GUILayout.Space(10);

            if (GUILayout.Button("Dump Leaks"))
            {
                mgr.DumpAllLeaks();
            }
        }
    }
}
#endif